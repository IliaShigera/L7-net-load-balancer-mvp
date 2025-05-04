namespace LoadBalancer.Core.State;

public sealed class Instance
{
    public required string Name { get; init; }
    public required string Address { get; init; }

    private int _isHealthy = -1;
    public bool IsHealthy => Volatile.Read(ref _isHealthy) == 1;

    public void MarkHealthy() => Volatile.Write(ref _isHealthy, 1);
    public void MarkUnhealthy() => Volatile.Write(ref _isHealthy, 0);
}