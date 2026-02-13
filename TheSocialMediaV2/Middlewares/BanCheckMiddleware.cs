using System.Security.Claims;
using TheSocialMediaV2.API.Data;

namespace TheSocialMediaV2.API.Middlewares
{
    public class BanCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public BanCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Kullanıcı giriş yapmış mı? (Token var mı?)
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                // 2. UserId'yi al
                var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdStr, out int userId))
                {
                    // 3. Veritabanına sor: "Bu adamın aktif banı var mı?"
                    // Scoped servis olduğu için HttpContext üzerinden Context'i alıyoruz.
                    var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();

                    // SORGULAMA MANTIĞI (Entity'deki IsActive'in SQL karşılığı):
                    // - UnbannedAt NULL OLMALI (Af çıkmamış)
                    // - VE (Süresiz Ban VEYA Süresi Dolmamış Ban)
                    bool isBanned = dbContext.UserBans.Any(b =>
                        b.UserId == userId &&
                        b.UnbannedAt == null &&
                        (b.BanUntil == null || b.BanUntil > DateTime.UtcNow)
                    );

                    if (isBanned)
                    {
                        // 4. BANLIYSA DURDUR!
                        context.Response.StatusCode = 403; // Forbidden
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"message\": \"Hesabınız yasaklanmıştır. Erişiminiz engellendi.\"}");
                        return; // Zinciri kır, Controller'a gitme.
                    }
                }
            }

            // 5. Temizse devam et
            await _next(context);
        }
    }
}