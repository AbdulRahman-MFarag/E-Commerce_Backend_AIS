using Identity.Api.Common.DTOs.Users;
using Identity.Api.Domain.Entities;
using Mapster;

namespace Identity.Api.Mapping;

public static class MappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<User, UserResponse>
            .NewConfig()
            .Map(
                dest => dest.Roles,
                src => src.UserRoles
                    .Select(x => x.Role.Name)
                    .ToList());
    }
}