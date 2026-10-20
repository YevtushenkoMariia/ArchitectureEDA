using System.Text;
using Training.Producer.API.Infrastructure;

Console.OutputEncoding = Encoding.UTF8;   // щоб українські повідомлення нормально виводились у консоль

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Singleton: один екземпляр на весь час роботи — інакше "база" очищалась би з кожним запитом
builder.Services.AddSingleton<InMemoryEventStore>();

// Підключаємось до RabbitMQ один раз при старті
var publisher = await RabbitMqPublisher.CreateAsync("localhost");
builder.Services.AddSingleton<IEventPublisher>(publisher);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();