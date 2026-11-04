namespace AerolineaW.Application.Common;

/// <summary>An expected unique-key or foreign-key conflict, with a client-safe message.</summary>
public sealed class PersistenceConflictException(Exception innerException)
    : Exception("The operation conflicts with an existing record or a referenced record.", innerException);
