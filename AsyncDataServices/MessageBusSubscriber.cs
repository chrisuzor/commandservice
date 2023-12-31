using System.Text;
using CommandService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandService.AsyncDataServices;

public class MessageBusSubscriber : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IEventProcessor _eventProcessor;
    private IConnection _connection;
    private IModel _channel;
    private string _queueName;

    public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
    {
        _configuration = configuration;
        _eventProcessor = eventProcessor;
        InitializeRabbitMQ();
    }
    
    private void InitializeRabbitMQ()
    {
        Console.WriteLine($"--> Initializing MessageBus on with hostname {_configuration["RabbitMQHost"]} port...{_configuration["RabbitMQPort"]}");
        var factory = new ConnectionFactory() {HostName = _configuration["RabbitMQHost"], Port = int.Parse(_configuration["RabbitMQPort"])};
        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);
            _queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queue: _queueName, exchange: "trigger", routingKey: "");
            Console.WriteLine("--> Listening on the Message Bus...");
            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
            Console.WriteLine("--> Connected to the Message Bus");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
        }
    }
    
    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> RabbitMQ Connection Shutdown");
    }
    
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
       stoppingToken.ThrowIfCancellationRequested();
       var consumer = new EventingBasicConsumer(_channel);
       
       consumer.Received += (ModuleHandle, ea) =>
       {
           Console.WriteLine("--> Event Received!");
           var body = ea.Body;
           var notificationMessage = Encoding.UTF8.GetString(body.ToArray());
           _eventProcessor.ProcessEvent(notificationMessage);
       };
       
       if (_channel != null)
       {
           _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
       }
       else
       {
           Console.WriteLine("--> Channel is null");
       }

       return Task.CompletedTask;
    }
}