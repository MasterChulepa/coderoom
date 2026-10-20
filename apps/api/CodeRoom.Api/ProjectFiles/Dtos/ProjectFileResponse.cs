namespace CodeRoom.Api.ProjectFiles.Dtos;

public class ProjectFileResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public required string Path { get; set; }
    public int ProjectId { get; set; }
}
