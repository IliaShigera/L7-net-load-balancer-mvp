namespace LoadBalancer.Core.State;

public sealed class InstanceDefinition
{
    public required string Name { get; init; }
    public required string Address { get; init; }
}