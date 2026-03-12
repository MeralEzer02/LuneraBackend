using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.Domain.Entities;

namespace TheSocialMediaV2.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugMatchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public DebugMatchController(AppDbContext context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }


        // ADIM 1: GERÇEK ROW VERSION CONFLICT TESTİ (2 Context)
        [HttpPost("test-real-occ/{userA}/{userB}")]
        public async Task<IActionResult> TestRealOCC(int userA, int userB)
        {
            var logs = new List<string>();

            // 1. HAZIRLIK: Temiz bir Pending Match oluştur
            int matchId;
            using (var scopeInit = _scopeFactory.CreateScope())
            {
                var dbInit = scopeInit.ServiceProvider.GetRequiredService<AppDbContext>();

                var match = Match.Create(userA, userB, 24, DateTime.UtcNow);

                dbInit.Matches.Add(match);
                await dbInit.SaveChangesAsync();
                matchId = match.Id;
                logs.Add($"1. Match Oluşturuldu (ID: {matchId}, Version: {Convert.ToBase64String(match.RowVersion)})");
            }

            // 2. SİMÜLASYON: İki Ayrı Context Yarat (Context A ve Context B)
            using var scopeA = _scopeFactory.CreateScope();
            using var scopeB = _scopeFactory.CreateScope();

            var contextA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
            var contextB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

            // 3. YÜKLEME: İkisi de aynı kaydı (v1) yüklüyor
            var matchA = await contextA.Matches.FindAsync(matchId);
            var matchB = await contextB.Matches.FindAsync(matchId);

            logs.Add("2. Context A ve Context B veriyi yükledi.");

            // 4. DEĞİŞİKLİK: İkisi de geçerli (Valid) hamleler yapıyor
            // Pending -> Accepted (Valid)

            matchA.Accept(DateTime.UtcNow);
            logs.Add("3. Context A: Accept() çağırdı.");

            // Pending -> Cancelled (Valid)

            matchB.Cancel(DateTime.UtcNow);
            logs.Add("4. Context B: Cancel() çağırdı.");

            try
            {
                // 5. KAYIT A: Başarılı olmalı
                await contextA.SaveChangesAsync();
                logs.Add($"5. Context A: SaveChanges() BAŞARILI. (Yeni Durum: {matchA.Status})");

                // Bu noktada DB'deki RowVersion değişti!
            }
            catch (Exception ex)
            {
                return BadRequest($"HATA: Context A kaydetmeliydi ama hata aldı: {ex.Message}");
            }

            try
            {
                await contextB.SaveChangesAsync();

                return BadRequest("HATA: Context B kaydetmeyi başardı! (OCC ÇALIŞMIYOR - Last Write Wins oldu)");
            }
            catch (DbUpdateConcurrencyException)
            {
                logs.Add("6. Context B: DbUpdateConcurrencyException YAKALANDI! ✅");
                logs.Add("SONUÇ: Test BAŞARILI. İkinci işlem reddedildi, veri ezilmedi.");
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest($"HATA: Yanlış Exception tipi: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}