using Identity.Api.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Identity.Api.Infrastructure.Authentication;

public interface IJwtService
{
    string GenerateAccessToken(
        User user,
        IEnumerable<string> roles);

    DateTime GetAccessTokenExpiration();
}