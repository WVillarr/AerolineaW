using System.Text.RegularExpressions;

namespace AerolineaW.Domain.ValueObjects;

/// <summary>A seat number such as "14A": a row number followed by a single seat letter.</summary>
public sealed partial record SeatNumber
{
    public string Value { get; }

    public SeatNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Seat number cannot be empty.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!SeatPattern().IsMatch(normalized))
        {
            throw new ArgumentException($"'{value}' is not a valid seat number (expected e.g. '14A').", nameof(value));
        }

        Value = normalized;
    }

    public override string ToString() => Value;

    public static implicit operator string(SeatNumber seat) => seat.Value;

    [GeneratedRegex("^[1-9][0-9]{0,2}[A-Z]$")]
    private static partial Regex SeatPattern();
}
