namespace Transational.Api.Domain.Common;

public static class Ensure
{
    public static void NotNull<T>(T value, string parameterName) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
        }
    }

    public static void NotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);
        }
    }

    public static void NotNullOrEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        }
    }

    public static void GreaterThanZero(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{parameterName} must be greater than zero", parameterName);
        }
    }

    public static void GreaterThanZero(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{parameterName} must be greater than zero", parameterName);
        }
    }

    public static void GreaterThan(decimal value, decimal minimum, string parameterName)
    {
        if (value <= minimum)
        {
            throw new ArgumentException($"{parameterName} must be greater than {minimum}", parameterName);
        }
    }

    public static void InRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be between {min} and {max}");
        }
    }

    public static void LessThanOrEqual(decimal value, decimal maximum, string parameterName)
    {
        if (value > maximum)
        {
            throw new ArgumentException($"{parameterName} must be less than or equal to {maximum}", parameterName);
        }
    }
}
