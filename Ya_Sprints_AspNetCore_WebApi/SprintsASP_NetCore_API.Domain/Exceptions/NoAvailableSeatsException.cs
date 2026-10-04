 

namespace SprintsASP_NetCore_API.Domain.Exceptions;

 
public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message) { } 
}
