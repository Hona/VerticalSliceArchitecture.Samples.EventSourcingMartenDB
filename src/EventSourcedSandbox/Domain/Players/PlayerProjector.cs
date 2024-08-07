using EventSourcedSandbox.Domain.Records;
using Marten.Events.Projections;

namespace EventSourcedSandbox.Domain.Players;

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
