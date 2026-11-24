using CodeRoom.Api.Auth.Models;
using CodeRoom.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace CodeRoom.Api.Auth.Services;

public class AuthService(CodeRoomDbContext _dbContext) : IAuthService
{
    public async Task<int> CreateGuestAsync(CancellationToken cancellationToken)
    {
        var guest = new Guest();
        _dbContext.Guests.Add(guest);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return guest.Id;
    }
}