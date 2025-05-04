namespace LoadBalancer.Core.Health;

public interface IInstanceHealthChecker
{
    Task<bool> IsAliveAsync(Instance instance, CancellationToken ct);
}