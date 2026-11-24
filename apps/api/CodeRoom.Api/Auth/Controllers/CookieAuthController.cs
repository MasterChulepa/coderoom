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
    public async Task<IActionResult> SignInGuestAsync(CancellationToken cancellationToken)
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
        return NoContent();
    }
}