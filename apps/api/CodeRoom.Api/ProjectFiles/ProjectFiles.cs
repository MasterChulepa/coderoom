using CodeRoom.Api.Collaboration;
using CodeRoom.Api.Projects;

namespace CodeRoom.Api.ProjectFiles;

public class ProjectFile
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public required string Path { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<CollaborativeDocumentUpdate> CollaborativeDocumentUpdates { get; set; } = new List<CollaborativeDocumentUpdate>();
}
