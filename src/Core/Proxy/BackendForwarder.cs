namespace LoadBalancer.Core.Proxy;

public sealed class BackendForwarder : IBackendForwarder
{
    private readonly HttpClient _http;

    public BackendForwarder(HttpClient http) => _http = http;

    public async Task ForwardAsync(HttpContext context, Instance backend, CancellationToken ct)
    {
        var target = RewriteRequestUri(context.Request, backend);
        var proxyRequest = CreateProxyRequest(context.Request, target);
        using var response = await _http.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
            if (!HttpProxyHelper.IsHopByHopHeader(header.Key))
                context.Response.Headers[header.Key] = header.Value.ToArray();

        foreach (var header in response.Content.Headers)
            if (!HttpProxyHelper.IsHopByHopHeader(header.Key))
                context.Response.Headers[header.Key] = header.Value.ToArray();

        await response.Content.CopyToAsync(context.Response.Body, ct);
    }

    private static Uri RewriteRequestUri(HttpRequest original, Instance backend) =>
        new($"{backend.Address}{original.Path}{original.QueryString}");

    private static HttpRequestMessage CreateProxyRequest(HttpRequest original, Uri target)
    {
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(original.Method),
            RequestUri = target,
            Content = HttpProxyHelper.HasBody(original.Method)
                ? new StreamContent(original.Body)
                : null
        };

        foreach (var header in original.Headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

        return request;
    }
}