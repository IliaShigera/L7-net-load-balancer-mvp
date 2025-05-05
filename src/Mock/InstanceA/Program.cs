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

app.MapGet("/", () => "Hello from Instance A");
app.MapGet("/healthz", () => "UP");
app.Run();