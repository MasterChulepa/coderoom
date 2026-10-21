using CodeRoom.Api.Data;
using Microsoft.EntityFrameworkCore;
using CodeRoom.Api.ProjectFiles.ExceptionHandlers;
using CodeRoom.Api.Projects.ExceptionHandlers;
using CodeRoom.Api.ProjectFiles.Services;
using CodeRoom.Api.Projects.Services;
using CodeRoom.Api.Collaboration;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectFileService, ProjectFileService>();
builder.Services.AddDbContext<CodeRoomDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services
    .AddExceptionHandler<ProjectAlreadyExistsExceptionHandler>()
    .AddExceptionHandler<ProjectFileAlreadyExistsExceptionHandler>();
builder.Services.AddSignalR();

var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<ProjectHub>("/hubs/projects");

app.Run();


public partial class Program
{
}
