using EventSourcedSandbox.Domain.Records;
using Marten.Events.Projections;

namespace EventSourcedSandbox.Domain.Players;

public class PlayerProjection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Record> Records { get; set; } = [];
}

public sealed class PlayerAggregateProjection : MultiStreamProjection<PlayerProjection, Guid>
{
    public PlayerAggregateProjection()
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

    // TODO: Given there is is similar logic in MapAggregate, consider extracting this logic to a shared Domain service
    public void Apply(PlayerAchievedMapRecordEvent @event, PlayerProjection view)
    {
        view.Records = RecordService.TryInsertOrUpdateRecord(view.Records, @event);
    }
}
