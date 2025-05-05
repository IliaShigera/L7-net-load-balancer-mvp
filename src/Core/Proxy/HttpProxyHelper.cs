namespace LoadBalancer.Core.Proxy;

public static class HttpProxyHelper
{
    private static readonly HashSet<string> HopByHopHeaders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Transfer-Encoding"
        };

    public static bool IsHopByHopHeader(string name) => HopByHopHeaders.Contains(name);

    public static bool HasBody(string method) =>
        method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase);
}