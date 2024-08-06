namespace EventSourcedSandbox.Domain.Records;

public record PlayerAchievedMapRecordEvent(Guid MapId, Guid PlayerId, TimeSpan Duration);
