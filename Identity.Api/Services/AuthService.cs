using Identity.Api.Common.DTOs.Auth;
using Identity.Api.Common.DTOs.Users;
using Identity.Api.Common.Exceptions;
using Identity.Api.Domain.Entities;
using Identity.Api.Domain.Enums;
using Identity.Api.Infrastructure.Authentication;
using Identity.Api.Infrastructure.Persistence;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IdentityDbContext context,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request)
    {
        var email =
            request.Email.Trim().ToLowerInvariant();

        var exists =
            await _context.Users
                .AnyAsync(x => x.Email == email);

        if (exists)
        {
            throw new ConflictException(
                "A user with this email already exists.");
        }

        var customerRole =
            await _context.Roles
                .FirstOrDefaultAsync(
                    x => x.Name == "Customer");

        if (customerRole == null)
        {
            throw new InvalidOperationException(
                "Customer role is not configured.");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),

            Email = email,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.Password),

            FirstName = request.FirstName.Trim(),

            LastName = request.LastName.Trim(),

            Status = UserStatus.Active,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = customerRole.RoleId
        };

        _context.Users.Add(user);

        _context.UserRoles.Add(userRole);

        await _context.SaveChangesAsync();

        user.UserRoles.Add(
            new UserRole
            {
                UserId = user.UserId,
                RoleId = customerRole.RoleId,
                Role = customerRole
            });

        return user.Adapt<UserResponse>();
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request)
    {
        var email =
            request.Email.Trim().ToLowerInvariant();

        var user =
            await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.Email == email);

        if (user == null)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedException(
                "This account is not active.");
        }

        var passwordValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!passwordValid)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        var roles =
            user.UserRoles
                .Select(x => x.Role.Name)
                .ToList();

        var accessToken =
            _jwtService.GenerateAccessToken(
                user,
                roles);

        var accessTokenExpiration =
            _jwtService.GetAccessTokenExpiration();

        var refreshToken =
            GenerateRefreshToken();

        var refreshExpiration =
            DateTime.UtcNow.AddDays(
                GetRefreshTokenExpirationDays());

        var refreshTokenEntity =
            new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),

                UserId = user.UserId,

                TokenHash =
                    HashToken(refreshToken),

                CreatedAt = DateTime.UtcNow,

                ExpiresAt = refreshExpiration
            };

        _context.RefreshTokens.Add(
            refreshTokenEntity);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,

            RefreshToken = refreshToken,

            AccessTokenExpiresAt =
                accessTokenExpiration,

            RefreshTokenExpiresAt =
                refreshExpiration,

            User = user.Adapt<UserResponse>()
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request)
    {
        var tokenHash =
            HashToken(request.RefreshToken);

        var storedToken =
            await _context.RefreshTokens
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.TokenHash == tokenHash);

        if (storedToken == null)
        {
            throw new UnauthorizedException(
                "Invalid refresh token.");
        }

        if (storedToken.RevokedAt != null)
        {
            throw new UnauthorizedException(
                "Refresh token has already been revoked.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException(
                "Refresh token has expired.");
        }

        var user = storedToken.User;

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedException(
                "This account is not active.");
        }

        // Rotate refresh token
        storedToken.RevokedAt =
            DateTime.UtcNow;

        var roles =
            user.UserRoles
                .Select(x => x.Role.Name)
                .ToList();

        var accessToken =
            _jwtService.GenerateAccessToken(
                user,
                roles);

        var accessExpiration =
            _jwtService.GetAccessTokenExpiration();

        var newRefreshToken =
            GenerateRefreshToken();

        var refreshExpiration =
            DateTime.UtcNow.AddDays(
                GetRefreshTokenExpirationDays());

        var newEntity =
            new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),

                UserId = user.UserId,

                TokenHash =
                    HashToken(newRefreshToken),

                CreatedAt = DateTime.UtcNow,

                ExpiresAt = refreshExpiration
            };

        _context.RefreshTokens.Add(newEntity);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,

            RefreshToken = newRefreshToken,

            AccessTokenExpiresAt =
                accessExpiration,

            RefreshTokenExpiresAt =
                refreshExpiration,

            User = user.Adapt<UserResponse>()
        };
    }

    public async Task RevokeTokenAsync(
        RevokeTokenRequest request)
    {
        var tokenHash =
            HashToken(request.RefreshToken);

        var token =
            await _context.RefreshTokens
                .FirstOrDefaultAsync(
                    x => x.TokenHash == tokenHash);

        if (token == null)
        {
            return;
        }

        if (token.RevokedAt == null)
        {
            token.RevokedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    private static string GenerateRefreshToken()
    {
        var bytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(
        string token)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }

    private int GetRefreshTokenExpirationDays()
    {
        return int.Parse(
            _configuration
                .GetSection("Jwt")
                ["RefreshTokenExpirationDays"]
            ?? "7");
    }
}