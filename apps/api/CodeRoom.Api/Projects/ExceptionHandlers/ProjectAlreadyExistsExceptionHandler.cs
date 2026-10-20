using CodeRoom.Api.Projects.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace CodeRoom.Api.Projects.ExceptionHandlers;

public class ProjectAlreadyExistsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ProjectAlreadyExistsException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Project already exists",
                    Detail = exception.Message,
                    Extensions =
                    {
                        ["errorCode"] = "project_name_conflict"
                    }
                }
            });
            return true;
        }

        return false;
    }
}