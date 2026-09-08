using Asp.Versioning;
using Identity.Api.Common.DTOs.Auth;
using Identity.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        var result =
            await _authService.RegisterAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var result =
            await _authService.LoginAsync(request);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request)
    {
        var result =
            await _authService.RefreshTokenAsync(request);

        return Ok(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        RevokeTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request);

        return NoContent();
    }
}