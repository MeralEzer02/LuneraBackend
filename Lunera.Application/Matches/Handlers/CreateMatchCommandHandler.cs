using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Lunera.Application.Abstractions.Repositories;
using Lunera.Application.Abstractions.Services;
using Lunera.Application.Matches.Commands;
using Lunera.Domain.Entities;

namespace Lunera.Application.Matches.Handlers
{
    public class CreateMatchCommandHandler : IRequestHandler<CreateMatchCommand, int>
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IClock _clock;

        public CreateMatchCommandHandler(IMatchRepository matchRepository, IClock clock)
        {
            _matchRepository = matchRepository;
            _clock = clock;
        }

        public async Task<int> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
        {
            // 1. Domain nesnesini yarat (İş kuralları ve Invariant'lar burada çalışır)
            // Not: Süre sınırını şimdilik sabit 24 saat veriyoruz.
            var match = Match.Create(request.UserAId, request.UserBId, 24, _clock.UtcNow);

            // 2. Veritabanına ekle
            await _matchRepository.AddAsync(match, cancellationToken);

            // 3. Transaction'ı tamamla (Unit of Work)
            await _matchRepository.SaveChangesAsync(cancellationToken);

            // 4. Oluşan yeni Eşleşmenin ID'sini dış dünyaya (API'ye) döndür
            return match.Id;
        }
    }
}