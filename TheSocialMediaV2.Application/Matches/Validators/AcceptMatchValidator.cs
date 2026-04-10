using FluentValidation;
using TheSocialMediaV2.Application.Matches.DTOs;

namespace TheSocialMediaV2.Application.Matches.Validators
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