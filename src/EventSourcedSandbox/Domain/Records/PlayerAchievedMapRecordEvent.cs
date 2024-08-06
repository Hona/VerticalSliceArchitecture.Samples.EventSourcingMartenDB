namespace EventSourcedSandbox.Domain.Records;

public record PlayerAchievedMapRecordEvent(
    DateTimeOffset Timestamp,
    Guid MapId,
    Guid PlayerId,
    TimeSpan Duration
);
