using System.Text;
using CoreBank.Application.Common.Interfaces;
using CoreBank.Infrastructure.Persistence;
using CoreBank.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CoreBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, b =>
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Services
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITransactionLimitService, TransactionLimitService>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        services.AddScoped<IScheduledPaymentService, ScheduledPaymentService>();
        services.AddScoped<IStatementService, StatementService>();
        services.AddScoped<IAuditService, AuditService>();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

        services.AddAuthentication(options =>
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
                ValidIssuer = jwtSettings["Issuer"] ?? "CoreBank",
                ValidAudience = jwtSettings["Audience"] ?? "CoreBankUsers",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin", "SuperAdmin"));

            options.AddPolicy("RequireSuperAdmin", policy =>
                policy.RequireRole("SuperAdmin"));

            options.AddPolicy("RequireKycApproved", policy =>
                policy.RequireClaim("kyc_status", "Approved"));
        });

        return services;
    }
}
