using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.Helpers;

public static class DtoValidation
{
    public static void EnsureValid<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var results = new List<ValidationResult>();
        var context = new ValidationContext(value);
        if (Validator.TryValidateObject(value, context, results, validateAllProperties: true))
        {
            return;
        }

        var errors = results
            .Select(result => result.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message));

        throw new InvalidOperationException($"Please correct the form: {string.Join(" ", errors)}");
    }
}
