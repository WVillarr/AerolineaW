namespace AerolineaW.Domain.Common;

/// <summary>
/// Base type for every business-rule violation raised by the domain model — as opposed to
/// <see cref="ArgumentException"/>, which the entities use for malformed input (empty names,
/// negative weights, and the like). The API layer maps this hierarchy to HTTP 409 Conflict;
/// a <see cref="DomainException"/> is an expected, named outcome and must never surface as a
/// raw 500.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}
