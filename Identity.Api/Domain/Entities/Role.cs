using System;
using System.Collections.Generic;

namespace Identity.Api.Domain.Entities;

public class Role
{
    public Guid RoleId { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();
}