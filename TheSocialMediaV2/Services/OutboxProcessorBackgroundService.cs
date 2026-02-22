using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Events;

namespace TheSocialMediaV2.API.Services
{
    public class OutboxProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessorBackgroundService> _logger;
        private const int BatchSize = 20; // Tek seferde işlenecek maksimum mesaj

        public OutboxProcessorBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Worker BAŞLADI.");

            // Graceful Shutdown loop'u
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Döngünün tamamen çökmesini engellemek için global catch
                    _logger.LogCritical(ex, "Outbox Processor'da beklenmeyen KRİTİK HATA!");
                }

                // Döngü arası bekleme (Delay Configurable)
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }

            _logger.LogInformation("Outbox Processor Worker DURDURULDU.");
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            // Her batch için yeni bir Scope (Memory Leak ve Concurrency'yi önler)
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IInternalDomainEventDispatcher>();

            // 1. FETCH STRATEGY
            // İşlenmemiş (ProcessedOnUtc == null) ve Zehirli Olmayan (RetryCount <= 5)
            // En eski (OccurredOnUtc ASC) BatchSize kadar mesajı getir.
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount <= 5)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BatchSize)
                .ToListAsync(stoppingToken);

            if (!messages.Any()) return; // İşlenecek mesaj yoksa çık

            // 2. PROCESS FLOW
            foreach (var message in messages)
            {
                try
                {
                    // A. Type Resolution (Hangi event sınıfı?)
                    var eventType = Type.GetType(message.Type);
                    if (eventType == null)
                    {
                        throw new InvalidOperationException($"Event tipi bulunamadı: {message.Type}");
                    }

                    // B. Deserialize
                    var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }) as IInternalDomainEvent;

                    if (domainEvent == null)
                        throw new InvalidOperationException("Payload deserialize edilemedi veya IInternalDomainEvent değil.");

                    // C. Publish
                    await dispatcher.Dispatch((dynamic)domainEvent);

                    // D. Başarı Durumu (Success)
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;
                }
                catch (Exception ex)
                {
                    // E. HATA YÖNETİMİ (Failure Handling)
                    message.RetryCount++;
                    message.Error = ex.Message;

                    _logger.LogError(ex, "Outbox Message işlenirken hata oluştu. ID: {Id}, Retry: {RetryCount}", message.Id, message.RetryCount);

                    // Poison Message Isolation
                    if (message.RetryCount > 5)
                    {
                        _logger.LogCritical("POISON MESSAGE TESPİT EDİLDİ! ID: {Id} işlem kuyruğundan kalıcı olarak çıkarıldı.", message.Id);
                    }
                }
            }

            // 3. BATCH SAVE (Tüm state değişikliklerini tek seferde DB'ye yaz)
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}