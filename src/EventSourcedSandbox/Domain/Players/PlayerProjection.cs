using EventSourcedSandbox.Domain.Records;

namespace EventSourcedSandbox.Domain.Players;

public class PlayerProjection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Record> Records { get; set; } = [];
}
