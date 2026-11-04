using Xunit;

namespace AerolineaW.Api.IntegrationTests;

/// <summary>
/// Shares one <see cref="ApiWebApplicationFactory"/> and its disposable SQL Server database across
/// every test class in this assembly, instead of paying a fresh container startup per class.
/// </summary>
[CollectionDefinition(nameof(SharedApiFixture))]
public sealed class SharedApiFixture : ICollectionFixture<ApiWebApplicationFactory>;
