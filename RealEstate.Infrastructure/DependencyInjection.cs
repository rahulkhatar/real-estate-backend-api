using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Interfaces;
using RealEstate.Infrastructure.ExternalServices;
using RealEstate.Infrastructure.ExternalServices.OpenAi;
using RealEstate.Infrastructure.ExternalServices.PaymentGateways;
using RealEstate.Infrastructure.Identity;
using RealEstate.Infrastructure.Persistence;
using RealEstate.Infrastructure.Persistence.Repositories;

namespace RealEstate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        BsonConventions.Register();

        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddSingleton<IMongoDbContext, MongoDbContext>();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IUnitLayoutRepository, UnitLayoutRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IListingEmbeddingRepository, ListingEmbeddingRepository>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<CloudflareR2Settings>(configuration.GetSection(CloudflareR2Settings.SectionName));
        services.AddScoped<IImageStorageService, CloudflareR2StorageService>();

        // Both gateways register as IPaymentGateway; command handlers pick the right one from
        // IEnumerable<IPaymentGateway> by Provider. IsConfigured is false (and CreateOrderAsync
        // throws PaymentGatewayNotConfiguredException) until real API keys are added.
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();

        services.Configure<RazorpaySettings>(configuration.GetSection(RazorpaySettings.SectionName));
        services.AddHttpClient<RazorpayPaymentGateway>(client =>
        {
            client.BaseAddress = new Uri("https://api.razorpay.com/v1/");
        });
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<RazorpayPaymentGateway>());

        // IsConfigured-style guard lives inline in each method (throws AiNotConfiguredException) since
        // there's no shared IsConfigured surface across embeddings vs. chat completion here.
        services.Configure<OpenAiSettings>(configuration.GetSection(OpenAiSettings.SectionName));
        services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
        });
        services.AddHttpClient<IChatCompletionService, OpenAiChatCompletionService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
        });

        return services;
    }
}
