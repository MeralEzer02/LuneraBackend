using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lunera.API.Data;
using Lunera.API.DTOs;
using Lunera.Domain.Utilities;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;
using System.Threading.Tasks;
using System;

namespace Lunera.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Bu e-posta adresi zaten kullanımda.");
            }

            if (await _context.UserProfiles.AnyAsync(p => p.Nickname == request.Nickname))
            {
                return BadRequest("Bu takma ad başkası tarafından alınmış.");
            }

            if (!Enum.TryParse(request.Gender, true, out Gender genderEnum))
            {
                return BadRequest("Geçersiz cinsiyet değeri. (Valid: Male, Female, NonBinary)");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = PasswordHasher.HashPassword(request.Password),
                    RoleId = 1,
                    Status = 1,
                    CreatedAt = DateTime.UtcNow,
                    WarningCount = 0
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var profile = new UserProfile
                {
                    UserId = user.Id,
                    RealName = request.RealName,
                    Nickname = request.Nickname,
                    Gender = genderEnum,
                    BirthDate = request.BirthDate,
                    Bio = request.Bio ?? string.Empty,
                    IdentityVisible = false
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { message = "Kayıt başarılı." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Kayıt sırasında bir hata oluştu.");
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("E-posta veya şifre hatalı.");
            }

            var inputHash = PasswordHasher.HashPassword(request.Password);
            if (user.PasswordHash != inputHash)
            {
                return Unauthorized("E-posta veya şifre hatalı.");
            }

            if (user.Status == 2)
            {
                return StatusCode(403, new { message = "Hesabınız yasaklanmıştır." });
            }

            bool hasActiveBan = await _context.UserBans
                .AsNoTracking()
                .AnyAsync(b => b.UserId == user.Id
                          && b.UnbannedAt == null
                          && (b.BanUntil == null || b.BanUntil > DateTime.UtcNow));

            if (hasActiveBan)
            {
                if (user.Status != 2)
                {
                    user.Status = 2;
                    await _context.SaveChangesAsync();
                }

                return StatusCode(403, new { message = "Hesabınızda aktif bir yasaklama bulunmaktadır. Oturum açılamaz." });
            }

            var token = GenerateJwtToken(user);

            return Ok(new { token = token });
        }

        // GET: api/auth/my-profile
        [Authorize]
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                nickname = user.UserProfile?.Nickname,
                realName = user.UserProfile?.RealName,
                bio = user.UserProfile?.Bio,
                status = user.Status,
                role = role
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
                throw new Exception("JWT SecretKey konfigürasyonu eksik!");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleId == 0 ? "Admin" : "User")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}