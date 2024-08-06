using EventSourcedSandbox.Domain.Records;
using Marten.Events.Aggregation;
using Marten.Events.Projections;

namespace EventSourcedSandbox.Domain.Players;

public class PlayerAggregate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Record> Records { get; set; }
}

public sealed class PlayerAggregateProjection : MultiStreamProjection<PlayerAggregate, Guid>
{
    public PlayerAggregateProjection()
    {
        Identity<PlayerRegisteredEvent>(x => x.PlayerId);
        Identity<PlayerAchievedMapRecordEvent>(x => x.MapId);
    }

    public static void Apply(PlayerRegisteredEvent registered, PlayerAggregate projection)
    {
        projection.Id = registered.PlayerId;
        projection.Name = registered.PlayerName;
        projection.Records = [];
    }

    // TODO: Given there is is similar logic in MapAggregate, consider extracting this logic to a shared Domain service
    public static void Apply(
        PlayerAchievedMapRecordEvent playerAchievedRecord,
        PlayerAggregate projection
    )
    {
        projection.Records = projection
            .Records.GroupBy(record => record.MapId)
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
                        .Where(r => r.PlayerId != playerAchievedRecord.PlayerId)
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
