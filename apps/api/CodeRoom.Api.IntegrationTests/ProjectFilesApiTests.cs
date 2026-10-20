using System.Net.Http.Json;
using CodeRoom.Api.Projects.Dtos;
using CodeRoom.Api.ProjectFiles.Dtos;
using CodeRoom.Api.Projects;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CodeRoom.Api.Data;
using Microsoft.EntityFrameworkCore;
using CodeRoom.Api.ProjectFiles.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;


namespace CodeRoom.Api.IntegrationTests;

public class ProjectFilesApiTests(CodeRoomApiFixture apiFixture) : IClassFixture<CodeRoomApiFixture>
{
    [Fact]
    public async Task CreateProjectFile_ShouldReturnCreatedFile()
    {
        // Arrange
        var projectRequest = new CreateProjectRequest
        {
            Name = $"Test Project {Guid.NewGuid():N}"
        };
        var projectFileRequest = new CreateProjectFileRequest
        {
            Path = $"test-file{Guid.NewGuid():N}.txt",
            Content = "This is a test file."
        };

        using var createdProjectResponse = await apiFixture.Client.PostAsJsonAsync("api/projects", projectRequest);
        var createdProject = await createdProjectResponse.Content.ReadFromJsonAsync<Project>();

        Assert.NotNull(createdProject);
        Assert.Equal(HttpStatusCode.Created, createdProjectResponse.StatusCode);
        Assert.True(createdProject.Id > 0);

        using var createdProjectFileResponse = await apiFixture.Client.PostAsJsonAsync($"api/projects/{createdProject.Id}/files", projectFileRequest);
        Assert.Equal(HttpStatusCode.Created, createdProjectFileResponse.StatusCode);

        var createdProjectFile = await createdProjectFileResponse.Content.ReadFromJsonAsync<ProjectFileResponse>();

        // Assert
        Assert.NotNull(createdProjectFile);
        Assert.Equal(projectFileRequest.Path, createdProjectFile.Path);
        Assert.Equal(projectFileRequest.Content, createdProjectFile.Content);
        Assert.Equal(createdProject.Id, createdProjectFile.ProjectId);


        var locationHeader = createdProjectFileResponse.Headers.Location?.ToString();
        Assert.NotNull(locationHeader);
        Assert.EndsWith($"api/projects/{createdProject.Id}/files/{createdProjectFile.Id}", locationHeader);

        using var getProjectFileResponse = await apiFixture.Client.GetAsync(locationHeader);
        Assert.Equal(HttpStatusCode.OK, getProjectFileResponse.StatusCode);

        var retrievedProjectFile = await getProjectFileResponse.Content.ReadFromJsonAsync<ProjectFileResponse>();
        Assert.NotNull(retrievedProjectFile);
        Assert.Equal(createdProjectFile.Id, retrievedProjectFile.Id);
        Assert.Equal(createdProjectFile.Path, retrievedProjectFile.Path);
        Assert.Equal(createdProjectFile.Content, retrievedProjectFile.Content);
    }

    [Fact]
    public async Task GetProjectFile_ShouldReturnNotFoundForNotCorrectProjectId()
    {
        // Arrange
        var projectRequest1 = new CreateProjectRequest
        {
            Name = $"Test Project {Guid.NewGuid():N}"
        };
        var projectRequest2 = new CreateProjectRequest
        {
            Name = $"Test Project {Guid.NewGuid():N}"
        };

        using var createdProjectResponse1 = await apiFixture.Client.PostAsJsonAsync("api/projects", projectRequest1);
        var createdProject1 = await createdProjectResponse1.Content.ReadFromJsonAsync<Project>();

        Assert.NotNull(createdProject1);
        Assert.Equal(HttpStatusCode.Created, createdProjectResponse1.StatusCode);

        using var createdProjectResponse2 = await apiFixture.Client.PostAsJsonAsync("api/projects", projectRequest2);
        var createdProject2 = await createdProjectResponse2.Content.ReadFromJsonAsync<Project>();

        Assert.NotNull(createdProject2);
        Assert.Equal(HttpStatusCode.Created, createdProjectResponse2.StatusCode);

        using var createdProjectFileResponse = await apiFixture.Client.PostAsJsonAsync($"api/projects/{createdProject1.Id}/files", new CreateProjectFileRequest
        {
            Path = $"test-file{Guid.NewGuid():N}.txt",
            Content = "This is a test file."
        });
        var createdProjectFile = await createdProjectFileResponse.Content.ReadFromJsonAsync<ProjectFileResponse>();
        Assert.Equal(HttpStatusCode.Created, createdProjectFileResponse.StatusCode);
        Assert.NotNull(createdProjectFile);
        using var getProjectFileResponse = await apiFixture.Client.GetAsync($"api/projects/{createdProject2.Id}/files/{createdProjectFile.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getProjectFileResponse.StatusCode);
    }

    [Fact]
    public async Task CreateProjectFile_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        var projectFileRequest = new CreateProjectFileRequest
        {
            Path = $"test-file{Guid.NewGuid():N}.txt",
            Content = "This is a test file."
        };

        using var createProjectFileResponse = await apiFixture.Client.PostAsJsonAsync($"api/projects/{int.MaxValue}/files", projectFileRequest);
        Assert.Equal(HttpStatusCode.NotFound, createProjectFileResponse.StatusCode);

    }

