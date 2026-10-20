using Training.Contracts;

namespace Training.Producer.API.Infrastructure;

public class ConcurrencyException(string message) : Exception(message);

public class InMemoryEventStore
{
    // Ключ — id тренування (ідентифікатор потоку), значення — потік подій
    private readonly Dictionary<Guid, List<IEvent>> _streams = new();
    private readonly object _lock = new();   // контролер обробляє запити паралельно

    public IReadOnlyList<IEvent> GetEvents(Guid trainingId)
    {
        lock (_lock)
            return _streams.TryGetValue(trainingId, out var stream) ? stream.ToList() : [];
    }

    public void Append(Guid trainingId, IReadOnlyList<IEvent> newEvents)
    {
        if (newEvents.Count == 0) return;

        lock (_lock)
        {
            if (!_streams.TryGetValue(trainingId, out var stream))
            {
                stream = new List<IEvent>();
                _streams[trainingId] = stream;
            }

            // Optimistic concurrency: перша нова подія має йти одразу за останньою збереженою
            var expectedVersion = stream.Count + 1;
            if (newEvents[0].Version != expectedVersion)
                throw new ConcurrencyException(
                    $"Конфлікт версій: очікувалась версія {expectedVersion}, отримано {newEvents[0].Version}. Повторіть запит.");

            stream.AddRange(newEvents);
        }
    }
}