using Training.Contracts;

namespace Training.Producer.API.Infrastructure;

public class ConcurrencyException(string message) : Exception(message);


// для зберігання станів агрегату, замість БД
public class EventStore
{
    
    private readonly Dictionary<Guid, List<IEvent>> _streams = new();

    // контролер обробляє запити паралельно
    private readonly object _lock = new();   


    // Отримання всіх подій для тренування 
    public IReadOnlyList<IEvent> GetEvents(Guid trainingId)
    {
        lock (_lock)
            return _streams.TryGetValue(trainingId, out var stream) ? stream.ToList() : [];
    }

    public void SaveEvents(Guid trainingId, IReadOnlyList<IEvent> newEvents)
    {
        if (newEvents.Count == 0) 
            return;

        lock (_lock)
        {
            if (!_streams.TryGetValue(trainingId, out var stream))
            {
                stream = new List<IEvent>();
                _streams[trainingId] = stream;
            }

            var expectedVersion = stream.Count + 1;

            // перевірка версії першої нової події 
            if (newEvents[0].Version != expectedVersion)
                throw new ConcurrencyException(
                    $"Конфлікт версій: очікувалась версія {expectedVersion}, отримано {newEvents[0].Version}. Повторіть запит.");

            stream.AddRange(newEvents);
        }
    }
}