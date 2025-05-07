using Labels = LoadBalancer.Host.Prometheus.MetricLabels;

namespace LoadBalancer.Host.Prometheus;

internal static class MetricsRegistry
{
    internal static readonly Counter ProxyRequests = Metrics.CreateCounter(
        "proxy_request_total",
        "Total of proxy requests.",
        new CounterConfiguration { LabelNames = [Labels.Instance, Labels.StatusCode] });

    public static readonly Counter ProxyErrors = Metrics.CreateCounter(
        "proxy_errors_total",
        "Total of proxy errors by instance",
        new CounterConfiguration { LabelNames = [Labels.Instance, Labels.ErrorType] });

    public static readonly Gauge InstanceHealth = Metrics.CreateGauge(
        "instance_health_status",
        "Health status of each backend (1=healthy, 0=unhealthy)",
        new GaugeConfiguration { LabelNames = [Labels.Instance] });
}