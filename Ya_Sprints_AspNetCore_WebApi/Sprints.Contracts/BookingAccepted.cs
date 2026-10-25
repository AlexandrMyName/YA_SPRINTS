using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sprints.Contracts
{
    /// <summary>
    /// Events → Bookings: «Место зарезервировано, подтверждай бронь».
    /// </summary>
    public sealed record BookingAccepted
    {
        public Guid MessageId { get; init; }
        public Guid BookingId { get; init; }
        public Guid EventId { get; init; }
        public DateTime AcceptedAt { get; init; }
    }
}
