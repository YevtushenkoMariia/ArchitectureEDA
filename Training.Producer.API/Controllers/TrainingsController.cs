using Microsoft.AspNetCore.Mvc;
using Training.Producer.API.Domain;
using Training.Producer.API.Dto;
using Training.Producer.API.Infrastructure;

namespace Training.Producer.API.Controllers;

[ApiController]
[Route("api/trainings")]
public class TrainingsController(EventStore store, IEventPublisher publisher) : ControllerBase
{
    // ---------- Команди ----------
    [HttpPost]
    public async Task<IActionResult> Create(CreateTrainingRequest req)
    {
        // новий агрегат 
        var aggregate = new TrainingAggregate();
        
        try
        {
            // виконання команди
            aggregate.Create(Guid.NewGuid(), req.CoachId, req.Title, req.Date, req.Description);
        }
        catch (DomainException e)
        {
            return BadRequest(new { error = e.Message });
        }

        // зберегти і опублікувати подію
        return await SaveAndPublishEvents(aggregate);
    }


    [HttpPost("{id:guid}/exercises")]
    public async Task<IActionResult> AddExercise(Guid id, AddExerciseRequest r)
    {
        return await Execute(id, a =>
            a.AddExercise(r.Name, r.DurationMinutes, r.Repetitions));
    }

    [HttpDelete("{id:guid}/exercises/{exerciseId:int}")]
    public async Task<IActionResult> RemoveExercise(Guid id, int exerciseId) { 
         return await Execute(id, a =>
            a.RemoveExercise(exerciseId));
    }

    [HttpPut("{id:guid}/details")]
    public async Task<IActionResult> UpdateDetails(Guid id, UpdateTrainingDetailsRequest r)
    {
        return await Execute(id, a =>
            a.UpdateDetails(r.Title, r.Date, r.Description));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignTrainingRequest r)
    {
        return await Execute(id, a =>
            a.Assign(r.AthleteId));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteTrainingRequest r)
    {
        return await Execute(id, a =>
            a.Complete(r.AthleteId, r.StateRating, r.Comment));
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewTrainingRequest r)
    {
        return await Execute(id, a =>
            a.Review(r.CoachId, r.Score, r.Comment));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelTrainingRequest r)
    {
        return await Execute(id, a =>
            a.Cancel(r.Reason));
    }

    //Перегляд потоку подій 
    [HttpGet("{id:guid}/events")]
    public IActionResult GetEvents(Guid id)
    {
        var events = store.GetEvents(id);
        if (events.Count == 0) 
            return NotFound(new { error = $"Тренування {id} не знайдено" });

        return Ok(events.Select(e => new
        {
            e.Version,
            Type = e.GetType().Name,
            e.Timestamp,
            Data = (object)e      
        }));
    }

    // Цикл обробки команди 
    private async Task<IActionResult> Execute(Guid id, Action<TrainingAggregate> command)
    {
        // завантаження історії
        var history = store.GetEvents(id);
        if (history.Count == 0)
            return NotFound(new { error = $"Тренування {id} не знайдено" });

        // відновлення поточного стану
        var aggregate = TrainingAggregate.FromHistory(history);

        // виконання команди
        try
        {
            command(aggregate);
        }
        catch (DomainException e)
        {
            return BadRequest(new { error = e.Message });   
        }

        // зберегти і опублікувати
        return await SaveAndPublishEvents(aggregate);
    }

    private async Task<IActionResult> SaveAndPublishEvents(TrainingAggregate aggregate)
    {
        var newEvents = aggregate.UncommittedEvents.ToList();

        try
        {
            // зберегти події агрегата 
            store.SaveEvents(aggregate.Id, newEvents);           
        }
        catch (ConcurrencyException e)
        {
            return Conflict(new { error = e.Message });     
        }

        // повідомлення про подію інших
        foreach (var e in newEvents)
            await publisher.PublishAsync(e);                

        return Ok(new
        {
            trainingId = aggregate.Id,
            status = aggregate.Status.ToString(),
            version = aggregate.Version,
            events = newEvents.Select(e => e.GetType().Name)
        });
    }
}