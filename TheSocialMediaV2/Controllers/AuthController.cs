using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TheSocialMediaV2.Data;
using TheSocialMediaV2.DTOs;
using TheSocialMediaV2.Entities;
using TheSocialMediaV2.Enums;
using TheSocialMediaV2.Utilities;

namespace TheSocialMediaV2.Controllers
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
            // 1. Email kontrolü (Aynı maille kayıt olunamaz)
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Bu e-posta adresi zaten kullanımda.");
            }

            // 2. Nickname kontrolü (Opsiyonel ama iyi bir pratiktir)
            if (await _context.UserProfiles.AnyAsync(p => p.Nickname == request.Nickname))
            {
                return BadRequest("Bu takma ad başkası tarafından alınmış.");
            }

            // 3. Cinsiyet dönüşümü (String -> Enum)
            if (!Enum.TryParse(request.Gender, true, out Gender genderEnum))
            {
                return BadRequest("Geçersiz cinsiyet değeri. (Valid: Male, Female, NonBinary)");
            }

            // 4. Transaction başlat (İki tabloya aynı anda kayıt atacağız)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // A) User Tablosuna Kayıt
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = PasswordHasher.HashPassword(request.Password),
                    RoleId = 1, // 1: User (Varsayılan)
                    Status = 1, // 1: Active
                    CreatedAt = DateTime.Now,
                    WarningCount = 0
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // User ID oluşsun diye önce kaydediyoruz

                // B) UserProfile Tablosuna Kayıt
                var profile = new UserProfile
                {
                    UserId = user.Id, // Oluşan ID'yi al
                    RealName = request.RealName,
                    Nickname = request.Nickname,
                    Gender = genderEnum,
                    BirthDate = request.BirthDate,
                    Bio = request.Bio ?? string.Empty,
                    IdentityVisible = false
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                // Her şey yolunda, onayla
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
            // 1. Kullanıcıyı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("E-posta veya şifre hatalı.");
            }

            // 2. Şifreyi doğrula
            var inputHash = PasswordHasher.HashPassword(request.Password);
            if (user.PasswordHash != inputHash)
            {
                return Unauthorized("E-posta veya şifre hatalı.");
            }

            // 3. Status kontrolü (Banlı mı?)
            // Status Enum kullanmıyoruz (şimdilik int), 2: Banned kabul edelim
            if (user.Status == 2)
            {
                return StatusCode(403, "Hesabınız yasaklanmıştır.");
            }

            // 4. Token Üret
            var token = GenerateJwtToken(user);

            return Ok(new { token = token });
        }

        // Yardımcı Metot: Token Üretici
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            // Güvenlik kontrolü (SecretKey null gelirse patlamasın)
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
                expires: DateTime.Now.AddDays(7), // Token 7 gün geçerli
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}