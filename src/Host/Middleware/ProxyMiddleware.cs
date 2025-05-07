namespace LoadBalancer.Host.Middleware;

internal sealed class ProxyMiddleware
{
    private readonly IInstanceRegistry _instanceRegistry;
    private readonly ILoadBalancingPolicy _loadBalancingPolicy;
    private readonly IBackendForwarder _backendForwarder;
    private readonly ILogger<ProxyMiddleware> _logger;

    public ProxyMiddleware(
        RequestDelegate next,
        IInstanceRegistry instanceRegistry,
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

    public async Task InvokeAsync(HttpContext context)
    {
        var healthyInstances = _instanceRegistry.ListAllHealthy(includeDrained: false);
        if (!healthyInstances.Any())
        {
            _logger.LogWarning("No healthy instance found for {Method} {Path} from {ClientIP}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("No available instances.", context.RequestAborted);
            return;
        }

        foreach (var instance in _loadBalancingPolicy.GetPreferredOrder(healthyInstances))
        {
            _logger.LogInformation("Routing {Method} {Path} to {InstanceName} [{InstanceAddress}] for {ClientIP}",
                context.Request.Method,
                context.Request.Path,
                instance.Name,
                instance.Address,
                context.Connection.RemoteIpAddress);

            try
            {
                context.Request.Headers["X-Forwarded-Host"] = context.Request.Host.ToString();
                context.Request.Headers["X-Forwarded-Proto"] = context.Request.Scheme;

                await _backendForwarder.ForwardAsync(context, instance, context.RequestAborted);
                _logger.LogInformation(
                    "Proxied {Method} {Path} to {InstanceName} [{InstanceAddress}] for {ClientIP}",
                    context.Request.Method,
                    context.Request.Path,
                    instance.Name,
                    instance.Address,
                    context.Connection.RemoteIpAddress);

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Proxy error: {InstanceName} [{InstanceAddress}] for {Method} {Path} from {ClientIP}",
                    instance.Name,
                    instance.Address,
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);
            }
        }

        _logger.LogWarning("All instances are unavailable for {Method} {Path} from {ClientIP}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsync("All instances are unavailable.", context.RequestAborted);
    }
}