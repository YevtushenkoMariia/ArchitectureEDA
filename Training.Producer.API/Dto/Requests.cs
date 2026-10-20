namespace Training.Producer.API.Dto;

public record CreateTrainingRequest(Guid CoachId, string Title, DateTime Date, string? Description);
public record AddExerciseRequest(string Name, int DurationMinutes, int Repetitions);
public record UpdateTrainingDetailsRequest(string Title, DateTime Date, string? Description);
public record AssignTrainingRequest(Guid AthleteId);
public record CompleteTrainingRequest(Guid AthleteId, int StateRating, string? Comment);
public record ReviewTrainingRequest(Guid CoachId, int Score, string? Comment);
public record CancelTrainingRequest(string Reason);