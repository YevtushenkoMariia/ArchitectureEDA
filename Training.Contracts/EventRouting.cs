using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Contracts
{
    public static class EventRouting
    {

        public const string ExchangeName = "training.events";

        public static string GetRoutingKey(IEvent e) => e switch
        {
            TrainingCreated => "training.created",
            ExerciseAdded => "training.exercise_added",
            ExerciseRemoved => "training.exercise_removed",
            TrainingDetailsUpdated => "training.details_updated",
            TrainingAssigned => "training.assigned",
            TrainingCompleted => "training.completed",
            TrainingReviewed => "training.reviewed",
            TrainingCancelled => "training.cancelled",
            _ => throw new ArgumentException($"Невідомий тип події: {e.GetType().Name}")
        };

        public static readonly Dictionary<string, Type> EventTypes = new()
        {
            [nameof(TrainingCreated)] = typeof(TrainingCreated),
            [nameof(ExerciseAdded)] = typeof(ExerciseAdded),
            [nameof(ExerciseRemoved)] = typeof(ExerciseRemoved),
            [nameof(TrainingDetailsUpdated)] = typeof(TrainingDetailsUpdated),
            [nameof(TrainingAssigned)] = typeof(TrainingAssigned),
            [nameof(TrainingCompleted)] = typeof(TrainingCompleted),
            [nameof(TrainingReviewed)] = typeof(TrainingReviewed),
            [nameof(TrainingCancelled)] = typeof(TrainingCancelled),
        };


    }
}
