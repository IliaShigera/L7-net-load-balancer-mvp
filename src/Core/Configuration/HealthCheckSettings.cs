namespace LoadBalancer.Core.Configuration;

public sealed class HealthCheckSettings
{
    public required int IntervalSeconds { get; init; }
    public required int TimeoutSeconds { get; init; }
}