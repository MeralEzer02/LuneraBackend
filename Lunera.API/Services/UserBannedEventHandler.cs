using Microsoft.EntityFrameworkCore;
using Lunera.API.Data;
using Lunera.Domain.Enums;
using Lunera.Domain.Entities;
using Lunera.Domain.Events;

namespace Lunera.API.Services
{
    public class UserBannedEventHandler : IInternalDomainEventHandler<UserBannedEvent>
    {
        private readonly AppDbContext _context;

        public UserBannedEventHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UserBannedEvent domainEvent)
        {
            bool isAlreadyProcessed = await _context.ProcessedEvents
                .AnyAsync(e => e.EventId == domainEvent.EventId);

            if (isAlreadyProcessed)
            {
                return;
            }

            var metric = await _context.UserAbuseMetrics
                .FirstOrDefaultAsync(m => m.UserId == domainEvent.UserId);

            if (metric == null)
            {
                metric = new UserAbuseMetric { UserId = domainEvent.UserId };
                _context.UserAbuseMetrics.Add(metric);
            }

            metric.TotalBans++;
            metric.LastBanDate = domainEvent.OccurredOn;
            metric.AbuseScore += 20;
            metric.RiskLevel = CalculateRiskLevel(metric.AbuseScore, metric.TotalBans);
            metric.LastUpdated = DateTime.UtcNow;

            _context.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = domainEvent.EventId,
                EventType = nameof(UserBannedEvent),
                ProcessedAt = DateTime.UtcNow
            });

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