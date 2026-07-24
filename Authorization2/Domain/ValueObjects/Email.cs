using System.Text.RegularExpressions;
using Authorization2.Domain.ValueObjects;

namespace Authorization2.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; private set; }

    private Email(string email)
    {
        Value = email;
    }

    public static Result<Email> Create(string email)
    {
        // Проверка на null или пустую строку
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure(new Error("Validation", "Email is required"));

        // Trim лишних пробелов
        email = email.Trim();

        // Проверка длины (максимальная длина email - 254 символа)
        if (email.Length > 254)
            return Result<Email>.Failure(new Error("Validation","Email is too long (max 254 characters)"));

        // Проверка формата через регулярное выражение
        if (!IsValidEmailFormat(email))
            return Result<Email>.Failure(new Error("Validation","Invalid email format"));

        // Проверка длины локальной части (до @) - макс 64 символа
        var localPart = email.Split('@')[0];
        if (localPart.Length > 64)
            return Result<Email>.Failure(new Error("Validation","Local part of email is too long (max 64 characters)"));

        // Приводим к нижнему регистру (каноническая форма)
        email = email.ToLowerInvariant();

        return Result<Email>.Success(new Email(email));
    }

    private static bool IsValidEmailFormat(string email)
    {
        // Самый надежный способ валидации email
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    // Дополнительные методы для работы с Email
    public string GetLocalPart()
    {
        return Value.Split('@')[0];
    }

    public string GetDomain()
    {
        return Value.Split('@')[1];
    }

    public bool IsFromDomain(string domain)
    {
        return GetDomain().Equals(domain, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}