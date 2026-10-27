namespace CodeRoom.Api.Collaboration.Services;

public interface ICollaborativeDocumentService
{
    Task<IReadOnlyList<byte[]>> GetUpdatesAsync(int projectId, int fileId, CancellationToken cancellationToken);
    Task AppendUpdateAsync(int projectId, int fileId, Guid updateId, byte[] update, CancellationToken cancellationToken);
}