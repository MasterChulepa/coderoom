using Microsoft.AspNetCore.SignalR;

namespace CodeRoom.Api.Collaboration;

public class ProjectHub : Hub
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
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}:file:{fileId}");
    }

    public async Task LeaveFile(int projectId, int fileId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}:file:{fileId}");
    }
}