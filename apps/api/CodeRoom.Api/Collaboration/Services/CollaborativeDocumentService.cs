using CodeRoom.Api.Collaboration.Exceptions;
using CodeRoom.Api.Data;
using CodeRoom.Api.ProjectFiles.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace CodeRoom.Api.Collaboration.Services;

public class CollaborativeDocumentService(CodeRoomDbContext _dbContext) : ICollaborativeDocumentService
{
    public async Task AppendUpdateAsync(int projectId, int fileId, Guid updateId, byte[] update, CancellationToken cancellationToken)
    {
        if (updateId == Guid.Empty) throw new InvalidUpdateIdException();
        await CheckIfFileExistInProject(projectId, fileId, cancellationToken);
        var updateFile = new CollaborativeDocumentUpdate
        {
            FileId = fileId,
            Update = update,
            UpdateId = updateId
        };
        _dbContext.CollaborativeDocumentUpdates.Add(updateFile);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
        && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation && pgEx.ConstraintName == "FK_CollaborativeDocumentUpdates_ProjectFiles_FileId")
        {
            _dbContext.Entry(updateFile).State = EntityState.Detached;
            throw new ProjectFileNotFoundException(fileId.ToString(), ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
        && pgEx.SqlState == PostgresErrorCodes.UniqueViolation && pgEx.ConstraintName == "IX_CollaborativeDocumentUpdates_FileId_UpdateId")
        {
            _dbContext.Entry(updateFile).State = EntityState.Detached;
            var existedUpdate = await _dbContext.CollaborativeDocumentUpdates.AsNoTracking()
                                .FirstOrDefaultAsync(u => u.FileId == fileId && u.UpdateId == updateId, cancellationToken);
            if (existedUpdate is null) throw;
            if (!existedUpdate.Update.SequenceEqual(update))
            {
                throw new ConflictWhileSavingUpdateException(fileId, updateId, ex);
            }
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