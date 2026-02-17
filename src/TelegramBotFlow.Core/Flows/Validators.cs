using System.Text.RegularExpressions;

namespace TelegramBotFlow.Core.Flows;

public static partial class Validators
{
    public static Func<string, string?> NotEmpty => input =>
        string.IsNullOrWhiteSpace(input) ? "Значение не может быть пустым." : null;

    public static Func<string, string?> Email => input =>
        EmailRegex().IsMatch(input) ? null : "Введите корректный email.";

    public static Func<string, string?> Integer => input =>
        int.TryParse(input, out _) ? null : "Введите целое число.";

    public static Func<string, string?> Phone => input =>
        PhoneRegex().IsMatch(input) ? null : "Введите корректный номер телефона.";

    public static Func<string, string?> MaxLength(int max) => input =>
        input.Length <= max ? null : $"Максимальная длина: {max} символов.";

    public static Func<string, string?> MinLength(int min) => input =>
        input.Length >= min ? null : $"Минимальная длина: {min} символов.";

    public static Func<string, string?> Combine(params Func<string, string?>[] validators) => input =>
    {
        foreach (var validator in validators)
        {
            var error = validator(input);
            if (error is not null)
                return error;
        }

        return null;
    };

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?\d{10,15}$", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();
}
