using Microsoft.AspNetCore.Mvc;
using CodeRoom.Api.Projects.Dtos;
using CodeRoom.Api.Projects.Services;

namespace CodeRoom.Api.Projects.Controllers;

[ApiController]
[Route("api/projects")]

public class ProjectsController(IProjectService _projectService) : ControllerBase
{
    [HttpGet(Name = "GetProjects")]
    public async Task<Project[]> Get(CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetProjectsAsync(cancellationToken);
        return projects;
    }

    [HttpGet("{id}", Name = "GetProject")]
    public async Task<ActionResult<Project>> GetProject(int id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }
        return Ok(project);
    }

    [HttpPost(Name = "CreateProject")]
    public async Task<ActionResult<Project>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _projectService.CreateProjectAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }


    [HttpPut("{id}", Name = "UpdateProject")]
    public async Task<ActionResult<Project>> Update(int id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _projectService.UpdateProjectAsync(id, request, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }
        return Ok(project);
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
