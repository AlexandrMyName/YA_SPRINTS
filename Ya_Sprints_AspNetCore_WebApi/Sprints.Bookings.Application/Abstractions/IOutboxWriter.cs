using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sprints.Bookings.Application.Abstractions
{
    public interface IOutboxWriter
    {
        /// <summary>
        /// Добавляет сообщение в Outbox. НЕ сохраняет — SaveChanges делает вызывающий.
        /// </summary>
        Task EnqueueAsync<T>(string topic, string key, T payload, CancellationToken ct = default)
            where T : class;
    }
}
