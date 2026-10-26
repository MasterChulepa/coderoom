using CodeRoom.Api.Data;
using CodeRoom.Api.ProjectFiles.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace CodeRoom.Api.Collaboration.Services;

public class CollaborativeDocumentService(CodeRoomDbContext _dbContext) : ICollaborativeDocumentService
{
    public async Task AppendUpdateAsync(int projectId, int fileId, byte[] update, CancellationToken cancellationToken)
    {
        await CheckIfFileExistInProject(projectId, fileId, cancellationToken);
        var updateFile = new CollaborativeDocumentUpdate
        {
            FileId = fileId,
            Update = update,
        };
        _dbContext.CollaborativeDocumentUpdates.Add(updateFile);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
        && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation && pgEx.ConstraintName == "FK_CollaborativeDocumentUpdates_ProjectFiles_FileId")
        {
            throw new ProjectFileNotFoundException(fileId.ToString(), ex);
        }
    }


    public async Task<IReadOnlyList<byte[]>> GetUpdatesAsync(int projectId, int fileId, CancellationToken cancellationToken)
    {
        await CheckIfFileExistInProject(projectId, fileId, cancellationToken);
        return await _dbContext.CollaborativeDocumentUpdates.Where(u => u.FileId == fileId)
        .OrderBy(u => u.Id).Select(u => u.Update).ToArrayAsync(cancellationToken);
    }

    private async Task CheckIfFileExistInProject(int projectId, int fileId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.ProjectFiles.AnyAsync(pf => pf.ProjectId == projectId && pf.Id == fileId, cancellationToken))
            throw new ProjectFileNotFoundException(fileId.ToString());
    }
}