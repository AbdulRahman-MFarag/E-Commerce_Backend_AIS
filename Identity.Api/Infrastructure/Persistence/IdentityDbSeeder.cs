using Identity.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Identity.Api.Infrastructure.Persistence;

public static class IdentityDbSeeder
{
	public static async Task SeedAsync(
		IdentityDbContext context)
	{
		if (await context.Roles.AnyAsync())
		{
			return;
		}

		var customerRole = new Role
		{
			RoleId = Guid.NewGuid(),
			Name = "Customer"
		};

		var adminRole = new Role
		{
			RoleId = Guid.NewGuid(),
			Name = "Admin"
		};

		context.Roles.AddRange(
			customerRole,
			adminRole);

		await context.SaveChangesAsync();
	}
}