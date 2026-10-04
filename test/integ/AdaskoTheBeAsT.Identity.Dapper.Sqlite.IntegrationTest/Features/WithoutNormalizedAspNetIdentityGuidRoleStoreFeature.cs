using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Features;

// Reqnroll tags only generate traits; attach the collection to serialize SQLite access.
[Collection("DatabaseWithGuidIdCollection")]
public partial class WithoutNormalizedAspNetIdentityGuidRoleStoreFeature;
