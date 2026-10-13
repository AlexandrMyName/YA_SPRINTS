using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprintsASP_NetCore_API.Domain.Exceptions
{
    public class BookingAlreadyCancelledException : Exception
    {
        public BookingAlreadyCancelledException(Guid bookingId)
            : base($"Бронь {bookingId} уже отменена") { }
    }
}
