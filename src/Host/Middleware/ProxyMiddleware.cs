namespace LoadBalancer.Host.Middleware;

internal sealed class ProxyMiddleware
{
    private readonly InstanceRegistry _instanceRegistry;
    private readonly ILoadBalancingPolicy _loadBalancingPolicy;
    private readonly IBackendForwarder _backendForwarder;
    private readonly ILogger<ProxyMiddleware> _logger;

    public ProxyMiddleware(
        RequestDelegate next,
        InstanceRegistry instanceRegistry,
        ILoadBalancingPolicy loadBalancingPolicy,
        IBackendForwarder backendForwarder,
        ILogger<ProxyMiddleware> logger)
    {
        _ = next;
        _instanceRegistry = instanceRegistry;
        _loadBalancingPolicy = loadBalancingPolicy;
        _backendForwarder = backendForwarder;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CancellationToken ct)
    {
        var healthyInstances = _instanceRegistry.ListHealthy();
        if (!healthyInstances.Any())
        {
            _logger.LogWarning("No healthy instance found.");

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("No available instances.", ct);
            return;
        }

        foreach (var instance in _loadBalancingPolicy.GetPreferredOrder(healthyInstances))
        {
            _logger.LogDebug("Routing request to {InstanceName} [{InstanceAddress}]", instance.Name, instance.Address);

            try
            {
                context.Request.Headers["X-Forwarded-Host"] = context.Request.Host.ToString();
                context.Request.Headers["X-Forwarded-Proto"] = context.Request.Scheme;

                await _backendForwarder.ForwardAsync(context, instance, ct);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proxy error: {InstanceName} [{InstanceAddress}]",
                    instance.Name,
                    instance.Address);
            }
        }

        _logger.LogWarning("All instances are unavailable.");
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsync("All instances are unavailable.", ct);
    }
}