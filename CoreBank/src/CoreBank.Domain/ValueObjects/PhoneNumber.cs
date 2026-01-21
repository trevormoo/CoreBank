using System.Text.RegularExpressions;
using CoreBank.Domain.Exceptions;

namespace CoreBank.Domain.ValueObjects;

public partial record PhoneNumber
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Phone number is required");

        // Remove all non-digit characters except + at the beginning
        var cleaned = CleanPhoneRegex().Replace(phoneNumber.Trim(), "");

        if (phoneNumber.StartsWith('+'))
            cleaned = "+" + cleaned;

        if (cleaned.Length < 10 || cleaned.Length > 15)
            throw new DomainException("Phone number must be between 10 and 15 digits");

        return new PhoneNumber(cleaned);
    }

    [GeneratedRegex(@"[^\d]", RegexOptions.Compiled)]
    private static partial Regex CleanPhoneRegex();

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}
