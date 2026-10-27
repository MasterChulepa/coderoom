namespace CodeRoom.Api.Collaboration.Exceptions;

public class InvalidUpdateIdException() : Exception("UpdateId must be a non-empty GUID")
{ }