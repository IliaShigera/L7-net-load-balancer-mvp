namespace LoadBalancer.Core.Routing;

public sealed class RoundRobinPolicy : ILoadBalancingPolicy
{
    private int _counter = -1;

    public Instance? Select(IReadOnlyList<Instance> instances)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (instances is null || instances.Count == 0)
            return null;

        var index = Interlocked.Increment(ref _counter) % instances.Count;
        return instances[index];
    }
}