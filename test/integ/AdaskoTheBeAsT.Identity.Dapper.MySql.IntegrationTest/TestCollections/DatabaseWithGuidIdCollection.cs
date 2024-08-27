using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;

[CollectionDefinition(nameof(DatabaseWithGuidIdCollection))]
public class DatabaseWithGuidIdCollection
    : ICollectionFixture<DatabaseWithGuidIdFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    // READ MORE: https://xunit.net/docs/shared-context
}
