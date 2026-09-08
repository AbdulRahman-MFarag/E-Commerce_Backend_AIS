using Identity.Api.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Identity.Api.Domain.Entities;

public class User
{
	public Guid UserId { get; set; }

	public string Email { get; set; } = null!;

	public string PasswordHash { get; set; } = null!;

	public string FirstName { get; set; } = null!;

	public string LastName { get; set; } = null!;

	public UserStatus Status { get; set; } = UserStatus.Active;

	public DateTime CreatedAt { get; set; }

	public DateTime UpdatedAt { get; set; }

	public ICollection<UserRole> UserRoles { get; set; }
		= new List<UserRole>();

	public ICollection<RefreshToken> RefreshTokens { get; set; }
		= new List<RefreshToken>();
}