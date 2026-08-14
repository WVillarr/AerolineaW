namespace AerolineaW.Application.Common;

/// <summary>
/// Raised when a lookup by id finds nothing. The API layer maps this to HTTP 404 — like
/// <see cref="AerolineaW.Domain.Common.DomainException"/>, it's an expected, named outcome
/// and must never surface as a raw 500.
/// </summary>
public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.")
{
}
