using CodeRoom.Api.ProjectFiles;

namespace CodeRoom.Api.Collaboration;

public class CollaborativeDocumentUpdate
{
    public int Id { get; set; }
    public Guid UpdateId { get; set; }
    public int FileId { get; set; }
    public byte[] Update { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
    public ProjectFile ProjectFile { get; set; } = null!;
}