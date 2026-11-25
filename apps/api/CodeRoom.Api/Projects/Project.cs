using CodeRoom.Api.Auth.Models;
using CodeRoom.Api.ProjectFiles;

namespace CodeRoom.Api.Projects;

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<ProjectFile> ProjectFiles { get; set; } = [];
    public ICollection<Guest> Guests { get; } = [];
}
