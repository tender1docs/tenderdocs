namespace TenderDocs.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key) : base($"\"{name}\" ({key}) was not found.") { }
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action.") : base(message) { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }
    public ValidationException(IDictionary<string, string[]> errors) : base("One or more validation failures occurred.")
        => Errors = errors;
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
