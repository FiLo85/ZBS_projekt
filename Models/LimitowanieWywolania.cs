using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

public class LimitowanieWywolania
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();
    private static readonly TimeSpan RequestWindow = TimeSpan.FromSeconds(30);
    private const int MaxRequests = 10;

    public LimitowanieWywolania(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var endpoint = context.Request.Path.ToString().ToLower();

        var key = $"{ipAddress}:{endpoint}";
        var now = DateTime.UtcNow;

        _requestLog.TryGetValue(key, out var requestTimes);
        requestTimes ??= new List<DateTime>();
        requestTimes = requestTimes.Where(t => now - t < RequestWindow).ToList();

        if (requestTimes.Count >= MaxRequests)
        {
            var powtorzPo = RequestWindow - (now - requestTimes.First());

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Zbyt wiele żądań. Spróbuj ponownie za kilka sekund.",
                powtorzPoCzasie = (int)powtorzPo.TotalSeconds + "sek"
            }));
            return;
        }

        requestTimes.Add(now);
        _requestLog[key] = requestTimes;

        await _next(context);
    }
}
