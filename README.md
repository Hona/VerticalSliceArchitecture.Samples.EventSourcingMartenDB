# Vertical Slice Architecture with Event Sourcing using Marten DB

> The goal of this sample is to determine the viability for me to use Event Sourcing + Marten DB on https://tf2jump.xyz

## Domain

Here there are a few main ideas, from the TF2 Jump real world example (https://github.com/Hona/TF2Jump.xyz)

Imagine there are `Map`s. Each `Map` can be speedran by `Player`s. Each `Player` can run many `Map`s.

Here are the events:

```csharp
public record MapAddedEvent(Guid MapId, string MapName);
public record PlayerRegisteredEvent(Guid PlayerId, string PlayerName);
public record PlayerAchievedMapRecordEvent(Guid MapId, Guid PlayerId, TimeSpan Duration);
```

We then end up with the write aggregate model:

```csharp
public sealed record MapAggregate(Guid Id, string MapName, List<Record> Records)
{
    public static MapAggregate Create(MapAddedEvent added) => new(added.MapId, added.MapName, []);

    public static MapAggregate Apply(
        PlayerAchievedMapRecordEvent playerAchievedRecord,
        MapAggregate mapAggregate
    ) =>
        mapAggregate with
        {
            Records = RecordService.TryInsertOrUpdateRecord(
                mapAggregate.Records,
                playerAchievedRecord
            )
        };
}
```

Note: There is no write model for Players, as in this sample there is no need for business rules inside a DDD context.

Then, a number of projections for all query (read) operations.

```csharp
public class PlayerProjection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Record> Records { get; set; } = [];
}

public class PlayerProjector : MultiStreamProjection<PlayerProjection, Guid>
{
    public PlayerProjector()
    {
        Identity<PlayerRegisteredEvent>(x => x.PlayerId);
        Identity<PlayerAchievedMapRecordEvent>(x => x.PlayerId);
    }

    public void Apply(PlayerRegisteredEvent @event, PlayerProjection view)
    {
        view.Id = @event.PlayerId;
        view.Name = @event.PlayerName;
        view.Records = [];
    }

    public void Apply(PlayerAchievedMapRecordEvent @event, PlayerProjection view)
    {
        view.Records = RecordService.TryInsertOrUpdateRecord(view.Records, @event);
    }
}

```

Marten is configured

```csharp
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
```


We can query the Aggregate (for writes only) like so

```csharp
var response = await session.Events.AggregateStreamAsync<MapAggregate>(
    request.MapId,
    token: cancellationToken
);
```

Projections can be treated like traditional Marten documents:

```csharp
var response = await session.LoadAsync<PlayerProjection>(
    request.PlayerId,
    cancellationToken
);
```

A VSA endpoint/slice

```csharp
using EventSourcedSandbox.Domain.Players;
using Marten;

namespace EventSourcedSandbox.Features.Players;

internal sealed record ViewPlayerRequest(Guid PlayerId);

internal sealed class ViewPlayerQuery(IQuerySession session)
    : Endpoint<ViewPlayerRequest, Results<Ok<PlayerProjection>, NotFound>>
{
    public override void Configure()
    {
        Get($"/players/{{{nameof(ViewPlayerRequest.PlayerId)}}}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        ViewPlayerRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await session.LoadAsync<PlayerProjection>(
            request.PlayerId,
            cancellationToken
        );

        if (response is null)
        {
            await SendResultAsync(TypedResults.NotFound());
            return;
        }

        await SendResultAsync(TypedResults.Ok(response));
    }
}
```

Note that there is a nice synergy with CQRS, VSA and Event Sourcing. The `ViewPlayerQuery` is a query endpoint, and the `PlayerProjector` is a projection. The `MapAggregate` is a write model.

Additionally, if you want to see the seed data I'm using for testing:

```csharp
public async Task SeedSampleDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();

    var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

    await store.Advanced.ResetAllData();

    await using var session = store.LightweightSession();

    var rootTimeStamp = DateTimeOffset.Now;

    // Add some maps

    var map1 = Guid.NewGuid();
    var map2 = Guid.NewGuid();

    var mapRegistered1 = new MapAddedEvent(map1, "jump_beef");
    var mapRegistered2 = new MapAddedEvent(map2, "jump_ice");

    session.Events.StartStream(map1, mapRegistered1);
    session.Events.StartStream(map2, mapRegistered2);

    // Save the pending changes to db
    await session.SaveChangesAsync();

    // Add some players

    var player1 = Guid.NewGuid();
    var player2 = Guid.NewGuid();
    var player3 = Guid.NewGuid();
    var player4 = Guid.NewGuid();
    var player5 = Guid.NewGuid();

    var playerRegistered1 = new PlayerRegisteredEvent(player1, "Alice");
    var playerRegistered2 = new PlayerRegisteredEvent(player2, "Bob");
    var playerRegistered3 = new PlayerRegisteredEvent(player3, "Charlie");
    var playerRegistered4 = new PlayerRegisteredEvent(player4, "David");
    var playerRegistered5 = new PlayerRegisteredEvent(player5, "Eve");

    session.Events.StartStream(player1, playerRegistered1);
    session.Events.StartStream(player2, playerRegistered2);
    session.Events.StartStream(player3, playerRegistered3);
    session.Events.StartStream(player4, playerRegistered4);
    session.Events.StartStream(player5, playerRegistered5);

    // Save the pending changes to db
    await session.SaveChangesAsync();

    // Register some records

    var record1 = new PlayerAchievedMapRecordEvent(map1, player1, TimeSpan.FromSeconds(1000));
    var record2 = new PlayerAchievedMapRecordEvent(map1, player2, TimeSpan.FromSeconds(900));
    var record3 = new PlayerAchievedMapRecordEvent(map1, player3, TimeSpan.FromSeconds(800));
    var record4 = new PlayerAchievedMapRecordEvent(map1, player4, TimeSpan.FromSeconds(700));
    var record5 = new PlayerAchievedMapRecordEvent(map1, player5, TimeSpan.FromSeconds(600));
    var record6 = new PlayerAchievedMapRecordEvent(map2, player1, TimeSpan.FromSeconds(500));
    var record7 = new PlayerAchievedMapRecordEvent(map2, player2, TimeSpan.FromSeconds(400));
    var record8 = new PlayerAchievedMapRecordEvent(map2, player3, TimeSpan.FromSeconds(300));
    var record9 = new PlayerAchievedMapRecordEvent(map2, player4, TimeSpan.FromSeconds(200));
    var record10 = new PlayerAchievedMapRecordEvent(map2, player5, TimeSpan.FromSeconds(100));

    session.Events.Append(map1, record1);
    session.Events.Append(map1, record2);
    session.Events.Append(map1, record3);
    session.Events.Append(map1, record4);
    session.Events.Append(map1, record5);
    session.Events.Append(map2, record6);
    session.Events.Append(map2, record7);
    session.Events.Append(map2, record8);
    session.Events.Append(map2, record9);
    session.Events.Append(map2, record10);

    // Save the pending changes to db
    await session.SaveChangesAsync();
}
```