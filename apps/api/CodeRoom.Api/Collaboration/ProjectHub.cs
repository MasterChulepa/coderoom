using CodeRoom.Api.Collaboration.Exceptions;
using CodeRoom.Api.Collaboration.Services;
using CodeRoom.Api.Data;
using CodeRoom.Api.ProjectFiles.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CodeRoom.Api.Collaboration;

public class ProjectHub(CodeRoomDbContext _dbContext, ICollaborativeDocumentService _collaborativeService, ILogger<ProjectHub> _logger) : Hub
{
    public async Task JoinProject(int projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    public async Task LeaveProject(int projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    public async Task JoinFile(int projectId, int fileId)
    {
        if (!await _dbContext.ProjectFiles.AnyAsync(pf => pf.ProjectId == projectId && pf.Id == fileId))
        {
            throw new HubException($"Project file with ID {fileId} does not exist in project {projectId}.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}:file:{fileId}");
    }

    public async Task LeaveFile(int projectId, int fileId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}:file:{fileId}");
    }

    public async Task<IReadOnlyList<byte[]>> GetCollaborativeDocumentUpdatesAsync(int projectId, int fileId)
    {
        IReadOnlyList<byte[]> updates;
        try
        {
            updates = await _collaborativeService.GetUpdatesAsync(projectId, fileId, Context.ConnectionAborted);
        }
        catch (ProjectFileNotFoundException)
        {
            throw new HubException($"Can't receive updates for file with id {fileId}");
        }

        return updates;
    }

    public async Task UpdateCollaborativeDocumentAsync(int projectId, int fileId, Guid updateId, byte[] update)
    {
        try
        {
            await _collaborativeService.AppendUpdateAsync(projectId, fileId, updateId, update, Context.ConnectionAborted);
        }
        catch (ProjectFileNotFoundException)
        {
            throw new HubException($"Can't update file with id {fileId}");
        }
        catch (ConflictWhileSavingUpdateException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (InvalidUpdateIdException ex)
        {
            throw new HubException(ex.Message);
        }
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
        try
        {
            await Clients.OthersInGroup($"project:{projectId}:file:{fileId}").SendAsync("DocumentUpdateReceived", projectId, fileId, update, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify clients about file {FileId} update in project {ProjectId}", fileId, projectId);
        }
    }
}