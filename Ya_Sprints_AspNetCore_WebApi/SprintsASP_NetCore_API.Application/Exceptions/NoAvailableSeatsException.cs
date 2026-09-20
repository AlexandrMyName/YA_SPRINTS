 

namespace SprintsASP_NetCore_API.Application.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message) { } 
}
