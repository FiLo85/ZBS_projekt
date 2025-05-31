namespace WeatherChecker_FO.Services;

public interface ICodeSender
{
    Task SendAsync(string email, string code);
}

public class MockCodeSender : ICodeSender
{
    public Task SendAsync(string email, string code)
    {
        Console.WriteLine($"[DEBUG] Kod 2FA wysłany na {email}: {code}");
        return Task.CompletedTask;
    }
}

