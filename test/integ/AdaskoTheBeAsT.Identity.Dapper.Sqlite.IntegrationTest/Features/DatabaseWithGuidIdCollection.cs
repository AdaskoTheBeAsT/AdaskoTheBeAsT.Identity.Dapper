using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Features;

// These features share one SQLite database. Reqnroll scenario tags only generate traits,
// so attach the xUnit collection to the feature classes to serialize their database access.
[Collection("DatabaseWithGuidIdCollection")]
public partial class WithoutNormalizedAspNetIdentityGuidRoleStoreFeature
{
}

[Collection("DatabaseWithGuidIdCollection")]
public partial class WithoutNormalizedAspNetIdentityGuidUserOnlyStoreFeature
{
}

[Collection("DatabaseWithGuidIdCollection")]
public partial class WithoutNormalizedAspNetIdentityGuidUserStoreFeature
{
}
