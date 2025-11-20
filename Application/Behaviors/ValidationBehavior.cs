using FluentValidation;
using MediatR;
using Transational.Api.Domain.Common;

namespace Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = string.Join(", ", failures.Select(f => f.ErrorMessage));

            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];

                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, new[] { typeof(string) });

                if (failureMethod != null)
                {
                    var genericFailureMethod = failureMethod.MakeGenericMethod(resultType);
                    var result = genericFailureMethod.Invoke(null, new object[] { errors });
                    if (result != null)
                    {
                        return (TResponse)result;
                    }
                }
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
