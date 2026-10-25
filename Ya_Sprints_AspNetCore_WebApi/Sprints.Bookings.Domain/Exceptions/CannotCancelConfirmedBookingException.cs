using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sprints.Bookings.Domain.Exceptions
{
    public class CannotCancelConfirmedBookingException : Exception
    {
        public CannotCancelConfirmedBookingException(Guid bookingId)
            : base($"Бронь {bookingId} уже подтверждена, отмена невозможна") { }
    }
}
