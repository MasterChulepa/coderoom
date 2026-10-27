namespace CodeRoom.Api.Collaboration.Exceptions;

public class ConflictWhileSavingUpdateException(int fileId, Guid updateId, Exception? innerException = null) : Exception(
    $"Update ID '{updateId}' for file '{fileId}' is already associated with different content.", innerException
)
{
}