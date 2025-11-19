using Microsoft.Extensions.DependencyInjection;
using Transational.Api.Application.Mappings;

namespace Transational.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<PaymentProfile>();
        });

        return services;
    }
}
