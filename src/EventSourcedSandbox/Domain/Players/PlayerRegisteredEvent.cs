namespace EventSourcedSandbox.Domain.Players;

public record PlayerRegisteredEvent(DateTimeOffset Timestamp, Guid PlayerId, string PlayerName);
