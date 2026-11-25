using CodeRoom.Api.Projects;

namespace CodeRoom.Api.Auth.Models;


public class Guest
{
    public int Id { get; set; }

    public ICollection<Project> Projects { get; } = [];
}