using CodeRoom.Api.Projects.Dtos;

namespace CodeRoom.Api.Projects.Services;

public interface IProjectService
{
    Task<Project[]> GetProjectsAsync(CancellationToken cancellationToken);

    Task<Project> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken);

    Task<Project?> GetProjectByIdAsync(int id, CancellationToken cancellationToken);

    Task<Project?> UpdateProjectAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteProjectAsync(int id, CancellationToken cancellationToken);
}
