using EventSourcedSandbox.Domain.Records;
using Marten.Events.Aggregation;

namespace EventSourcedSandbox.Domain.Maps;

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
