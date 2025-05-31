using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

public class RequestLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLogMiddleware> _logger;
    private readonly string _logFilePath;

    public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "request-log.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var time = DateTime.UtcNow.ToString("o");

        string body = "";
        if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        //string email = context.User?.Identity?.IsAuthenticated == true
        //    ? context.User.Identity.E
        //    : "Nieznany";
        string email = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "Nieznany";


        string logEntry = $"[{time}] Metoda={method}, IP={ip}, Email={email}, Body={body}{Environment.NewLine}";

        _logger.LogInformation(logEntry);

        await File.AppendAllTextAsync(_logFilePath, logEntry);

        await _next(context);
    }
}
