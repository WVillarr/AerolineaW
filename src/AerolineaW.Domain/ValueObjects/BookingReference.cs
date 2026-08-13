using System.Text.RegularExpressions;

namespace AerolineaW.Domain.ValueObjects;

/// <summary>
/// A 6-character PNR-style booking reference, e.g. "3F9K2X". Excludes the visually ambiguous
/// characters 0/O and 1/I, the same convention real airline reservation systems use.
/// Uniqueness is enforced by a database index, not by this type.
/// </summary>
public sealed partial record BookingReference
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Value { get; }

    public BookingReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Booking reference cannot be empty.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!ReferencePattern().IsMatch(normalized))
        {
            throw new ArgumentException($"'{value}' is not a valid booking reference.", nameof(value));
        }

        Value = normalized;
    }

    public static BookingReference Generate()
    {
        Span<char> buffer = stackalloc char[6];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new BookingReference(new string(buffer));
    }

    public override string ToString() => Value;

    public static implicit operator string(BookingReference reference) => reference.Value;

    [GeneratedRegex("^[A-Z0-9]{6}$")]
    private static partial Regex ReferencePattern();
}
