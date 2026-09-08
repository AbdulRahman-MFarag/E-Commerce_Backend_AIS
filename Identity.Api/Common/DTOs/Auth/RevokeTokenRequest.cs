namespace Identity.Api.Common.DTOs.Auth;

public class RevokeTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}