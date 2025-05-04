namespace LoadBalancer.Core.State;

public sealed class InstanceRegistry
{
    private readonly List<Instance> _instances;

    public InstanceRegistry(List<Instance> instances)
    {
        if (instances is null || instances.Count == 0)
            throw new ArgumentException("Instances must not be empty.", nameof(instances));
            
        _instances = instances;
    }

    public IReadOnlyList<Instance> ListHealthy() => _instances
        .Where(i => i.IsHealthy)
        .ToList()
        .AsReadOnly();
}