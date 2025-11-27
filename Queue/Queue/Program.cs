using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Queue;
using Queue.Repositories;
using Queue.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var secretKey = "super_super_secret_key_with_256_bits_!!!"; // 256+ бит
var issuer = "MyApp";
var audience = "MyUsers";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });

    c.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Name = "access_token",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "JWT stored in cookie"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "cookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });

});
// ✅ Разрешаем CORS для фронтенда на localhost:8080
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:8080", "http://localhost:5173") // адрес твоего Vue
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // разрешаем куки (JWT в cookies)
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = false,
            //ClockSkew = TimeSpan.Zero
        };

        // Берём токен из куки
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // Прерываем редирект
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"error\":\"Forbidden\"}");
            }
        };
    });
builder.Services.AddAuthorization();

var configuration = builder.Configuration.GetConnectionString("ApplicationDbContext");
builder.Services.AddDbContext<ApplicationDbContext>(context => context.UseNpgsql(configuration));
builder.Services.AddTransient<UserRepository>();
builder.Services.AddTransient<DatesRepository>();
builder.Services.AddTransient<GroupRepository>();
builder.Services.AddTransient<QueueRepository>();
builder.Services.AddTransient<SubjectRepository>();
builder.Services.AddTransient<ScheduleRepository>();
builder.Services.AddTransient<EmailService>();


builder.Services.AddHostedService<GroupSyncService>();

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseRouting();

app.Use(async (context, next) =>
{
    var handler = new JwtSecurityTokenHandler();
    var accessToken = context.Request.Cookies["access_token"];
    var refreshToken = context.Request.Cookies["refresh_token"];

    ClaimsPrincipal? principal = null;

    if (!string.IsNullOrEmpty(accessToken))
    {
        try
        {
            principal = handler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch (SecurityTokenExpiredException)
        {
            // Токен просрочен — попробуем обновить
        }
        catch
        {
            // access_token невалиден — удалим
            context.Response.Cookies.Delete("access_token");
        }
    }

    if (principal == null && !string.IsNullOrEmpty(refreshToken))
    {
        try
        {
            var refreshPrincipal = handler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var username = refreshPrincipal.Identity?.Name;
            var idClaim = refreshPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            var userId = idClaim != null ? int.Parse(idClaim.Value) : (int?)null;
            var newAccessToken = GenerateToken(username, userId.Value, 1);

            context.Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(1)
            });

            // Добавим в заголовок, чтобы JwtBearer его увидел
            context.Request.Headers["Authorization"] = $"Bearer {newAccessToken}";
        }
        catch
        {
            context.Response.Cookies.Delete("refresh_token");
        }
    }

    // Устанавливаем principal в контексте, если он валиден
    if (principal != null)
    {
        context.User = principal;
    }

    await next();
});
string GenerateToken(string username, int id, int minutes)
{
    var claims = new[]
    {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, id.ToString())
            };

    var token = new JwtSecurityToken(
        issuer,
        audience,
        claims,
        expires: DateTime.UtcNow.AddMinutes(minutes),
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.UseCors("AllowVueApp"); // <-- добавить вот эту строку
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
// Configure the HTTP request pipeline.



app.MapControllers();

app.Run();


// dotnet ef migrations add InitialCreate --project "Queue" --startup-project "Queue"
// dotnet ef database update --project "Курсач_1" --startup-project "Курсач_1"
// dotnet ef migrations add UpdateEventTable
// dotnet ef database update