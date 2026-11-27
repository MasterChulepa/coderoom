using CodeRoom.Api.Projects.Models;

namespace CodeRoom.Api.Projects;

public class ProjectMember
{
    public int GuestId { get; set; }
    public int ProjectId { get; set; }
    public required string DisplayName { get; set; }
    public Project Project { get; set; } = null!;
    public Roles Role { get; set; } = Roles.Viewer;
}