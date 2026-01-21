using System.Text.RegularExpressions;
using CoreBank.Domain.Exceptions;

namespace CoreBank.Domain.ValueObjects;

public partial record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value.ToLowerInvariant();
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required");

        email = email.Trim();

        if (email.Length > 254)
            throw new DomainException("Email is too long");

        if (!EmailRegex().IsMatch(email))
            throw new DomainException("Invalid email format");

        return new Email(email);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
