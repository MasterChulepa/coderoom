using System.Net;
using System.Net.Http.Json;
using CodeRoom.Api.Projects;
using CodeRoom.Api.Projects.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
namespace CodeRoom.Api.IntegrationTests;

public class ProjectsApiTests(CodeRoomApiFixture apiFixture) : IClassFixture<CodeRoomApiFixture>
{
    [Fact]
    public async Task CreateProject_ReturnsCreated()
    {

        using var response = await CreateProjectAsync(" Test Project ");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var readProject = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(readProject);
        Assert.Equal("Test Project", readProject!.Name);
        Assert.True(readProject.Id > 0);

        var location = response.Headers.Location;

        Assert.NotNull(location);
        Assert.EndsWith(
            $"/api/projects/{readProject.Id}",
            location.ToString());

    }

    [Fact]

    public async Task CreateProject_ReturnsConflict_WhenNameAlreadyExists()
    {
        var projectName = $"Test Project {Guid.NewGuid():N}";
        using var setupResponse = await CreateProjectAsync(projectName);
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);


        using var response = await CreateProjectAsync(projectName);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal("Project already exists", problem.Title);
        Assert.Contains(projectName, problem.Detail);
        Assert.Equal("project_name_conflict", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task CreateProject_ReturnsBadRequest_WhenNameIsEmpty()
    {
        using var response = await CreateProjectAsync("");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Contains(nameof(CreateProjectRequest.Name), problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateProject_ReturnsBadRequest_WhenNameIsTooLong()
    {
        var longName = new string('A', 101);
        using var response = await CreateProjectAsync(longName);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Contains(nameof(CreateProjectRequest.Name), problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateProject_ReturnsBadRequest_WhenNameContainsOnlyWhitespace()
    {
        using var response = await CreateProjectAsync("   ");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Contains(nameof(CreateProjectRequest.Name), problem.Errors.Keys);
    }

    [Fact]
    public async Task GetProject_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        var nonExistentProjectId = int.MaxValue;
        using var response = await apiFixture.Client.GetAsync($"/api/projects/{nonExistentProjectId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_ReturnsProject_WhenProjectExists()
    {
        var projectName = $"Test Project {Guid.NewGuid():N}";
        using var setupResponse = await CreateProjectAsync(projectName);
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        var createdProject = await setupResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject);

        using var response = await apiFixture.Client.GetAsync($"/api/projects/{createdProject!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var readProject = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(readProject);
        Assert.Equal(createdProject.Id, readProject!.Id);
        Assert.Equal(createdProject.Name, readProject.Name);
    }

    [Fact]
    public async Task UpdateProject_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        var nonExistentProjectId = 9999;
        using var response = await apiFixture.Client.PutAsJsonAsync($"/api/projects/{nonExistentProjectId}", new UpdateProjectRequest { Name = "Updated Project" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_ReturnsUpdatedProject_WhenProjectExists()
    {
        var projectName = $"Test Project {Guid.NewGuid():N}";
        using var setupResponse = await CreateProjectAsync(projectName);
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        var createdProject = await setupResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject);

        var updatedName = $"Updated Project {Guid.NewGuid():N}";
        using var response = await apiFixture.Client.PutAsJsonAsync($"/api/projects/{createdProject!.Id}", new UpdateProjectRequest { Name = updatedName });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedProject = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(updatedProject);
        Assert.Equal(createdProject.Id, updatedProject!.Id);
        Assert.Equal(updatedName, updatedProject.Name);
    }

    [Fact]
    public async Task UpdateProject_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var projectName = $"Test Project {Guid.NewGuid():N}";
        using var setupResponse = await CreateProjectAsync(projectName);
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        var createdProject = await setupResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject);

        using var response = await apiFixture.Client.PutAsJsonAsync($"/api/projects/{createdProject!.Id}", new UpdateProjectRequest { Name = "" });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Contains(nameof(UpdateProjectRequest.Name), problem.Errors.Keys);
    }

    [Fact]
    public async Task UpdateProject_ReturnsConflict_WhenProjectNameIsAlreadyTaken()
    {
        var projectName1 = $"Test Project 1 {Guid.NewGuid():N}";
        var projectName2 = $"Test Project 2 {Guid.NewGuid():N}";

        using var setupResponse1 = await CreateProjectAsync(projectName1);
        Assert.Equal(HttpStatusCode.Created, setupResponse1.StatusCode);
        var createdProject1 = await setupResponse1.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject1);

        using var setupResponse2 = await CreateProjectAsync(projectName2);
        Assert.Equal(HttpStatusCode.Created, setupResponse2.StatusCode);
        var createdProject2 = await setupResponse2.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject2);

        using var response = await apiFixture.Client.PutAsJsonAsync($"/api/projects/{createdProject2!.Id}", new UpdateProjectRequest { Name = projectName1 });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using var projectResponse = await apiFixture.Client.GetAsync($"/api/projects/{createdProject2!.Id}");
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal("Project already exists", problem.Title);
        Assert.Contains(projectName1, problem.Detail);
        Assert.Equal("project_name_conflict", problem.Extensions["errorCode"]?.ToString());
        Assert.NotNull(project);
        Assert.Equal(projectName2, project!.Name);
    }

    [Fact]
    public async Task DeleteProject_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        var nonExistentProjectId = int.MaxValue;
        using var response = await apiFixture.Client.DeleteAsync($"/api/projects/{nonExistentProjectId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_ReturnsNoContent_WhenProjectExists()
    {
        var projectName = $"Test Project {Guid.NewGuid():N}";
        using var setupResponse = await CreateProjectAsync(projectName);
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        var createdProject = await setupResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject);

        using var response = await apiFixture.Client.DeleteAsync($"/api/projects/{createdProject!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var getResponse = await apiFixture.Client.GetAsync($"/api/projects/{createdProject.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateProjectAsync(string projectName)
    {
        var httpClient = apiFixture.Client;
        using var sessionResponse = await httpClient.PostAsJsonAsync("/api/guest-session", new { });
        Assert.Equal(HttpStatusCode.NoContent, sessionResponse.StatusCode);
        return await httpClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest { Name = projectName, DisplayName = "Owner" });
    }
}
