using FluentValidation;
using Lunera.Application.Matches.DTOs;

namespace Lunera.Application.Matches.Validators
{
    public class AcceptMatchValidator : AbstractValidator<AcceptMatchRequest>
    {
        public AcceptMatchValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId boş olamaz.")
                .GreaterThan(0).WithMessage("Geçerli bir UserId girilmelidir.");
        }
    }
}