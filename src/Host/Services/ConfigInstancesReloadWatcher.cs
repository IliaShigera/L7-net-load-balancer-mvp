namespace LoadBalancer.Host.Services;

internal sealed class ConfigInstancesReloadWatcher : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IInstanceRegistry _instanceRegistry;
    private readonly ILogger<ConfigInstancesReloadWatcher> _logger;
    private readonly FileSystemWatcher _watcher;

    public ConfigInstancesReloadWatcher(
        IConfiguration configuration,
        IInstanceRegistry instanceRegistry,
        ILogger<ConfigInstancesReloadWatcher> logger)
    {
        _configuration = configuration;
        _instanceRegistry = instanceRegistry;
        _logger = logger;

        _watcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory, ConfigFiles.AppSettings)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        _watcher.Changed += OnChanged_ReloadInstances;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    private void OnChanged_ReloadInstances(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Reloading instances");

        try
        {
            var reloaded = _configuration
                .GetRequiredSection(ConfigSectionKeys.Instances)
                .Get<List<InstanceDefinition>>();

            if (reloaded is null or [])
            {
                _logger.LogWarning("Instance definitions missing or invalid in config");
                return;
            }

            var newInstances = reloaded
                .Select(Instance.CreateFromDefinition)
                .ToList();

            _instanceRegistry.ReplaceAll(newInstances);
            _logger.LogInformation("Reloaded {Count} instances from config", newInstances.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload instances fron config");
        }
    }

    public override void Dispose()
    {
        _watcher.Dispose();
        base.Dispose();
    }
}