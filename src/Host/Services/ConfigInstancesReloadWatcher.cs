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
        try
        {
            var reloaded = _configuration
                .GetRequiredSection(ConfigSectionKeys.Instances)
                .Get<List<InstanceDefinition>>();

            if (reloaded is null)
            {
                _logger.LogError("Invalid instance definitions.");
                return;
            }

            if (!reloaded.Any())
                return;

            var newInstances = reloaded
                .Select(Instance.CreateFromDefinition)
                .ToList();

            _logger.LogInformation("Reloading instances");
            _instanceRegistry.ReplaceAll(newInstances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload instances: {Message}", ex.Message);
        }
    }

    public override void Dispose()
    {
        _watcher.Dispose();
        base.Dispose();
    }
}