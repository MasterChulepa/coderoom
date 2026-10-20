using CodeRoom.Api.ProjectFiles.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CodeRoom.Api.ProjectFiles.ExceptionHandlers;

public class ProjectFileAlreadyExistsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ProjectFileAlreadyExistsException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Project file already exists",
                    Detail = exception.Message,
                    Extensions =
                    {
                        ["errorCode"] = "project_file_path_conflict"
                    }
                }
            });
            return true;
        }

        return false;
    }
}
