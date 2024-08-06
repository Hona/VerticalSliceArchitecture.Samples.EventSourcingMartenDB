using EventSourcedSandbox.Domain.Maps;
using EventSourcedSandbox.Domain.Players;
using EventSourcedSandbox.Domain.Records;
using Marten;
using Marten.Events.Projections;
using Weasel.Core;

namespace EventSourcedSandbox.Common.Marten;

public static class DependencyInjection
{
    public static void AddAppMarten(this IServiceCollection services, string connectionString)
    {
        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.DatabaseSchemaName = "eventsourcedsandbox";
            options.AutoCreateSchemaObjects = AutoCreate.All;

            options.Projections.Add<PlayerProjector>(ProjectionLifecycle.Inline);

            options.Events.AddEventType<MapAddedEvent>();
            options.Events.AddEventType<PlayerRegisteredEvent>();
            options.Events.AddEventType<PlayerAchievedMapRecordEvent>();
        });
    }
}
