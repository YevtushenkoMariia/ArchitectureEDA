using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Training.Contracts;
using Training.Projection.Consumer.Projections;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

const string QueueName = "training-projection";
const string BindingKey = "training.*";

var projector = new TrainingProjector();

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(EventRouting.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync(QueueName, EventRouting.ExchangeName, routingKey: BindingKey);
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

        Console.WriteLine($"\n[Отримано] {ea.RoutingKey} | {eventTypeName} v{@event.Version}");

        // Оновлюємо модель читання
        projector.Project(@event);

        // Підтверджуємо ПІСЛЯ успішного оновлення
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Помилка] {ex.Message}");
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
    }
};

await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer);

Console.WriteLine($"[Projection] Черга '{QueueName}' ← '{EventRouting.ExchangeName}' ({BindingKey})");
Console.WriteLine("[Projection] Команди: list — усі тренування, stats — статистика спортсменів, exit — вихід\n");

// Сторона ЧИТАННЯ: запити до готової моделі, без жодного звернення до Producer чи Event Store
while (true)
{
    var input = Console.ReadLine()?.Trim().ToLowerInvariant();
    if (input is null or "exit") break;
    if (input == "list") projector.PrintAllTrainings();
    else if (input == "stats") projector.PrintAthleteStats();
}

static string? GetHeader(IReadOnlyBasicProperties props, string key) =>
    props.Headers is not null && props.Headers.TryGetValue(key, out var value) && value is byte[] bytes
        ? Encoding.UTF8.GetString(bytes)
        : null;