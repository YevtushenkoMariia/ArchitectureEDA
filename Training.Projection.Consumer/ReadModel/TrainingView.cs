using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Training.Projection.Consumer.ReadModel
{

    public record ExerciseView(
        int Id, 
        string Name, 
        int DurationMinutes, 
        int Repetitions
        );

    public class TrainingView
    {
        public Guid Id { get; set; }
        public Guid CoachId { get; set; }
        public string Title { get; set; } = "";
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Draft";


        public List<ExerciseView> Exercises { get; } = new();
        public int ExercisesCount => Exercises.Count;
        public int TotalDurationMinutes => Exercises.Sum(e => e.DurationMinutes);   // вже порахована сума


        public Guid? AthleteId { get; set; }
        public int? StateRating { get; set; }        // самооцінка спортсмена
        public string? AthleteComment { get; set; }
        public int? CoachScore { get; set; }         // оцінка тренера
        public string? CoachComment { get; set; }
        public string? CancelReason { get; set; }


        public int Version { get; set; }             // остання застосована подія — для ідемпотентності
        public DateTime LastUpdatedAt { get; set; }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"┌─ ТРЕНУВАННЯ: {Title}  [{Status}]  v{Version}");
            sb.AppendLine($"│  Дата: {Date:dd.MM.yyyy HH:mm}   Id: {Id}");
            if (!string.IsNullOrWhiteSpace(Description))
                sb.AppendLine($"│  Опис: {Description}");

            sb.AppendLine($"│  Вправи ({ExercisesCount}, разом {TotalDurationMinutes} хв):");
            foreach (var e in Exercises)
                sb.AppendLine($"│    #{e.Id} {e.Name} — {e.DurationMinutes} хв × {e.Repetitions}");

            if (AthleteId is not null) 
                sb.AppendLine($"│  Спортсмен: {AthleteId}");
            if (StateRating is not null) 
                sb.AppendLine($"│  Самооцінка стану: {StateRating}/10  «{AthleteComment}»");
            if (CoachScore is not null) 
                sb.AppendLine($"│  Оцінка тренера: {CoachScore}/10  «{CoachComment}»");
            if (CancelReason is not null) 
                sb.AppendLine($"│  Скасовано: {CancelReason}");
           
            sb.Append("└─────────────────────────────────────────");
            
            return sb.ToString();
        }

    }
}
