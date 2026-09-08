using FluentValidation;
using Identity.Api.Common.DTOs.Auth;

namespace Identity.Api.Features.Auth.RefreshToken;

public class RefreshTokenValidator
    : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}