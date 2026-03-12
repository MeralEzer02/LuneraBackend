using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.DTOs;
using TheSocialMediaV2.Domain.Utilities;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Domain.Enums;

namespace TheSocialMediaV2.API.Controllers
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
                    CreatedAt = DateTime.UtcNow, // Best Practice: UtcNow
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
            // 1. Kullanıcıyı Bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("E-posta veya şifre hatalı.");
            }

            // 2. Şifre Kontrolü
            var inputHash = PasswordHasher.HashPassword(request.Password);
            if (user.PasswordHash != inputHash)
            {
                return Unauthorized("E-posta veya şifre hatalı.");
            }

            // 3. Basit Status Kontrolü (Legacy)
            if (user.Status == 2)
            {
                return StatusCode(403, new { message = "Hesabınız yasaklanmıştır." });
            }

            // 4. 🛑 KRİTİK GÜVENLİK KONTROLÜ (A3.3 - Deep Ban Check)
            // Token üretmeden önce, veritabanında aktif ve kapanmamış bir ban dosyası var mı?
            // Bu kontrol, Admin panelinden atılan süreli banların Login anında yakalanmasını sağlar.
            bool hasActiveBan = await _context.UserBans
                .AsNoTracking()
                .AnyAsync(b => b.UserId == user.Id
                          && b.UnbannedAt == null
                          && (b.BanUntil == null || b.BanUntil > DateTime.UtcNow));

            if (hasActiveBan)
            {
                // Eğer status güncel değilse onu da düzeltelim (Self-Healing)
                if (user.Status != 2)
                {
                    user.Status = 2;
                    await _context.SaveChangesAsync();
                }

                return StatusCode(403, new { message = "Hesabınızda aktif bir yasaklama bulunmaktadır. Oturum açılamaz." });
            }

            // 5. Her şey temizse Token ver
            var token = GenerateJwtToken(user);

            return Ok(new { token = token });
        }

        // GET: api/auth/my-profile
        [Authorize]
        [HttpGet("my-profile")]
        public IActionResult GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "Tebrikler! Bu özel alana girdiniz.",
                id = userId,
                email = email,
                role = role,
                serverTime = DateTime.UtcNow
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
                expires: DateTime.UtcNow.AddDays(7), // Best Practice: UtcNow
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}