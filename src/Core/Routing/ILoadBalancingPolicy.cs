namespace LoadBalancer.Core.Routing;

public interface ILoadBalancingPolicy
{
    IEnumerable<Instance> GetPreferredOrder(IReadOnlyList<Instance> instances);
}