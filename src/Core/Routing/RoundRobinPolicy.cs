namespace LoadBalancer.Core.Routing;

public sealed class RoundRobinPolicy : ILoadBalancingPolicy
{
    private int _counter = -1;

    public IEnumerable<Instance> GetPreferredOrder(IReadOnlyList<Instance> instances)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (instances is null || instances.Count == 0)
            yield break;
        
        // todo: what if the index gets over max ?
        var index = Interlocked.Increment(ref _counter) % instances.Count;
        for (var i = 0; i < instances.Count; i++)
            yield return instances[(index + i) % instances.Count];
    }
}