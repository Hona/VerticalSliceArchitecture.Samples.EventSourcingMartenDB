using EventSourcedSandbox.Domain.Records;
using Marten.Events.Aggregation;

namespace EventSourcedSandbox.Domain.Maps;

public class MapAggregate : SingleStreamProjection<MapAggregate>
{
    public Guid Id { get; set; }
    public string MapName { get; set; }
    public List<Record> Records { get; set; }

    public void Apply(MapAddedEvent added)
    {
        Id = added.MapId;
        MapName = added.MapName;
        Records = new();
    }

    /// <summary>
    /// Players can only have one record per map. If a player already has a record for the map,
    /// the new record will replace the old one only if the new record's duration is faster.
    /// </summary>
    /// <param name="playerAchievedRecord"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public void Apply(PlayerAchievedMapRecordEvent playerAchievedRecord)
    {
        Records = Records
            .GroupBy(record => new { record.PlayerId, record.MapId })
            .SelectMany(group =>
            {
                var existingRecord = group.FirstOrDefault(r =>
                    r.PlayerId == playerAchievedRecord.PlayerId
                    && r.MapId == playerAchievedRecord.MapId
                );

                if (
                    existingRecord == null
                    || playerAchievedRecord.Duration < existingRecord.Duration
                )
                {
                    // Replace with the new record if it's faster or if no existing record
                    return group
                        .Where(r =>
                            !(
                                r.PlayerId == playerAchievedRecord.PlayerId
                                && r.MapId == playerAchievedRecord.MapId
                            )
                        )
                        .Append(
                            new Record(
                                playerAchievedRecord.PlayerId,
                                playerAchievedRecord.MapId,
                                playerAchievedRecord.Timestamp,
                                playerAchievedRecord.Duration
                            )
                        );
                }

                // Keep the existing records, no need to append the new one
                return group;
            })
            .OrderBy(x => x.Duration)
            .ToList();
    }
}
