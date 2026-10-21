namespace Training.Producer.API.Dto;


public class CreateTrainingRequest
{
    public Guid CoachId { get; set; }
    public string Title { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public class AddExerciseRequest
{
    public string Name { get; set; }
    public int DurationMinutes { get; set; }
    public int Repetitions { get; set; }
}

public class UpdateTrainingDetailsRequest
{
    public string Title { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public class AssignTrainingRequest
{
    public Guid AthleteId { get; set; }
}

public class CompleteTrainingRequest
{
    public Guid AthleteId { get; set; }
    public int StateRating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewTrainingRequest
{
    public Guid CoachId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
}

public class CancelTrainingRequest
{
    public string Reason { get; set; }
}

//public record CreateTrainingRequest(Guid CoachId, string Title, DateTime Date, string? Description);
//public record AddExerciseRequest(string Name, int DurationMinutes, int Repetitions);
//public record UpdateTrainingDetailsRequest(string Title, DateTime Date, string? Description);
//public record AssignTrainingRequest(Guid AthleteId);
//public record CompleteTrainingRequest(Guid AthleteId, int StateRating, string? Comment);
//public record ReviewTrainingRequest(Guid CoachId, int Score, string? Comment);
//public record CancelTrainingRequest(string Reason);