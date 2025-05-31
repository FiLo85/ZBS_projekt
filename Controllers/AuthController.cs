using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WeatherChecker_FO.Models;
using WeatherChecker_FO.Services;
using Microsoft.Extensions.Caching.Memory;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IJwtProviderService _jwtProviderService;
    private readonly IMemoryCache _cache;
    //private readonly ICodeSender _codeSender;

    public AuthController(IAccountService accountService, IJwtProviderService jwtProviderService, IMemoryCache cache)
    {
        _accountService = accountService;
        _jwtProviderService = jwtProviderService;
        _cache = cache;
    }



    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _accountService.EmailExistsAsync(request.Email))
        {
            return BadRequest(new { message = "Użytkownik z takim emailem już istnieje." });
        }

        await _accountService.RegisterUserAsync(request.Email, request.Password);

        return Ok(new { message = "Zarejestrowano" });
    }



    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Sprawdź, czy IP jest zablokowane
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (IsIpBlocked(ip))
        {
            return Unauthorized(new { message = "Twoje IP jest zablokowane na godzinę z powodu zbyt wielu nieudanych prób logowania." });
        }

        var account = await _accountService.GetByEmailAsync(request.Email);
        if (account == null)
        {
            return Unauthorized(new { message = "Nieprawidłowy email lub hasło." });
        }

        if (account.BlokadaDo != null && account.BlokadaDo > DateTime.UtcNow)
        {
            var remaining = (account.BlokadaDo.Value - DateTime.UtcNow).TotalMinutes;
            return Unauthorized(new { message = $"Konto zablokowane. Spróbuj ponownie za {Math.Ceiling(remaining)} minut." });
        }

        if (!_accountService.VerifyPassword(account, request.Password))
        {
            // Rejestruj nieudane próby logowania i blokuj IP
            RegisterFailedAttempt(ip);
            account.NieudaneProbyLogowania++;

            if (account.NieudaneProbyLogowania >= 5)
            {
                account.BlokadaDo = DateTime.UtcNow.AddMinutes(10);
                account.NieudaneProbyLogowania = 0;
            }

            await _accountService.UpdateAccountAsync(account);

            return Unauthorized(new { message = "Nieprawidłowy email lub hasło." });
        }

        account.NieudaneProbyLogowania = 0;
        account.BlokadaDo = null;
        await _accountService.UpdateAccountAsync(account);

        var code = await _accountService.GenerateTwoFactorCodeAsync(account);
        Console.WriteLine($"Kod 2FA dla {account.Email}: {code}");

        return Ok(new
        {
            message = "Hasło poprawne. Kod 2FA został wygenerowany.", 
        });
    }



    [HttpPost("verify-login-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] Login2faRequest request)
    {
        
        var account = await _accountService.GetByEmailAsync(request.Email);
        if (account == null || !_accountService.VerifyTwoFactorCode(account, request.Kod))
        {
            return Unauthorized(new { message = "Nieprawidłowy email lub kod." });
        }

        var token = _jwtProviderService.GenerateToken(account.Email);
        return Ok(new { token });
    }


    private bool IsIpBlocked(string ip)
    {
        if (_cache.TryGetValue($"blocked_{ip}", out bool blocked) && blocked)
        {
            return true;
        }
        return false;
    }

    private void RegisterFailedAttempt(string ip)
    {
        var todayKey = $"failed_{ip}_{DateTime.UtcNow.Date:yyyyMMdd}";
        int failCount = _cache.GetOrCreate(todayKey, entry =>
        {
            entry.AbsoluteExpiration = DateTime.UtcNow.Date.AddDays(1);
            return 0;
        });

        failCount++;
        _cache.Set(todayKey, failCount);

        if (failCount >= 100)
        {
            _cache.Set($"blocked_{ip}", true, TimeSpan.FromHours(1));
        }
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
