using CodeRoom.Api.ProjectFiles.Dtos;

namespace CodeRoom.Api.ProjectFiles.Services;

public interface IProjectFileService
{
    Task<ProjectFileResponse[]> GetProjectFilesAsync(int projectId, CancellationToken cancellationToken);
    Task<ProjectFileResponse?> GetProjectFileAsync(int projectId, int fileId, CancellationToken cancellationToken);
    Task<ProjectFileResponse?> CreateProjectFileAsync(int projectId, CreateProjectFileRequest request, CancellationToken cancellationToken);
    Task<ProjectFileResponse?> UpdateProjectFileAsync(int projectId, int fileId, UpdateProjectFileRequest request, CancellationToken cancellationToken);
}

