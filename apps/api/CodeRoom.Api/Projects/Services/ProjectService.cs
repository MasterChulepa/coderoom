using CodeRoom.Api.Data;
using Microsoft.EntityFrameworkCore;
using CodeRoom.Api.Projects.Exceptions;
using Npgsql;
using CodeRoom.Api.Projects.Dtos;
using CodeRoom.Api.Projects.Models;


namespace CodeRoom.Api.Projects.Services;

public class ProjectService(CodeRoomDbContext _dbContext) : IProjectService
{
    public Task<Project[]> GetProjectsAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Projects.AsNoTracking().ToArrayAsync(cancellationToken);
    }

    public async Task<Project> CreateProjectAsync(CreateProjectRequest request, int guestId, CancellationToken cancellationToken)
    {
        var projectName = request.Name.Trim();
        var newProject = new Project { Name = projectName };
        _dbContext.Projects.Add(newProject);
        AddProjectOwner(newProject, guestId, request.DisplayName);
        await SaveProjectChangesAsync(projectName, cancellationToken);
        return newProject;
    }

    public Task<Project?> GetProjectByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project?> UpdateProjectAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }


        var projectName = request.Name.Trim();
        project.Name = projectName;
        await SaveProjectChangesAsync(projectName, cancellationToken);
        return project;
    }

    public async Task<bool> DeleteProjectAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task SaveProjectChangesAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when ((ex.InnerException is PostgresException postgresException)
         && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
         && postgresException.ConstraintName == "IX_Projects_Name")
        {
            throw new ProjectAlreadyExistsException(name, ex);
        }
    }

    private void AddProjectOwner(Project project, int guestId, string ownerName)
    {
        var pm = new ProjectMember
        {
            GuestId = guestId,
            DisplayName = ownerName.Trim(),
            Role = Roles.Owner,
            Project = project,
        };
        _dbContext.ProjectMembers.Add(pm);
    }
}
