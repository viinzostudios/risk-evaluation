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
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<PaymentProfile>();
        });

        return services;
    }
}
