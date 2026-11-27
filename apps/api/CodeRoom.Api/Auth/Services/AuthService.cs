using CodeRoom.Api.Auth.Models;
using CodeRoom.Api.Data;

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

    public async Task<bool> GuestExistsAsync(int id, CancellationToken cancellationToken)
    {
        var guest = await _dbContext.Guests.FindAsync(id, cancellationToken);
        return guest is not null;
    }
}