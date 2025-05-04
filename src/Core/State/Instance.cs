namespace LoadBalancer.Core.State;

public sealed class Instance
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    
    // todo: is this safe ?
    private bool _isHealthy;
    public bool IsHealthy => _isHealthy;

    public void MarkHealthy() => _isHealthy = true;
    public void MarkUnhealthy() => _isHealthy = false;
}