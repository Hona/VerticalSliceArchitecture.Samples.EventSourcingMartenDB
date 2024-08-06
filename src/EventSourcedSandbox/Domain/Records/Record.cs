namespace EventSourcedSandbox.Domain.Records;

public record Record(Guid PlayerId, Guid MapId, TimeSpan Duration);
