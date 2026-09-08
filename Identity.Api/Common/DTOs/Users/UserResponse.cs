using Identity.Api.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Identity.Api.Common.DTOs.Users;

public class UserResponse
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public UserStatus Status { get; set; }

    public List<string> Roles { get; set; } = new();
}