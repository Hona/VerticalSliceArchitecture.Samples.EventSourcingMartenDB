namespace EventSourcedSandbox.Domain.Records;

public record Record(Guid PlayerId, Guid MapId, DateTimeOffset Timestamp, TimeSpan Duration);
