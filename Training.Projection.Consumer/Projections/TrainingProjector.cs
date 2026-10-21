using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Training.Contracts;
using Training.Projection.Consumer.ReadModel;


namespace Training.Projection.Consumer.Projections
{
    public class TrainingProjector
    {

        private readonly Dictionary<Guid, TrainingView> _trainings = new();
        private readonly Dictionary<Guid, AthleteStatsView> _athletes = new();
        private readonly object _lock = new();

        public void Project(IEvent e)
        {
            lock (_lock)
            {
                if (e is TrainingCreated created)
                {

                    if (_trainings.ContainsKey(created.TrainingId))
                    {
                        Console.WriteLine($"[Дублікат] {created.TrainingId} вже існує (skip)");
                        return;
                    }

                    var newView = new TrainingView
                    {
                        Id = created.TrainingId,
                        CoachId = created.CoachId,
                        Title = created.Title,
                        Date = created.Date,
                        Description = created.Description,
                        Status = "Draft",
                        Version = created.Version,
                        LastUpdatedAt = created.Timestamp
                    };
                    _trainings[newView.Id] = newView;
                    Console.WriteLine($"[Read DB] Створено картку\n{newView}");
                    return;

                }

                if (!_trainings.TryGetValue(e.TrainingId, out var view))
                {
                    Console.WriteLine($"[Eventual consistency] Подія {e.GetType().Name} для невідомого тренування {e.TrainingId}. ");
                    return;
                }

                if (e.Version <= view.Version)
                {
                    Console.WriteLine($"[Дублікат] {e.GetType().Name} v{e.Version} вже оброблено (поточна v{view.Version}) ");
                    return;
                }

                // Пропуск у послідовності: якась подія загубилась
                if (e.Version > view.Version + 1) {
                    Console.WriteLine($"[Попередження] Очікувалась v{view.Version + 1}, отримано v{e.Version} — частину подій пропущено");
                }


                switch (e)
                {
                    case ExerciseAdded x:
                        view.Exercises.Add(new ExerciseView(
                            x.ExerciseId,
                            x.Name, 
                            x.DurationMinutes, 
                            x.Repetitions
                        ));
                        break;

                    case ExerciseRemoved r:
                        view.Exercises.RemoveAll(ex => ex.Id == r.ExerciseId);
                        break;

                    case TrainingDetailsUpdated u:
                        view.Title = u.Title;
                        view.Date = u.Date;
                        view.Description = u.Description;
                        break;

                    case TrainingAssigned a:
                        view.Status = "Assigned";
                        view.AthleteId = a.AthleteId;
                        GetStats(a.AthleteId).AssignedCount++;
                        break;

                    case TrainingCompleted c:
                        view.Status = "Completed";
                        view.StateRating = c.StateRating;
                        view.AthleteComment = c.Comment;

                        var stats = GetStats(c.AthleteId);

                        stats.CompletedCount++;
                        stats.TotalMinutes += view.TotalDurationMinutes;
                        stats.StateRatings.Add(c.StateRating);
                        break;

                    case TrainingReviewed rv:
                        view.Status = "Reviewed";
                        view.CoachScore = rv.Score;
                        view.CoachComment = rv.Comment;
                     
                        if (view.AthleteId is Guid athleteId)
                            GetStats(athleteId).CoachScores.Add(rv.Score);

                        break;

                    case TrainingCancelled cn:
                        view.Status = "Cancelled";
                        view.CancelReason = cn.Reason;

                        if (view.AthleteId is Guid cancelledFor)
                            GetStats(cancelledFor).CancelledCount++;

                        break;
                }

                view.Version = e.Version;
                view.LastUpdatedAt = e.Timestamp;

                Console.WriteLine($"[Read DB] Оновлено картку ({e.GetType().Name})\n{view}");
                if (view.AthleteId is Guid id && e is TrainingAssigned or TrainingCompleted or TrainingReviewed or TrainingCancelled)
                    Console.WriteLine(_athletes[id]);



            }

        }


        public void PrintAllTrainings()
        {
            lock (_lock)
            {
                Console.WriteLine($"\n===== Усі тренування ({_trainings.Count}) =====");
                foreach (var t in _trainings.Values.OrderBy(t => t.Date))
                    Console.WriteLine(t);
            }
        }

        public void PrintAthleteStats()
        {
            lock (_lock)
            {
                Console.WriteLine($"\n===== Статистика спортсменів ({_athletes.Count}) =====");
                foreach (var a in _athletes.Values)
                    Console.WriteLine(a);
            }
        }

        private AthleteStatsView GetStats(Guid athleteId)
        {
            if (!_athletes.TryGetValue(athleteId, out var stats))
            {
                stats = new AthleteStatsView { AthleteId = athleteId };
                _athletes[athleteId] = stats;
            }
            return stats;
        }

    }
}
