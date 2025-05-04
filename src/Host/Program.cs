var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOptions<HealthCheckSettings>()
    .Bind(builder.Configuration.GetRequiredSection(ConfigSectionKeys.HealthCheck))
    .Validate(opts => opts is { IntervalSeconds: > 0, TimeoutSeconds: > 0 }, "Invalid health check options")
    .ValidateOnStart();

builder.Services.AddSingleton<InstanceRegistry>(_ =>
{
    var instances = builder.Configuration
        .GetSection(ConfigSectionKeys.Instances)
        .Get<List<Instance>>() ?? [];

    if (instances.Count == 0)
        throw new ArgumentException("No instances configured.", nameof(instances));

    return new InstanceRegistry(instances);
});

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<ILoadBalancingPolicy, RoundRobinPolicy>();
builder.Services.AddSingleton<IInstanceHealthChecker, InstanceHealthChecker>();
builder.Services.AddSingleton<IBackendForwarder, BackendForwarder>();

builder.Services.AddHostedService<HealthCheckService>();

await using var app = builder.Build();
app.UseMiddleware<ProxyMiddleware>();
await app.RunAsync();
