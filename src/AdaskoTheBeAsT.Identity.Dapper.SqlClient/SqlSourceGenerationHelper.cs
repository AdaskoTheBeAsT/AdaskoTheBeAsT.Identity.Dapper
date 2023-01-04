using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient;

public class SqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public SqlSourceGenerationHelper()
        : base(new SqlIdentityUserSourceGenerator())
    {
    }
}
