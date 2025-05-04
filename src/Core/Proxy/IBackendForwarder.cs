namespace LoadBalancer.Core.Proxy;

public interface IBackendForwarder
{
    Task ForwardAsync(HttpContext context, Instance backend, CancellationToken ct);
}