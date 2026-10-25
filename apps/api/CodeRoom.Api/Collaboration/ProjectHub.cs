using CodeRoom.Api.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CodeRoom.Api.Collaboration;

public class ProjectHub(CodeRoomDbContext _dbContext) : Hub
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
}