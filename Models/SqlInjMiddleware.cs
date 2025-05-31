using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class SqlInjMiddleware
{
    private readonly RequestDelegate _next;

    // Prosty regex wykrywający typowe wzorce SQL Injection
    private static readonly Regex _sqlInjectionPattern = new Regex(
        @"(\b(select|insert|update|delete|drop|alter|exec|union|--|;|')\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public SqlInjMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Sprawdź query string
        foreach (var value in request.Query.SelectMany(q => q.Value))
        {
            if (_sqlInjectionPattern.IsMatch(value))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Potencjalny atak SQL Injection (QueryString)");
                return;
            }
        }

        if ((request.Method == HttpMethods.Post || request.Method == HttpMethods.Put)
            && request.ContentType != null && request.ContentType.Contains("application/json"))
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            if (_sqlInjectionPattern.IsMatch(body))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Potencjalny atak SQL Injection (Body)");
                return;
            }
        }

        await _next(context);
    }
}
