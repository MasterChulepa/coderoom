namespace CodeRoom.Api.Projects.Exceptions;

public class ProjectAlreadyExistsException(string name, Exception? innerException = null) : Exception($"A project with the name '{name}' already exists.", innerException)
{
}