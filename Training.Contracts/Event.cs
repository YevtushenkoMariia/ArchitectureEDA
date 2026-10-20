using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Contracts
{
    public record TrainingCreated : EventBase
    {
        public Guid CoachId { get; init; }
        public string Title { get; init; } = "";
        public DateTime Date { get; init; }
        public string? Description { get; init; }
    }

    public record ExerciseAdded : EventBase
    {
        public int ExerciseId { get; init; }
        public string Name { get; init; } = "";
        public int DurationMinutes { get; init; }
        public int Repetitions { get; init; }

        public Guid? AthleteId { get; init; }
    }

    public record ExerciseRemoved : EventBase
    {
        public int ExerciseId { get; init; }
        public Guid? AthleteId { get; init; }
    }

    public record TrainingDetailsUpdated : EventBase
    {
        public string Title { get; init; } = "";
        public DateTime Date { get; init; }
        public string? Description { get; init; }
        public Guid? AthleteId { get; init; }
    }

    public record TrainingAssigned : EventBase
    {
        public Guid AthleteId { get; init; }
      
    }

    public record TrainingCompleted : EventBase
    {
        public Guid AthleteId { get; init; }
        public int StateRating { get; init; }
        public string? Comment { get; init; }
    }

    public record TrainingReviewed : EventBase
    {
        public Guid CoachId { get; init; }
        public int Score { get; init; }
        public string? Comment { get; init; }
    }

    public record TrainingCancelled : EventBase
    {
        public string Reason { get; init; } = "";
        public Guid? AthleteId { get; init; }
    }


}
