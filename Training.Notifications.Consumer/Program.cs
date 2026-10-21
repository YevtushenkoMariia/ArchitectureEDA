using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Training.Contracts;


Console.OutputEncoding = Encoding.UTF8;

const string QueueName = "notifications";

// Фільтр: тільки події, про які треба сповістити користувача
string[] bindingKeys =
[
    "*.assigned", 
    "*.completed", 
    "*.reviewed", 
    "*.cancelled",
    "*.details_updated", 
    "*.exercise_added", 
    "*.exercise_removed"
];

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(EventRouting.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

// Окрема черга — незалежна від черги проєкції
await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);

// Кілька bindings до однієї черги
foreach (var key in bindingKeys)
    await channel.QueueBindAsync(QueueName, EventRouting.ExchangeName, routingKey: key);

await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        var eventTypeName = GetHeader(ea.BasicProperties, "EventType");
        var json = Encoding.UTF8.GetString(ea.Body.Span);

        if (eventTypeName is null || !EventRouting.EventTypes.TryGetValue(eventTypeName, out var eventType))
        {
            Console.WriteLine($"[!] Невідомий тип події: {eventTypeName}");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var @event = (IEvent)JsonSerializer.Deserialize(json, eventType)!;

        // Шаблон { AthleteId: Guid athlete } спрацьовує лише тоді, коли AthleteId НЕ null
        var notification = BuildNotification(@event);

        if (notification is not null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Сповіщення] ({ea.RoutingKey}) {notification}");
        }
        else if (@event is TrainingCancelled or TrainingDetailsUpdated or ExerciseAdded or ExerciseRemoved)
        {
            // Другий рівень фільтрації — за вмістом: тренування ще не призначене, сповіщати нікого
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[Пропущено] ({ea.RoutingKey}) тренування {@event.TrainingId} ще не призначене — сповіщати нікого");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Неочікувана подія {eventTypeName} ({ea.RoutingKey}) — перевірте bindings черги");
        }
        Console.ResetColor();

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Помилка] {ex.Message}");
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
    }
};

await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer);

Console.WriteLine($"[Notifications] Черга '{QueueName}' ← '{EventRouting.ExchangeName}'");
Console.WriteLine($"[Notifications] Фільтр (bindings): {string.Join(", ", bindingKeys)}");
Console.WriteLine("[Notifications] Очікування подій... (Enter — вихід)\n");
Console.ReadLine();

static string? GetHeader(IReadOnlyBasicProperties props, string key) =>
    props.Headers is not null && props.Headers.TryGetValue(key, out var value) && value is byte[] bytes
        ? Encoding.UTF8.GetString(bytes)
        : null;

string? BuildNotification(IEvent @event)
{
    switch (@event)
    {
        case TrainingAssigned a:
            return $"Спортсмену {a.AthleteId}: вам призначено нове тренування {a.TrainingId}.";

        case TrainingCompleted c:
            {
                string text = $"Тренеру: спортсмен {c.AthleteId} виконав тренування {c.TrainingId}. " +
                              $"Самооцінка стану: {c.StateRating}/10";

                if (string.IsNullOrWhiteSpace(c.Comment))
                    text += ".";
                else
                    text += $", коментар: «{c.Comment}».";

                return text;
            }

        case TrainingReviewed r:
            {
                string text = $"Спортсмену: тренер оцінив ваше тренування {r.TrainingId} на {r.Score}/10";

                if (string.IsNullOrWhiteSpace(r.Comment))
                    text += ".";
                else
                    text += $" — «{r.Comment}».";

                return text;
            }

        case TrainingCancelled x:
            if (x.AthleteId == null)
                return null;

            return $"Спортсмену {x.AthleteId.Value}: тренування {x.TrainingId} скасовано. Причина: {x.Reason}.";

        case TrainingDetailsUpdated u:
            if (u.AthleteId == null)
                return null;

            return $"Спортсмену {u.AthleteId.Value}: у тренуванні {u.TrainingId} змінено деталі — " +
                   $"«{u.Title}», {u.Date:dd.MM.yyyy HH:mm}.";

        case ExerciseAdded ea:
            if (ea.AthleteId == null)
                return null;

            return $"Спортсмену {ea.AthleteId.Value}: до тренування {ea.TrainingId} додано вправу " +
                   $"«{ea.Name}» ({ea.DurationMinutes} хв).";

        case ExerciseRemoved er:
            if (er.AthleteId == null)
                return null;

            return $"Спортсмену {er.AthleteId.Value}: з тренування {er.TrainingId} прибрано вправу #{er.ExerciseId}.";

        default:
            return null;
    }
}
