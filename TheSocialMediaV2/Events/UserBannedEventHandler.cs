using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Enums;

namespace TheSocialMediaV2.API.Events
{
    public class UserBannedEventHandler : IDomainEventHandler<UserBannedEvent>
    {
        private readonly AppDbContext _context;

        public UserBannedEventHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UserBannedEvent domainEvent)
        {
            // 1. Kullanıcının Metric kaydını bul veya oluştur
            var metric = await _context.UserAbuseMetrics
                .FirstOrDefaultAsync(m => m.UserId == domainEvent.UserId);

            if (metric == null)
            {
                metric = new UserAbuseMetric { UserId = domainEvent.UserId };
                _context.UserAbuseMetrics.Add(metric);
            }

            // 2. İstatistikleri Güncelle
            metric.TotalBans++;
            metric.LastBanDate = domainEvent.OccurredOn;

            // 3. Skor Hesaplama (Basit bir algoritma)
            // Her ban 20 puan risk artırır
            metric.AbuseScore += 20;

            // 4. Risk Seviyesi Belirleme (Escalation Policy)
            metric.RiskLevel = CalculateRiskLevel(metric.AbuseScore, metric.TotalBans);

            metric.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private RiskLevel CalculateRiskLevel(int score, int totalBans)
        {
            if (totalBans >= 3 || score >= 100) return RiskLevel.Critical;
            if (totalBans >= 1 || score >= 50) return RiskLevel.High;
            if (score >= 20) return RiskLevel.Medium;
            return RiskLevel.Low;
        }
    }
}