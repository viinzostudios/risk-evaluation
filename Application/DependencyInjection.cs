using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Application.Behaviors;
using Application.Mappings;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Add ValidationBehavior to the pipeline
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation - Register all validators from this assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<PaymentProfile>();
        });

        return services;
    }
}
