namespace CodeRoom.Api.Auth.Services;

public interface IAuthService
{
    Task<int> CreateGuestAsync(CancellationToken cancellationToken);
}