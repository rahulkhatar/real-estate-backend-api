using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RealEstate.Api.Middleware;
using Scalar.AspNetCore;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Interfaces;
using RealEstate.Infrastructure;
using RealEstate.Infrastructure.Persistence;
using Serilog;

// Entry point for the API host -- deployed to BigRock via the RealEstateAPI project's
// pipeline, triggered by pushes to this repo's main branch (see azure-pipelines.yml).
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ---- Services -------------------------------------------------------------

builder.Services.AddControllers();

// DTOs carry [Required]/[MaxLength] etc. purely as machine-readable documentation for the
// OpenAPI schema. Validation itself is owned entirely by FluentValidation (via the MediatR
// pipeline), so that every validation failure — nested DTOs included — goes through the same
// path and comes back in the same { message, errors } shape instead of ASP.NET Core's built-in
// ModelState filter racing it with a differently-shaped ProblemDetails response.
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddOpenApi();

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("Jwt:Secret is not configured. Set it via appsettings, user-secrets, or an environment variable before starting the API.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---- Startup: indexes + admin bootstrap ------------------------------------

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var mongoContext = services.GetRequiredService<IMongoDbContext>();
    await IndexInitializer.InitializeAsync(mongoContext);

    await AdminSeeder.SeedAsync(
        services.GetRequiredService<IAgentRepository>(),
        services.GetRequiredService<IPasswordHasher>(),
        app.Configuration,
        services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder"));
}

// ---- Middleware pipeline ----------------------------------------------------

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Real Estate API"));
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Unauthenticated liveness probe for the Docker HEALTHCHECK / container orchestrator.
app.MapGet("/health", () => Results.Ok("healthy"));

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
