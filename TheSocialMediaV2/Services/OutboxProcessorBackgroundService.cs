using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Domain.Events;

namespace TheSocialMediaV2.API.Services
{
    public class OutboxProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessorBackgroundService> _logger;
        private const int BatchSize = 20;

        public OutboxProcessorBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Worker BAŞLADI.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Outbox Processor'da beklenmeyen KRİTİK HATA!");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IInternalDomainEventDispatcher>();

            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount <= 5)
                .OrderBy(m => m.OccurredOnUtc)
                .ThenBy(m => m.Id)
                .Take(BatchSize)
                .ToListAsync(stoppingToken);

            if (!messages.Any()) return;

            foreach (var message in messages)
            {
                try
                {
                    var eventType = typeof(MatchAcceptedEvent).Assembly.GetType(message.Type);
                    if (eventType == null) throw new InvalidOperationException($"Tip çözülemedi: {message.Type}");

                    var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType) as IInternalDomainEvent;
                    if (domainEvent == null) throw new InvalidOperationException("Payload geçersiz.");

                    await dispatcher.Dispatch((dynamic)domainEvent);

                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                    _logger.LogError(ex, "Mesaj hatası. ID: {Id}, Retry: {Retry}", message.Id, message.RetryCount);
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}