using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Projection.Consumer.ReadModel
{
    public class AthleteStatsView
    {
        public Guid AthleteId { get; set; }
        public int AssignedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int TotalMinutes { get; set; }


        public List<int> StateRatings { get; } = new();
        public List<int> CoachScores { get; } = new();

        public double? AvgStateRating => StateRatings.Count > 0 ? StateRatings.Average() : null;
        public double? AvgCoachScore => CoachScores.Count > 0 ? CoachScores.Average() : null;


        public override string ToString() =>
       $"[СТАТИСТИКА] Спортсмен {AthleteId}: призначено {AssignedCount}, виконано {CompletedCount}, " +
       $"скасовано {CancelledCount}, хвилин тренувань {TotalMinutes}, " +
       $"сер. самооцінка {AvgStateRating?.ToString("0.0") ?? "—"}, " +
       $"сер. оцінка тренера {AvgCoachScore?.ToString("0.0") ?? "—"}";
    }
}
