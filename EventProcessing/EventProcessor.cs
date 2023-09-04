using System.Text.Json;
using AutoMapper;
using CommandService.Data;
using CommandService.Dtos;
using CommandService.Models;

namespace CommandService.EventProcessing;

public class EventProcessor : IEventProcessor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMapper _mapper;

    public EventProcessor(IServiceScopeFactory serviceScopeFactory, AutoMapper.IMapper mapper)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _mapper = mapper;
    }
    
    public void ProcessEvent(string message)
    {
        var eventType = DetermineEvent(message);
        switch (eventType)
        {
            case EventType.PlatformPublished:
                AddPlatform(message);
                break;
            default:
                break;
        }
    }
    
    private EventType DetermineEvent(string notificationMessage)
    {
        Console.WriteLine("--> Determining Event");
        var genericEventDto = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);
        switch (genericEventDto?.Event)
        {
            case "Platform_Published":
                Console.WriteLine("--> Platform Published Event Detected");
                return EventType.PlatformPublished;
            default:
                Console.WriteLine("--> Could not determine the event type");
                return EventType.Undetermined;
        }
    }
    
    private void AddPlatform(string platformPublishedMessage)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICommandRepo>();
        var platformPublishedDto = JsonSerializer.Deserialize<PlatformPublishedDto>(platformPublishedMessage);
        try
        {
            var plat = _mapper.Map<Platform>(platformPublishedDto);
            if (!repo.ExternalPlatformExists(plat.ExternalId))
            {
                repo.CreatePlatform(plat);
                repo.SaveChanges();
                Console.WriteLine("--> Platform added!");
            }
            else
            {
                Console.WriteLine("--> Platform already exists...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not add Platform to DB: {ex.Message}");
        }
    }
    
    enum EventType
    {
        PlatformPublished,
        Undetermined
    }
}