namespace LoadBalancer.Core.Proxy;

public sealed class HttpReverseProxy : IHttpReverseProxy
{
    private readonly ILogger<HttpReverseProxy> _logger;
    private readonly HttpClient _http;

    public HttpReverseProxy(ILogger<HttpReverseProxy> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task ProxyAsync(HttpContext context, Instance instance, CancellationToken ct)
    {
        try
        {
            var target = RewriteRequestUri(context.Request, instance);
            var proxyRequest = CreateProxyRequest(context.Request, target);
            var response = await _http.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in response.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            await response.Content.CopyToAsync(context.Response.Body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proxy error: {InstanceName} [{InstanceAddress}]",
                instance.Name,
                instance.Address);

            context.Response.StatusCode = StatusCodes.Status502BadGateway;
        }
    }

    private static Uri RewriteRequestUri(HttpRequest original, Instance backendServer)
    {
        return new Uri($"{backendServer.Address}{original.Path}{original.QueryString}");
    }

    private static HttpRequestMessage CreateProxyRequest(HttpRequest original, Uri target)
    {
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(original.Method),
            RequestUri = target,
            Content = new StreamContent(original.Body)
        };

        foreach (var header in original.Headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

        return request;
    }
}