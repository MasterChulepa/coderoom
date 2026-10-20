namespace CodeRoom.Api.ProjectFiles.Exceptions;

public class ProjectFileAlreadyExistsException(string path, int projectId, Exception? innerException = null) : Exception($"A file with the path '{path}' already exists in project {projectId}.", innerException)
{
}