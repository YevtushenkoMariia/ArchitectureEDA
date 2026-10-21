using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Contracts
{
    public interface IEvent
    {
        Guid EventId { get; }        
        Guid TrainingId { get; }     // ідентифікатор потоку 
        DateTime Timestamp { get; } 
        int Version { get; set; }    // порядковий номер у потоці
    }

    public abstract class EventBase : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public Guid TrainingId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public int Version { get; set; }
    }
}

