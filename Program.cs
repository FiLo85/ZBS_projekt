using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeatherChecker_FO.Data;
using WeatherChecker_FO.Models;
using WeatherChecker_FO.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);




var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddScoped<IJwtProviderService, JwtProviderService>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter the JWT with Bearer",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WeatherCheckerDB"));

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IAccountService, AccountService>();

builder.Logging.AddConsole();

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    if (!db.Accounts.Any())
//    {
//        db.Accounts.Add(new Account
//        {
//            Email = "test@test.com",
//            PasswordHash = "startowe" 
//        });
//        db.SaveChanges();
//    }
//}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<RequestLogMiddleware>();
app.UseMiddleware<SqlInjMiddleware>();
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<LimitowanieWywolania>();
app.Run();
