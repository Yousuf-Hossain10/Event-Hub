namespace EventHub.IntegrationTests;

// All integration test classes share one physical test database, so they must run sequentially
// relative to each other (xUnit parallelizes across classes by default) and share one migrated
// factory instance rather than each re-migrating/competing over the same rows.
[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
