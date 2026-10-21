using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Training.Contracts;

namespace Training.Producer.API.Infrastructure;

public interface IEventPublisher
{
    Task PublishAsync(IEvent @event);
}

public sealed class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    // для використання каналу тільки один потоком, не кількома одночасно
    private readonly SemaphoreSlim _publishLock = new(1, 1);   

    private RabbitMqPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    // Асинхронна "фабрика": у 7.x підключення асинхронне, а конструктор не може бути async
    //???
    public static async Task<RabbitMqPublisher> CreateAsync(string hostName)
    {
        var factory = new ConnectionFactory { HostName = hostName };   // логін/пароль за замовчуванням guest/guest
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Оголошення обмінника ідемпотентне: якщо вже існує з такими ж параметрами — нічого не станеться
        await channel.ExchangeDeclareAsync(
            exchange: EventRouting.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,          // переживе перезапуск RabbitMQ
            autoDelete: false);

        return new RabbitMqPublisher(connection, channel);
    }


    //RabbitMQ logic
    public async Task PublishAsync(IEvent @event)
    {
        // Серіалізуємо за РЕАЛЬНИМ типом події, інакше в JSON потраплять лише поля IEvent
        var json = JsonSerializer.Serialize(@event, @event.GetType());
        var body = Encoding.UTF8.GetBytes(json);

        // routing key залежить від типу події
        var routingKey = EventRouting.GetRoutingKey(@event);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true,                          
            MessageId = @event.EventId.ToString(),
            Headers = new Dictionary<string, object?>
            {
                //Додавання типу події
                ["EventType"] = @event.GetType().Name 
            }
        };

        await _publishLock.WaitAsync();
        try
        {
            await _channel.BasicPublishAsync(
                exchange: EventRouting.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);
        }
        finally
        {
            _publishLock.Release();
        }

        Console.WriteLine($"[Publisher] {routingKey} (v{@event.Version}) → {EventRouting.ExchangeName}");
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}