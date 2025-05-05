var builder = WebApplication.CreateBuilder(args);

var lbIP = builder.Configuration["LoadBalancerIP"];
if (!IPAddress.TryParse(lbIP, out var allowedIp))
    throw new ArgumentException("Invalid or missing LoadBalancerIP in configuration.");

var app = builder.Build();
app.Use(async (context, next) =>
{
    var allowed = lbIP;
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();

    if (remoteIp != allowed)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Access denied");
        return;
    }

    await next();
});


app.MapGet("/healthz", () => Results.Ok("UP"));

app.MapMethods("/{*path}", ["GET", "POST", "PUT", "DELETE", "PATCH"], async (HttpContext ctx) =>
{
    if (ctx.Request.Path.HasValue && ctx.Request.Path.Value.EndsWith("/test"))
    {
        ctx.Request.EnableBuffering();
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        ctx.Request.Body.Position = 0;

        var info = new
        {
            Method = ctx.Request.Method,
            Path = ctx.Request.Path,
            Query = ctx.Request.QueryString,
            RemoteIp = ctx.Connection.RemoteIpAddress?.ToString(),
            Headers = ctx.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = body
        };

        return Results.Ok(info);
    }

    return Results.Ok("Hello from Instance A");
});

app.Run();