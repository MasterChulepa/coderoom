using Microsoft.AspNetCore.Authorization;

namespace CodeRoom.Api.Auth.Authorization;

public sealed class GuestExistsRequirement : IAuthorizationRequirement
{
}