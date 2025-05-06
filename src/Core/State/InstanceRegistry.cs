namespace LoadBalancer.Core.State;

public sealed class InstanceRegistry : IInstanceRegistry
{
    private readonly ConcurrentDictionary<string, Instance> _instances = [];
    private object _lock = new();

    public void Add(Instance instance) => _instances.TryAdd(instance.Name, instance);

    public Instance? FindByName(string name) => _instances.GetValueOrDefault(name);

    public IReadOnlyList<Instance> ListAll() =>
        _instances.Values
            .ToList()
            .AsReadOnly();

    public IReadOnlyList<Instance> ListAllHealthy(bool includeDrained) =>
        _instances.Values
            .Where(i => i.IsHealthy && (includeDrained || !i.IsDrained))
            .ToList()
            .AsReadOnly();


    public void ReplaceAll(IEnumerable<Instance> newInstances)
    {
        var map = newInstances.ToDictionary(i => i.Name);

        lock (_lock)
        {
            _instances.Clear();
            foreach (var kv in map)
                _instances.TryAdd(kv.Key, kv.Value);
        }
    }

    public void Remove(string name) => _instances.TryRemove(name, out _);
}