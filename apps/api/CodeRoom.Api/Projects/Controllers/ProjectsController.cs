using Microsoft.AspNetCore.Mvc;
using CodeRoom.Api.Projects.Dtos;
using CodeRoom.Api.Projects.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodeRoom.Api.Projects.Controllers;

[ApiController]
[Route("api/projects")]

public class ProjectsController(IProjectService _projectService) : ControllerBase
{
    [HttpGet(Name = "GetProjects")]
    public async Task<ProjectResponse[]> Get(CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetProjectsAsync(cancellationToken);
        return [.. projects.Select(p => new ProjectResponse { Id = p.Id, Name = p.Name })];
    }

    [HttpGet("{id}", Name = "GetProject")]
    public async Task<ActionResult<ProjectResponse>> GetProject(int id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }
        return Ok(new ProjectResponse { Id = project.Id, Name = project.Name });
    }

    [Authorize(Policy = "Guest")]
    [HttpPost(Name = "CreateProject")]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {

        var guestIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(guestIdClaim, out var guestId))
        {
            var project = await _projectService.CreateProjectAsync(request, guestId, cancellationToken);
            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new ProjectResponse { Id = project.Id, Name = project.Name });
        }

        return Unauthorized();
    }


    [HttpPut("{id}", Name = "UpdateProject")]
    public async Task<ActionResult<ProjectResponse>> Update(int id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _projectService.UpdateProjectAsync(id, request, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }
        return Ok(new ProjectResponse { Id = project.Id, Name = project.Name });
    }

    [HttpDelete("{id}", Name = "DeleteProject")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _projectService.DeleteProjectAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}
