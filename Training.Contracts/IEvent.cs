using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Contracts
{
    public interface IEvent
    {
        Guid EventId { get; }        // унікальний ідентифікатор події
        Guid TrainingId { get; }     // ідентифікатор потоку (до якого тренування належить)
        DateTime Timestamp { get; }  // коли подія сталася
        int Version { get; set; }    // порядковий номер у потоці
    }

    public abstract record EventBase : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public Guid TrainingId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public int Version { get; set; }
    }
}

