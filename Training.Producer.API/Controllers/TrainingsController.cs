using Microsoft.AspNetCore.Mvc;
using Training.Producer.API.Domain;
using Training.Producer.API.Dto;
using Training.Producer.API.Infrastructure;

namespace Training.Producer.API.Controllers;

[ApiController]
[Route("api/trainings")]
public class TrainingsController(InMemoryEventStore store, IEventPublisher publisher) : ControllerBase
{
    // ---------- Команди ----------

    [HttpPost]
    public async Task<IActionResult> Create(CreateTrainingRequest r)
    {
        var aggregate = new TrainingAggregate();   // новий агрегат — історії ще немає
        try
        {
            aggregate.Create(Guid.NewGuid(), r.CoachId, r.Title, r.Date, r.Description);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        return await SaveAndPublish(aggregate);
    }

    [HttpPost("{id:guid}/exercises")]
    public Task<IActionResult> AddExercise(Guid id, AddExerciseRequest r) =>
        Execute(id, a => a.AddExercise(r.Name, r.DurationMinutes, r.Repetitions));

    [HttpDelete("{id:guid}/exercises/{exerciseId:int}")]
    public Task<IActionResult> RemoveExercise(Guid id, int exerciseId) =>
        Execute(id, a => a.RemoveExercise(exerciseId));

    [HttpPut("{id:guid}/details")]
    public Task<IActionResult> UpdateDetails(Guid id, UpdateTrainingDetailsRequest r) =>
        Execute(id, a => a.UpdateDetails(r.Title, r.Date, r.Description));

    [HttpPost("{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, AssignTrainingRequest r) =>
        Execute(id, a => a.Assign(r.AthleteId));

    [HttpPost("{id:guid}/complete")]
    public Task<IActionResult> Complete(Guid id, CompleteTrainingRequest r) =>
        Execute(id, a => a.Complete(r.AthleteId, r.StateRating, r.Comment));

    [HttpPost("{id:guid}/review")]
    public Task<IActionResult> Review(Guid id, ReviewTrainingRequest r) =>
        Execute(id, a => a.Review(r.CoachId, r.Score, r.Comment));

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancelTrainingRequest r) =>
        Execute(id, a => a.Cancel(r.Reason));

    // ---------- Перегляд потоку подій (для демонстрації та налагодження) ----------
    // Увага: це НЕ сторона читання CQRS, а "сирий" журнал зі сторони запису.
    [HttpGet("{id:guid}/events")]
    public IActionResult GetEvents(Guid id)
    {
        var events = store.GetEvents(id);
        if (events.Count == 0) return NotFound(new { error = $"Тренування {id} не знайдено." });

        return Ok(events.Select(e => new
        {
            e.Version,
            Type = e.GetType().Name,
            e.Timestamp,
            Data = (object)e      // (object) — щоб серіалізувались усі поля конкретної події
        }));
    }

    // ---------- Спільний цикл обробки команди ----------

    private async Task<IActionResult> Execute(Guid id, Action<TrainingAggregate> command)
    {
        // 1. Завантажуємо історію
        var history = store.GetEvents(id);
        if (history.Count == 0) return NotFound(new { error = $"Тренування {id} не знайдено." });

        // 2. Відновлюємо поточний стан (Replay)
        var aggregate = TrainingAggregate.FromHistory(history);

        // 3. Виконуємо команду — агрегат перевіряє правила
        try
        {
            command(aggregate);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });   // команду відхилено, подій немає
        }

        // 4–5. Зберігаємо і публікуємо
        return await SaveAndPublish(aggregate);
    }

    private async Task<IActionResult> SaveAndPublish(TrainingAggregate aggregate)
    {
        var newEvents = aggregate.UncommittedEvents.ToList();

        try
        {
            store.Append(aggregate.Id, newEvents);           // спочатку — в джерело істини
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(new { error = ex.Message });      // 409
        }

        foreach (var e in newEvents)
            await publisher.PublishAsync(e);                  // потім — повідомляємо світ

        return Ok(new
        {
            trainingId = aggregate.Id,
            status = aggregate.Status.ToString(),
            version = aggregate.Version,
            events = newEvents.Select(e => e.GetType().Name)
        });
    }
}