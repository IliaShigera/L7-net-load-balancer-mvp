namespace LoadBalancer.Core.Proxy;

public interface IHttpReverseProxy
{
    Task ProxyAsync(HttpContext context, Instance backend, CancellationToken ct);
}