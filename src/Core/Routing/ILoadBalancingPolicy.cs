namespace LoadBalancer.Core.Routing;

public interface ILoadBalancingPolicy
{
    Instance? Select(IReadOnlyList<Instance> instances);
}