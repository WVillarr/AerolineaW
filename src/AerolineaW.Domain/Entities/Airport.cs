using AerolineaW.Domain.ValueObjects;

namespace AerolineaW.Domain.Entities;

/// <summary>An airport that can serve as a flight's origin or destination.</summary>
public sealed class Airport
{
    public int Id { get; private set; }

    public IataCode Code { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    /// <summary>IANA time zone id (e.g. "America/New_York"), used to render local times to clients.</summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    private Airport()
    {
        // Reserved for EF Core materialization.
    }

    public Airport(IataCode code, string name, string city, string country, string timeZoneId)
    {
        Code = code;
        SetDetails(name, city, country, timeZoneId);
    }

    public void SetDetails(string name, string city, string country, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Airport name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City cannot be empty.", nameof(city));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country cannot be empty.", nameof(country));
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"'{timeZoneId}' is not a recognized time zone id.", nameof(timeZoneId));
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException($"'{timeZoneId}' is not a valid time zone id.", nameof(timeZoneId));
        }

        Name = name.Trim();
        City = city.Trim();
        Country = country.Trim();
        TimeZoneId = timeZoneId;
    }
}
