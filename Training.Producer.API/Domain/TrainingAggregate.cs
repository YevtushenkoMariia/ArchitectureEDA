using Training.Contracts;

namespace Training.Producer.API.Domain;


// Стани агрегату
public enum TrainingStatus { Draft, Assigned, Completed, Reviewed, Cancelled }

public record ExerciseItem(int Id, string Name, int DurationMinutes, int Repetitions);


// Агрегат
public class TrainingAggregate
{
    // Стан агрегату 
    public Guid Id { get; private set; }
    public TrainingStatus Status { get; private set; }
    public Guid CoachId { get; private set; }
    public Guid? AthleteId { get; private set; }
    public int Version { get; private set; }            // скільки подій вже застосовано

    private bool _isCreated;

    // вправи в тренування
    private int _lastExerciseId;                        
    private readonly List<ExerciseItem> _exercises = new();

    // нові події, які ще не збережені в EventStore
    public List<IEvent> UncommittedEvents = new List<IEvent>();


    // відновлення стану з історії 
    public static TrainingAggregate FromHistory(IEnumerable<IEvent> history)
    {
        var aggregate = new TrainingAggregate();
        foreach (var e in history)
            aggregate.Apply(e);  
        
        return aggregate;
    }

    // КОМАНДИ
    public void Create(Guid id, Guid coachId, string title, DateTime date, string? description)
    {
        if (_isCreated) 
            throw new DomainException("Тренування вже створене");

        if (coachId == Guid.Empty) 
            throw new DomainException("Не вказано тренера");

        ValidateDetails(title, date);

        Raise(new TrainingCreated
        {
            TrainingId = id,
            CoachId = coachId,
            Title = title,
            Date = date,
            Description = description
        });
    }

    public void AddExercise(string name, int durationMinutes, int repetitions)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(name)) 
            throw new DomainException("Назва вправи не може бути порожньою");

        if (durationMinutes <= 0) 
            throw new DomainException("Тривалість вправи має бути більшою за 0");

        Raise(new ExerciseAdded
        {
            TrainingId = Id,
            ExerciseId = _lastExerciseId + 1,   
            Name = name,
            DurationMinutes = durationMinutes,
            Repetitions = repetitions,
            AthleteId = AthleteId
        });
    }

    public void RemoveExercise(int exerciseId)
    {
        EnsureEditable();
        if (_exercises.All(e => e.Id != exerciseId))
            throw new DomainException($"Вправу з id {exerciseId} не знайдено в тренуванні.");
       

        //?
        if (Status == TrainingStatus.Assigned && _exercises.Count == 1)
            throw new DomainException("Не можна видалити останню вправу з призначеного тренування");

        Raise(new ExerciseRemoved { 
            TrainingId = Id, 
            ExerciseId = exerciseId,
            AthleteId = AthleteId
        });
    }

    public void UpdateDetails(string title, DateTime date, string? description)
    {
        EnsureEditable();
        ValidateDetails(title, date);

        Raise(new TrainingDetailsUpdated { 
            TrainingId = Id, 
            Title = title, 
            Date = date, 
            Description = description,
            AthleteId = AthleteId
        });
    }

    public void Assign(Guid athleteId)
    {
        EnsureCreated();
        if (Status != TrainingStatus.Draft)
            throw new DomainException($"Призначити можна лише тренування у стані Draft (поточний: {Status})");
      
        if (_exercises.Count == 0)
            throw new DomainException("Не можна призначити тренування без жодної вправи");
        
        if (athleteId == Guid.Empty) 
            throw new DomainException("Не вказано спортсмена");

        Raise(new TrainingAssigned { 
            TrainingId = Id, 
            AthleteId = athleteId 
        });
    }

    public void Complete(Guid athleteId, int stateRating, string? comment)
    {
        EnsureCreated();
        if (Status != TrainingStatus.Assigned)
            throw new DomainException($"Відмітити виконання можна лише для призначеного тренування (поточний: {Status})");
       
        if (athleteId != AthleteId)
            throw new DomainException("Відмітити виконання може лише спортсмен, якому призначено тренування");
        if (stateRating is < 1 or > 10)
            throw new DomainException("Оцінка стану має бути від 1 до 10");

        Raise(new TrainingCompleted { 
            TrainingId = Id, 
            AthleteId = athleteId, 
            StateRating = stateRating, 
            Comment = comment 
        });
    }

    public void Review(Guid coachId, int score, string? comment)
    {
        EnsureCreated();
        if (Status != TrainingStatus.Completed)
            throw new DomainException($"Відгук можна надати лише на виконане тренування (поточний: {Status})");
       
        if (coachId != CoachId)
            throw new DomainException("Відгук може надати лише тренер, який створив тренування");
        
        if (score is < 1 or > 10)
            throw new DomainException("Оцінка має бути від 1 до 10");

        Raise(new TrainingReviewed { 
            TrainingId = Id, 
            CoachId = coachId, 
            Score = score, 
            Comment = comment 
        });
    }

    public void Cancel(string reason)
    {
        EnsureCreated();

        if (Status is TrainingStatus.Completed 
            or TrainingStatus.Reviewed 
            or TrainingStatus.Cancelled)
            throw new DomainException($"Не можна скасувати тренування у стані {Status}");

        if (string.IsNullOrWhiteSpace(reason)) 
            throw new DomainException("Вкажіть причину скасування");

        Raise(new TrainingCancelled { 
            TrainingId = Id, 
            Reason = reason,
            AthleteId = AthleteId
        });
    }

    // ЗАСТОСУВАННЯ ПОДІЙ 
    // подія відбулася
    private void Raise(EventBase e)
    {
        e.Version = Version + 1;
        Apply(e);
        UncommittedEvents.Add(e);
    }

    private void Apply(IEvent e)
    {
        switch (e)
        {
            case TrainingCreated c:
                Id = c.TrainingId;
                CoachId = c.CoachId;
                Status = TrainingStatus.Draft;
                _isCreated = true;
                break;
            case ExerciseAdded x:
                _exercises.Add(new ExerciseItem(x.ExerciseId, x.Name, x.DurationMinutes, x.Repetitions));
                _lastExerciseId = x.ExerciseId;
                break;
            case ExerciseRemoved r:
                _exercises.RemoveAll(ex => ex.Id == r.ExerciseId);
                break;
            case TrainingDetailsUpdated:
                break;                      // назва/дата не потрібні для правил — їх зберігає Read Model
            case TrainingAssigned a:
                AthleteId = a.AthleteId;
                Status = TrainingStatus.Assigned;
                break;
            case TrainingCompleted:
                Status = TrainingStatus.Completed;
                break;
            case TrainingReviewed:
                Status = TrainingStatus.Reviewed;
                break;
            case TrainingCancelled:
                Status = TrainingStatus.Cancelled;
                break;
        }
        Version = e.Version;
    }

    //  ПЕРЕВІРКИ  
    private void EnsureCreated()
    {
        if (!_isCreated) 
            throw new DomainException("Тренування не створено");
    }

    private void EnsureEditable()
    {
        EnsureCreated();
        if (Status is not (TrainingStatus.Draft or TrainingStatus.Assigned))
            throw new DomainException($"Редагувати можна лише у станах Draft або Assigned (поточний: {Status})");
    }

    private static void ValidateDetails(string title, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(title)) 
            throw new DomainException("Назва тренування не може бути порожньою");

        if (date.Date < DateTime.Today) 
            throw new DomainException("Дата тренування не може бути в минулому");
    }
}