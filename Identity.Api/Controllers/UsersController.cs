using Asp.Versioning;
using Identity.Api.Common.DTOs.Users;
using Identity.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IdentityDbContext _context;

    public UsersController(
        IdentityDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out var userId))
        {
            return Unauthorized();
        }

        var user =
            await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        var response = new UserResponse
        {
            UserId = user.UserId,

            Email = user.Email,

            FirstName = user.FirstName,

            LastName = user.LastName,

            Status = user.Status,

            Roles = user.UserRoles
                .Select(x => x.Role.Name)
                .ToList()
        };

        return Ok(response);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminTest()
    {
        return Ok(new
        {
            message = "Admin access granted."
        });
    }
}