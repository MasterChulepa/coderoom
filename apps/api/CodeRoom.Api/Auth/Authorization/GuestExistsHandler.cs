
using System.Security.Claims;
using CodeRoom.Api.Auth.Services;
using Microsoft.AspNetCore.Authorization;

namespace CodeRoom.Api.Auth.Authorization;

public sealed class GuestExistsHandler(IAuthService _authService) : AuthorizationHandler<GuestExistsRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GuestExistsRequirement requirement)
    {
        string? guestIdClaim;
        var user = context.User;
        var cancellationToken = CancellationToken.None;
        if (user.Identity?.IsAuthenticated != true) return;
        guestIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(guestIdClaim, out var guestId) && await _authService.GuestExistsAsync(guestId, cancellationToken))
        {
            context.Succeed(requirement);
        }
    }
}