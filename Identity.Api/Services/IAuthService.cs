using Identity.Api.Common.DTOs.Auth;
using Identity.Api.Common.DTOs.Users;
using System.Threading.Tasks;

namespace Identity.Api.Services.Auth;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(
        RegisterRequest request);

    Task<AuthResponse> LoginAsync(
        LoginRequest request);

    Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request);

    Task RevokeTokenAsync(
        RevokeTokenRequest request);
}