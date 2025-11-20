using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Transational.Api.Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using kcs = KafkaClient.Service.Implementations;
using KafkaClient.Service.Interfaces;
using KafkaClient.Service;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration), "DefaultConnection is required");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.Configure<KafkaSettings>(configuration.GetSection("KafkaSettings"));
        services.AddSingleton<IKafkaClient, kcs.KafkaClient>();
        return services;
    }
}
