using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprintsASP_NetCore_API.Domain.Exceptions
{
    public class EventAlreadyStartedException : Exception
    {
        public EventAlreadyStartedException(Guid eventId, DateTime startAt)
            : base($"Событие {eventId} уже началось ({startAt:u})") { }
    }

}
