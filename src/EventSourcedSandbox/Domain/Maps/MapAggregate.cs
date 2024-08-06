using EventSourcedSandbox.Domain.Records;
using Marten.Events.Aggregation;

namespace EventSourcedSandbox.Domain.Maps;

public sealed record MapAggregate(Guid Id, string MapName, List<Record> Records)
{
    public static MapAggregate Create(MapAddedEvent added) => new(added.MapId, added.MapName, []);

    /// <summary>
    /// Players can only have one record per map. If a player already has a record for the map,
    /// the new record will replace the old one only if the new record's duration is faster.
    /// </summary>
    /// <param name="playerAchievedRecord"></param>
    /// <param name="mapAggregate"></param>
    /// <returns></returns>
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
