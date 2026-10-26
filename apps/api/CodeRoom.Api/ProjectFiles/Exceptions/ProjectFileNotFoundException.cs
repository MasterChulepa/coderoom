namespace CodeRoom.Api.ProjectFiles.Exceptions;

public class ProjectFileNotFoundException(string fileId, Exception? innerException = null)
 : Exception($"The file with id '{fileId}' was not found", innerException)
{ }