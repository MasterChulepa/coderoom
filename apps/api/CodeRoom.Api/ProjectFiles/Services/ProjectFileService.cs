using CodeRoom.Api.Data;
using Microsoft.EntityFrameworkCore;
using CodeRoom.Api.ProjectFiles.Dtos;
using Npgsql;
using CodeRoom.Api.ProjectFiles.Exceptions;
using Microsoft.AspNetCore.SignalR;
using CodeRoom.Api.Collaboration;

namespace CodeRoom.Api.ProjectFiles.Services;

public class ProjectFileService(CodeRoomDbContext _dbContext, IHubContext<ProjectHub> _hubContext, ILogger<ProjectFileService> _logger) : IProjectFileService
{
    public async Task<ProjectFileResponse[]> GetProjectFilesAsync(int projectId, CancellationToken cancellationToken)
    {
        var projectFiles = await _dbContext.ProjectFiles.AsNoTracking()
            .Where(pf => pf.ProjectId == projectId)
            .ToArrayAsync(cancellationToken);
        return [.. projectFiles.Select(pf => new ProjectFileResponse
        {
            Id = pf.Id,
            Content = pf.Content,
            Path = pf.Path,
            ProjectId = pf.ProjectId
        })];
    }

    public async Task<ProjectFileResponse?> CreateProjectFileAsync(int projectId, CreateProjectFileRequest request, CancellationToken cancellationToken)
    {
        var projectFile = new ProjectFile
        {
            Content = request.Content,
            Path = request.Path,
            ProjectId = projectId
        };

        if (!await _dbContext.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            return null;
        }
        _dbContext.ProjectFiles.Add(projectFile);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation && pgEx.ConstraintName == "FK_ProjectFiles_Projects_ProjectId")
        {
            return null;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation && pgEx.ConstraintName == "IX_ProjectFiles_ProjectId_Path")
        {
            throw new ProjectFileAlreadyExistsException(request.Path, projectId, ex);
        }
        return new ProjectFileResponse
        {
            Id = projectFile.Id,
            Content = projectFile.Content,
            Path = projectFile.Path,
            ProjectId = projectFile.ProjectId
        };
    }

    public async Task<ProjectFileResponse?> GetProjectFileAsync(int projectId, int fileId, CancellationToken cancellationToken)
    {
        var projectFile = await _dbContext.ProjectFiles.AsNoTracking()
            .FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.Id == fileId, cancellationToken);

        if (projectFile is null)
        {
            return null;
        }

        return new ProjectFileResponse
        {
            Id = projectFile.Id,
            Content = projectFile.Content,
            Path = projectFile.Path,
            ProjectId = projectFile.ProjectId
        };
    }

    public async Task<ProjectFileResponse?> UpdateProjectFileAsync(int projectId, int fileId, UpdateProjectFileRequest request, CancellationToken cancellationToken)
    {
        var projectFile = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.Id == fileId, cancellationToken);

        if (projectFile is null)
        {
            return null;
        }

        projectFile.Content = request.Content;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation && pgEx.ConstraintName == "FK_ProjectFiles_Projects_ProjectId")
        {
            return null;
        }

        await NotifyProjectFileUpdatedAsync(projectId, fileId);

        return new ProjectFileResponse
        {
            Id = projectFile.Id,
            Content = projectFile.Content,
            Path = projectFile.Path,
            ProjectId = projectFile.ProjectId
        };

    }

    private async Task NotifyProjectFileUpdatedAsync(int projectId, int fileId)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
        try
        {
            await _hubContext.Clients.Group($"project:{projectId}").SendAsync("ProjectFileUpdated", new
            {
                ProjectId = projectId,
                FileId = fileId,
            }, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify clients about file {FileId} in project {ProjectId}", fileId, projectId);
        }
    }

    public async Task<bool> DeleteProjectFileAsync(int projectId, int fileId, CancellationToken cancellationToken)
    {
        var projectFile = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.Id == fileId, cancellationToken);

        if (projectFile is null)
        {
            return false;
        }
        _dbContext.ProjectFiles.Remove(projectFile);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }

        return true;
    }

}
