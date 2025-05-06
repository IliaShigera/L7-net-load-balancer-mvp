namespace LoadBalancer.Core.State;

public interface IInstanceRegistry
{
    void Add(Instance instance);
    Instance? FindByName(string name);
    IReadOnlyList<Instance> ListAll();
    IReadOnlyList<Instance> ListAllHealthy(bool includeDrained);
    void ReplaceAll(IEnumerable<Instance> newInstances);
    void Remove(string name);
}