using TheSocialMediaV2.API.Middlewares;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using TheSocialMediaV2.Domain.Events;
using TheSocialMediaV2.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 0. Serilog Loglama Sistemini Baþlat
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.

// 1. Veritabaný Baðlantýsý (Dependency Injection)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
// Servis Kayýtlarý
builder.Services.AddScoped<TheSocialMediaV2.API.Services.IAdminActionLogger, TheSocialMediaV2.API.Services.AdminActionLogger>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// --- EVENT LAYER ---
builder.Services.AddScoped<IInternalDomainEventDispatcher, DomainEventDispatcher>();

// Handler'larý tek tek kaydediyoruz
builder.Services.AddScoped<IInternalDomainEventHandler<UserBannedEvent>, UserBannedEventHandler>();

builder.Services.AddHostedService<TheSocialMediaV2.API.Services.OutboxProcessorBackgroundService>();

// Swagger Ayarlarý (JWT Desteði ile)
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "TheSocialMediaV2 API", Version = "v1" });

    // Kilit Ekranýný (Authorize) Aktif Etme
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Lütfen token'ý 'Bearer [boþluk] TOKEN' formatýnda giriniz.",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

// 2. JWT Authentication Ayarlarý
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

// MediatR Kaydý
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TheSocialMediaV2.Application.Matches.Commands.AcceptMatchCommand).Assembly));

// Repository Kaydý
builder.Services.AddScoped<TheSocialMediaV2.Application.Abstractions.Repositories.IMatchRepository, TheSocialMediaV2.API.Data.MatchRepository>();

// Zaman Servisi Kaydý
builder.Services.AddSingleton<TheSocialMediaV2.Application.Abstractions.Services.IClock, TheSocialMediaV2.API.Services.SystemClock>();

var app = builder.Build();

// Özel Hata Yakalayýcý Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<TheSocialMediaV2.API.Middlewares.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<TheSocialMediaV2.API.Middlewares.BanCheckMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }