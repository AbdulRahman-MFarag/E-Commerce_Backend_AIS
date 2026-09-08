using System.Text;
using Asp.Versioning;
using FluentValidation;
using Identity.Api.Infrastructure.Authentication;
using Identity.Api.Infrastructure.Persistence;
using Identity.Api.Middleware;
using Identity.Api.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Serilog
// ========================================

builder.Host.UseSerilog();

// ========================================
// Database
// ========================================

var connectionString =
    builder.Configuration
        .GetConnectionString("IdentityDatabase");

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("IdentityDatabase");

    options.UseSqlServer(connectionString);
});

// ========================================
// JWT Authentication
// ========================================

var jwtSettings =
    builder.Configuration.GetSection("Jwt");

var jwtKey =
    jwtSettings["Key"]
    ?? throw new InvalidOperationException(
        "JWT Key is not configured.");

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings["Issuer"],

                ValidAudience =
                    jwtSettings["Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew =
                    TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

// ========================================
// Services
// ========================================

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    IJwtService,
    JwtService>();

// ========================================
// FluentValidation
// ========================================

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>();

// ========================================
// API Versioning
// ========================================

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion =
            new ApiVersion(1, 0);

        options.AssumeDefaultVersionWhenUnspecified =
            true;

        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat =
            "'v'VVV";

        options.SubstituteApiVersionInUrl = true;
    });

// ========================================
// Controllers
// ========================================

builder.Services.AddControllers();

// ========================================
// Swagger
// ========================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ========================================
// Global Exception Handling
// ========================================

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

var app = builder.Build();

// ========================================
// Database Migration + Seed
// ========================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<IdentityDbContext>();

    await context.Database.MigrateAsync();

    await IdentityDbSeeder.SeedAsync(context);
}

// ========================================
// Exception Handler
// ========================================

app.UseExceptionHandler();

// ========================================
// Swagger
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// ========================================
// Middleware
// ========================================

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// ========================================
// Controllers
// ========================================

app.MapControllers();

app.Run();