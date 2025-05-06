var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile(ConfigFilePaths.AppSettings, optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.WebHost
    .ConfigureKestrel(server => builder.Configuration.GetSection(ConfigSectionKeys.Kestrel).Bind(server))
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.AddConfiguration(builder.Configuration.GetSection(ConfigSectionKeys.Logging));
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

await using var app = builder.Build();

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
        return Results.BadRequest("Instance not found.");

    logger.LogInformation("Draining instance {Name}", instance.Name);
    instance.Drain();

    return Results.Ok();
});

adminGroup.MapPost("/recover", (
    [FromQuery(Name = "name")] string instanceName,
    IInstanceRegistry registry, ILogger<Program> logger) =>
{
    var instance = registry.FindByName(instanceName);
    if (instance is null)
        return Results.BadRequest("Instance not found.");

    logger.LogInformation("Recovering instance {Name}", instance.Name);
    instance.Recover();

    return Results.Ok();
});

adminGroup.MapPost("/reload", (IConfiguration configuration, IInstanceRegistry registry, ILogger<Program> logger) =>
{
    var reloaded = configuration
        .GetRequiredSection(ConfigSectionKeys.Instances)
        .Get<List<InstanceDefinition>>();

    if (reloaded is null)
        return Results.BadRequest("Invalid instance definitions.");

    var newInstances = reloaded
        .Select(Instance.CreateFromDefinition)
        .ToList();

    logger.LogInformation("Reloading instances");
    registry.ReplaceAll(newInstances);

    return Results.Ok();
});

adminGroup.MapPost("/add", (
    [FromBody] InstanceDefinition iDef,
    IInstanceRegistry registry,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(iDef.Name) || string.IsNullOrWhiteSpace(iDef.Address))
        return Results.BadRequest("Invalid instance definition.");

    var instance = Instance.CreateFromDefinition(iDef);
    logger.LogInformation("adding instance {Name}", instance.Name);
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
        return Results.BadRequest("Instance not found.");

    logger.LogInformation("Removing instance {Name}", instance.Name);
    registry.Remove(instance.Name);

    return Results.Ok();
});

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/admin"),
    subApp => subApp.UseMiddleware<ProxyMiddleware>());

await app.RunAsync();