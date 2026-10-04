using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprintsASP_NetCore_API.Domain.Exceptions
{
    public class ActiveBookingsLimitExceededException : Exception
    {
        public ActiveBookingsLimitExceededException(Guid userId, int limit)
            : base($"Пользователь {userId} достиг лимита активных броней ({limit})") { }
    }
}
