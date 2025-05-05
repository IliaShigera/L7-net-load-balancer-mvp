namespace LoadBalancer.Core.Health;

public sealed class InstanceHealthChecker : IInstanceHealthChecker
{
    private readonly HttpClient _http;
    private readonly HealthCheckSettings _settings;
    
    public InstanceHealthChecker(HttpClient http, IOptions<HealthCheckSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<bool> IsAliveAsync(Instance instance, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            var response = await _http.GetAsync(
                new Uri($"{instance.Address}/{_settings.Path}"),
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}