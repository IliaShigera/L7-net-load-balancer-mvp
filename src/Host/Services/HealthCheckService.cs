namespace LoadBalancer.Host.Services;

internal sealed class HealthCheckService : BackgroundService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IInstanceRegistry _instanceRegistry;
    private readonly HealthCheckSettings _settings;
    private readonly IInstanceHealthChecker _healthChecker;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IInstanceRegistry instanceRegistry,
        IOptions<HealthCheckSettings> options,
        IInstanceHealthChecker healthChecker)
    {
        _logger = logger;
        _instanceRegistry = instanceRegistry;
        _settings = options.Value;
        _healthChecker = healthChecker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var instances = _instanceRegistry.ListAll();
            foreach (var instance in instances)
            {
                var isAlive = await _healthChecker.IsAliveAsync(instance, stoppingToken);
                if (isAlive) instance.MarkHealthy();
                else instance.MarkUnhealthy();

                MetricsRegistry.InstanceHealth
                    .WithLabels(instance.Name)
                    .Set(instance.IsHealthy ? 1 : 0);

                _logger.LogInformation("Health check: {InstanceName} [{InstanceAddress}] is {Status}",
                    instance.Name,
                    instance.Address,
                    instance.IsHealthy ? "healthy" : "unhealthy");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);
        }
    }
}