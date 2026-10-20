using System.ComponentModel.DataAnnotations;

namespace CodeRoom.Api.ProjectFiles.Dtos;

public class CreateProjectFileRequest
{

    public string Content { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Path { get; set; }

}
