using FluentValidation;
using Identity.Api.Common.DTOs.Auth;

namespace Identity.Api.Features.Auth.Register;

public class RegisterValidator
	: AbstractValidator<RegisterRequest>
{
	public RegisterValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress()
			.MaximumLength(255);

		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(8)
			.MaximumLength(100)
			.Matches("[A-Z]")
			.WithMessage(
				"Password must contain at least one uppercase letter.")
			.Matches("[a-z]")
			.WithMessage(
				"Password must contain at least one lowercase letter.")
			.Matches("[0-9]")
			.WithMessage(
				"Password must contain at least one number.");

		RuleFor(x => x.FirstName)
			.NotEmpty()
			.MaximumLength(100);

		RuleFor(x => x.LastName)
			.NotEmpty()
			.MaximumLength(100);
	}
}