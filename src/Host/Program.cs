var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile(ConfigFiles.AppSettings, optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.WebHost
    .ConfigureKestrel(server => builder.Configuration.GetSection(ConfigSectionKeys.Kestrel).Bind(server))
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog();
    });

builder.Services
    .AddOptions<HealthCheckSettings>()
    .Bind(builder.Configuration.GetRequiredSection(ConfigSectionKeys.HealthCheck))
    .Validate(opts => opts is { IntervalSeconds: > 0, TimeoutSeconds: > 0 }, "Invalid health check options")
    .ValidateOnStart();

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<IInstanceRegistry, InstanceRegistry>();
builder.Services.AddSingleton<IInstanceHealthChecker, InstanceHealthChecker>();
builder.Services.AddSingleton<ILoadBalancingPolicy, RoundRobinPolicy>();
builder.Services.AddSingleton<IBackendForwarder, BackendForwarder>();

builder.Services.AddHostedService<HealthCheckService>();
builder.Services.AddHostedService<ConfigInstancesReloadWatcher>();

await using var app = builder.Build();
app.UseSerilogRequestLogging();

var adminGroup = app.MapGroup("admin");
adminGroup.MapGet("/healthz", () => Results.Ok("UP"));
adminGroup.MapGet("/status", (IInstanceRegistry registry) =>
{
    var instances = registry.ListAll();
    return Results.Ok(instances);
});

adminGroup.MapPost("/drain", (
    [FromQuery(Name = "name")] string instanceName,
    IInstanceRegistry registry,
    ILogger<Program> logger) =>
{
    var instance = registry.FindByName(instanceName);
    if (instance is null)
        return Results.BadRequest("Instance not found");

    logger.LogInformation("Draining instance {Name} {Address}", instance.Name, instance.Address);
    instance.Drain();

    return Results.Ok();
});

adminGroup.MapPost("/recover", (
    [FromQuery(Name = "name")] string instanceName,
    IInstanceRegistry registry, ILogger<Program> logger) =>
{
    var instance = registry.FindByName(instanceName);
    if (instance is null)
        return Results.BadRequest("Instance not found");

    logger.LogInformation("Recovering instance {Name} {Address}", instance.Name, instance.Address);
    instance.Recover();

    return Results.Ok();
});

adminGroup.MapPost("/reload", (IConfiguration configuration, IInstanceRegistry registry, ILogger<Program> logger) =>
{
    var reloaded = configuration
        .GetRequiredSection(ConfigSectionKeys.Instances)
        .Get<List<InstanceDefinition>>();

    if (reloaded is null or [])
    {
        logger.LogWarning("Instance definitions missing or invalid in config");
        return Results.BadRequest("Invalid instance definitions");
    }

    var newInstances = reloaded
        .Select(Instance.CreateFromDefinition)
        .ToList();

    logger.LogInformation("Reloading {Count} instances from config", newInstances.Count);
    registry.ReplaceAll(newInstances);

    return Results.Ok();
});

adminGroup.MapPost("/add", (
    [FromBody] InstanceDefinition definition,
    IInstanceRegistry registry,
    ILogger<Program> logger) =>
{
    if (definition is { Name: [], Address: []})
        return Results.BadRequest("Invalid instance definition");

    var instance = Instance.CreateFromDefinition(definition);
    logger.LogInformation("Adding instance {Name} {Address}", instance.Name, instance.Address);
    registry.Add(instance);

    var instances = registry.ListAll();
    var defs = instances.Select(i => new InstanceDefinition
    {
        Name = i.Name,
        Address = i.Address
    });

    return Results.Ok(defs);
});

adminGroup.MapDelete("/remove", (
    [FromQuery(Name = "name")] string instanceName,
    IInstanceRegistry registry,
    ILogger<Program> logger) =>
{
    var instance = registry.FindByName(instanceName);
    if (instance is null)
        return Results.BadRequest("Instance not found");

    logger.LogInformation("Removing instance {Name} {Address}", instance.Name, instance.Address);
    registry.Remove(instance.Name);

    return Results.Ok();
});

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/admin"),
    subApp => subApp.UseMiddleware<ProxyMiddleware>());

await app.RunAsync();