using AerolineaW.Domain.Enums;

namespace AerolineaW.Domain.Entities;

/// <summary>A person who can hold bookings.</summary>
public sealed class Passenger
{
    public int Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public DocumentType DocumentType { get; private set; }

    public string DocumentNumber { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    private Passenger()
    {
        // Reserved for EF Core materialization.
    }

    public Passenger(string fullName, DocumentType documentType, string documentNumber, string country, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name cannot be empty.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new ArgumentException("Document number cannot be empty.", nameof(documentNumber));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country cannot be empty.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone cannot be empty.", nameof(phone));
        }

        FullName = fullName.Trim();
        DocumentType = documentType;
        DocumentNumber = documentNumber.Trim();
        Country = country.Trim();
        Phone = phone.Trim();
    }

    public void UpdateContactDetails(string country, string phone)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country cannot be empty.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone cannot be empty.", nameof(phone));
        }

        Country = country.Trim();
        Phone = phone.Trim();
    }
}
