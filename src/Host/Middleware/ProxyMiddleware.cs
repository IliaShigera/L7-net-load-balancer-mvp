namespace LoadBalancer.Host.Middleware;

internal sealed class ProxyMiddleware
{
    private readonly InstanceRegistry _instanceRegistry;
    private readonly ILoadBalancingPolicy _loadBalancingPolicy;
    private readonly IHttpReverseProxy _proxy;
    private readonly ILogger<ProxyMiddleware> _logger;

    public ProxyMiddleware(
        RequestDelegate next,
        InstanceRegistry instanceRegistry,
        ILoadBalancingPolicy loadBalancingPolicy,
        IHttpReverseProxy proxy,
        ILogger<ProxyMiddleware> logger)
    {
        _ = next;
        _instanceRegistry = instanceRegistry;
        _loadBalancingPolicy = loadBalancingPolicy;
        _proxy = proxy;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CancellationToken ct)
    {
        var instance = _loadBalancingPolicy.Select(_instanceRegistry.ListAll());
        if (instance is not { IsHealthy: true })
        {
            _logger.LogWarning("No healthy instance found.");

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("No available instances.");
            return;
        }

        _logger.LogDebug("Routing request to {InstanceName} [{InstanceAddress}]", instance.Name, instance.Address);

        context.Request.Headers["X-Forwarded-Host"] = context.Request.Host.ToString();
        context.Request.Headers["X-Forwarded-Proto"] = context.Request.Scheme;

        await _proxy.ProxyAsync(context, instance, ct);
    }
}