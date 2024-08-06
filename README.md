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