using System.ComponentModel.DataAnnotations;

namespace CodeRoom.Api.Projects.Dtos;

public class UpdateProjectRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }
}
