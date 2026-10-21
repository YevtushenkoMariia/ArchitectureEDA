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

        public static string GetRoutingKey(IEvent e)
        {
            switch (e)
            {
                case TrainingCreated:
                    return "training.created";

                case ExerciseAdded:
                    return "training.exercise_added";

                case ExerciseRemoved:
                    return "training.exercise_removed";

                case TrainingDetailsUpdated:
                    return "training.details_updated";

                case TrainingAssigned:
                    return "training.assigned";

                case TrainingCompleted:
                    return "training.completed";

                case TrainingReviewed:
                    return "training.reviewed";

                case TrainingCancelled:
                    return "training.cancelled";

                default:
                    throw new ArgumentException("Невідомий тип події: " + e.GetType().Name);
            }
        }


        // співставлення назва події - тип події 
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
