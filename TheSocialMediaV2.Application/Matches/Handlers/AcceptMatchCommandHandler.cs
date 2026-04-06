using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using TheSocialMediaV2.Application.Abstractions.Repositories;
using TheSocialMediaV2.Application.Abstractions.Services;
using TheSocialMediaV2.Application.Exceptions; // EKLENDİ: Özel hata sınıfımız
using TheSocialMediaV2.Application.Matches.Commands;
using TheSocialMediaV2.Domain.Enums; // EKLENDİ: MatchStatus için

namespace TheSocialMediaV2.Application.Matches.Handlers
{
    public class AcceptMatchCommandHandler : IRequestHandler<AcceptMatchCommand>
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IClock _clock;

        public AcceptMatchCommandHandler(IMatchRepository matchRepository, IClock clock)
        {
            _matchRepository = matchRepository;
            _clock = clock;
        }

        public async Task Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
        {
            var match = await _matchRepository.GetByIdAsync(request.MatchId, cancellationToken);

            if (match == null)
            {
                throw new NotFoundException($"Eşleşme bulunamadı: {request.MatchId}");
            }

            if (match.Status == MatchStatus.Accepted)
            {
                return;
            }

            match.Accept(_clock.UtcNow);

            await _matchRepository.SaveChangesAsync(cancellationToken);
        }
    }
}