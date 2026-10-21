using CodeRoom.Api.ProjectFiles.Dtos;
using CodeRoom.Api.ProjectFiles.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeRoom.Api.ProjectFiles.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/files")]
public class ProjectFilesController(IProjectFileService projectFileService) : ControllerBase
{
    [HttpGet(Name = "GetProjectFiles")]
    public async Task<ProjectFileResponse[]> GetProjectFiles(int projectId, CancellationToken cancellationToken)
    {
        var projectFiles = await projectFileService.GetProjectFilesAsync(projectId, cancellationToken);
        return projectFiles;
    }

    [HttpPost(Name = "CreateProjectFile")]
    public async Task<ActionResult<ProjectFileResponse>> CreateProjectFile([FromRoute] int projectId, [FromBody] CreateProjectFileRequest request, CancellationToken cancellationToken)
    {
        var createdProjectFile = await projectFileService.CreateProjectFileAsync(projectId, request, cancellationToken);
        if (createdProjectFile is null)
        {
            return NotFound();
        }
        return CreatedAtAction(nameof(GetProjectFile), new { projectId, fileId = createdProjectFile.Id }, createdProjectFile);
    }

    [HttpGet("{fileId:int}", Name = "GetProjectFile")]
    public async Task<ActionResult<ProjectFileResponse>> GetProjectFile([FromRoute] int projectId, [FromRoute] int fileId, CancellationToken cancellationToken)
    {
        var projectFile = await projectFileService.GetProjectFileAsync(projectId, fileId, cancellationToken);

        if (projectFile is null)
        {
            return NotFound();
        }

        return Ok(projectFile);
    }

    [HttpPut("{fileId:int}/content", Name = "UpdateProjectFile")]
    public async Task<ActionResult<ProjectFileResponse>> UpdateProjectFile([FromRoute] int projectId, [FromRoute] int fileId, [FromBody] UpdateProjectFileRequest request, CancellationToken cancellationToken)
    {
        var updatedProjectFile = await projectFileService.UpdateProjectFileAsync(projectId, fileId, request, cancellationToken);

        if (updatedProjectFile is null)
        {
            return NotFound();
        }

        return Ok(updatedProjectFile);
    }

    [HttpDelete("{fileId:int}", Name = "DeleteProjectFile")]
    public async Task<ActionResult> DeleteProjectFile([FromRoute] int projectId, [FromRoute] int fileId, CancellationToken cancellationToken)
    {
        var deleted = await projectFileService.DeleteProjectFileAsync(projectId, fileId, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

}
