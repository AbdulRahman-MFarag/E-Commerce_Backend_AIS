using FluentValidation;
using Identity.Api.Common.DTOs.Auth;

namespace Identity.Api.Features.Auth.RevokeToken;

public class RevokeTokenValidator
    : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}