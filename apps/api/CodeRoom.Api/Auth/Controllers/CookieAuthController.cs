using System.Security.Claims;
using CodeRoom.Api.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CodeRoom.Api.Auth.Controllers;

[ApiController]
[Route("/api/guest-session")]

public class CookieAuthController(IAuthService authService) : ControllerBase
{
    [HttpPost(Name = "SignInGuest")]
    public async Task<IActionResult> EnsureGuestSessionAsync(CancellationToken cancellationToken)
    {

        if (User.Identity?.IsAuthenticated == true)
        {
            var guestIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(guestIdClaim, out var guestId)
                && await authService.GuestExistsAsync(guestId, cancellationToken))
            {
                return NoContent();
            }

        }
        await SignInNewGuestAsync(cancellationToken);
        return NoContent();
    }

    private async Task SignInNewGuestAsync(CancellationToken cancellationToken)
    {
        var guestId = await authService.CreateGuestAsync(cancellationToken);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guestId.ToString()),
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties { };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
         new ClaimsPrincipal(claimsIdentity), authProperties);
    }

}