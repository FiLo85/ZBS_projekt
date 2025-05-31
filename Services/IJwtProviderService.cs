namespace WeatherChecker_FO.Services
{
    public interface IJwtProviderService
    {
        string GenerateToken(string email);
    }
}