    private class ProjectFileSaveChangesInterceptor(Func<CancellationToken, Task> beforeSave) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await beforeSave(cancellationToken);
            return result;
        }
    }

    [Fact]

    public async Task CreateProjectFile_ReturnsNull_WhenProjectDeletedBeforeSave()
    {
        // Arrange
        var projectRequest = new CreateProjectRequest
        {
            Name = $"Test Project {Guid.NewGuid():N}"
        };

        using var createdProjectResponse = await apiFixture.Client.PostAsJsonAsync("api/projects", projectRequest);
        var createdProject = await createdProjectResponse.Content.ReadFromJsonAsync<Project>();

        Assert.NotNull(createdProject);
        Assert.Equal(HttpStatusCode.Created, createdProjectResponse.StatusCode);

        var projectFileRequest = new CreateProjectFileRequest
        {
            Path = $"test-file{Guid.NewGuid():N}.txt",
            Content = "This is a test file."
        };

        // Simulate a race condition by intercepting the SaveChangesAsync call
        var interceptor = new ProjectFileSaveChangesInterceptor(async cancellationToken =>
        {

            await using var dbContext = new CodeRoomDbContext(new DbContextOptionsBuilder<CodeRoomDbContext>()
                 .UseNpgsql(apiFixture.ConnectionString)
                 .Options);
            dbContext.Projects.Remove(await dbContext.Projects.FirstAsync(p => p.Id == createdProject.Id, cancellationToken));
            await dbContext.SaveChangesAsync(cancellationToken);
        });
        var options = new DbContextOptionsBuilder<CodeRoomDbContext>()
            .UseNpgsql(apiFixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var creatingContext = new CodeRoomDbContext(options);
        var projectFileService = new ProjectFileService(creatingContext);
        var createdFile = await projectFileService.CreateProjectFileAsync(createdProject.Id, projectFileRequest, CancellationToken.None);
        Assert.Null(createdFile);
    }

    [Fact]

    public async Task CreateProjectFile_ReturnsConflict_WhenFileAlreadyExists()
    {
        // Arrange
        var projectRequest = new CreateProjectRequest
        {
            Name = $"Test Project {Guid.NewGuid():N}"
        };

        using var createdProjectResponse = await apiFixture.Client.PostAsJsonAsync("api/projects", projectRequest);
        var createdProject = await createdProjectResponse.Content.ReadFromJsonAsync<Project>();

        Assert.NotNull(createdProject);
        Assert.Equal(HttpStatusCode.Created, createdProjectResponse.StatusCode);

        var projectFileRequest = new CreateProjectFileRequest
        {
            Path = $"test-file{Guid.NewGuid():N}.txt",
            Content = "This is a test file."
        };

        using var createdProjectFileResponse1 = await apiFixture.Client.PostAsJsonAsync($"api/projects/{createdProject.Id}/files", projectFileRequest);
        Assert.Equal(HttpStatusCode.Created, createdProjectFileResponse1.StatusCode);

        using var createdProjectFileResponse2 = await apiFixture.Client.PostAsJsonAsync($"api/projects/{createdProject.Id}/files", projectFileRequest);
        var errorResponse = await createdProjectFileResponse2.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(HttpStatusCode.Conflict, createdProjectFileResponse2.StatusCode);
        Assert.NotNull(errorResponse);
        Assert.Equal((int)HttpStatusCode.Conflict, errorResponse.Status);
        Assert.Equal("project_file_path_conflict", errorResponse.Extensions["errorCode"]?.ToString());
    }
}


