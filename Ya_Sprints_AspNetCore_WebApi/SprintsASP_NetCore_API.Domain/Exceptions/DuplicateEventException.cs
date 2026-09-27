 

namespace SprintsASP_NetCore_API.Domain.Exceptions;

/// <summary>
/// Нарушение уникальности события (по Id или Title).
/// </summary>
public class DuplicateEventException : Exception
{
    public DuplicateEventException(string message) : base(message) { }
    public DuplicateEventException(string message, Exception inner) : base(message, inner) { }
}
