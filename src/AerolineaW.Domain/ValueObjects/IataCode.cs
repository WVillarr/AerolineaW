namespace AerolineaW.Domain.ValueObjects;

/// <summary>A three-letter IATA airport code (e.g. "MIA", "JFK"). Always stored upper-case.</summary>
public sealed record IataCode
{
    public string Value { get; }

    public IataCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("IATA code cannot be empty.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException($"'{value}' is not a valid IATA code: it must be exactly 3 letters.", nameof(value));
        }

        Value = normalized;
    }

    public override string ToString() => Value;

    public static implicit operator string(IataCode code) => code.Value;
}
