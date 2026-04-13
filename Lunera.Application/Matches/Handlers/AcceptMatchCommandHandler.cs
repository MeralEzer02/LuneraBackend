using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Lunera.Application.Abstractions.Repositories;
using Lunera.Application.Abstractions.Services;
using Lunera.Application.Exceptions;
using Lunera.Application.Matches.Commands;
using Lunera.Domain.Enums;

namespace Lunera.Application.Matches.Handlers
{
    public class AcceptMatchCommandHandler : IRequestHandler<AcceptMatchCommand, Unit>
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IClock _clock;

        public AcceptMatchCommandHandler(IMatchRepository matchRepository, IClock clock)
        {
            _matchRepository = matchRepository;
            _clock = clock;
        }

        public async Task<Unit> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
        {
            var match = await _matchRepository.GetByIdAsync(request.MatchId, cancellationToken);

            if (match == null)
            {
                throw new NotFoundException($"Eşleşme bulunamadı: {request.MatchId}");
            }

            if (match.UserAId != request.UserId && match.UserBId != request.UserId)
            {
                throw new InvalidOperationException("Bu eşleşmeyi kabul etme yetkiniz yok.");
            }

            if (match.Status == MatchStatus.Accepted)
            {
                return Unit.Value;
            }

            match.Accept(_clock.UtcNow);

            try
            {
                await _matchRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                var currentMatch = await _matchRepository.GetByIdAsync(request.MatchId, cancellationToken);

                if (currentMatch != null && currentMatch.Status == MatchStatus.Accepted)
                {
                    return Unit.Value;
                }

                throw new InvalidOperationException("Bu eşleşmenin durumu bir başkası tarafından değiştirilmiş.");
            }

            return Unit.Value;
        }
    }
}