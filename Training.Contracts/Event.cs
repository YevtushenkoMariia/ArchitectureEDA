using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Contracts
{
    public class TrainingCreated : EventBase
    {
        public Guid CoachId { get; init; }
        public string Title { get; init; } = "";
        public DateTime Date { get; init; }
        public string? Description { get; init; }
    }

    public class ExerciseAdded : EventBase
    {
        public int ExerciseId { get; init; }
        public string Name { get; init; } = "";
        public int DurationMinutes { get; init; }
        public int Repetitions { get; init; }

        public Guid? AthleteId { get; init; }
    }

    public class ExerciseRemoved : EventBase
    {
        public int ExerciseId { get; init; }
        public Guid? AthleteId { get; init; }
    }

    public class TrainingDetailsUpdated : EventBase
    {
        public string Title { get; init; } = "";
        public DateTime Date { get; init; }
        public string? Description { get; init; }
        public Guid? AthleteId { get; init; }
    }

    public class TrainingAssigned : EventBase
    {
        public Guid AthleteId { get; init; }
      
    }

    public class TrainingCompleted : EventBase
    {
        public Guid AthleteId { get; init; }
        public int StateRating { get; init; }
        public string? Comment { get; init; }
    }

    public class TrainingReviewed : EventBase
    {
        public Guid CoachId { get; init; }
        public int Score { get; init; }
        public string? Comment { get; init; }
    }

    public class TrainingCancelled : EventBase
    {
        public string Reason { get; init; } = "";
        public Guid? AthleteId { get; init; }
    }


}
