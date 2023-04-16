namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class IdentityDapperOptions
{
    public IdentityDapperOptions(
        string schema,
        bool skipNormalized)
    {
        Schema = schema;
        SkipNormalized = skipNormalized;
    }

    public string Schema { get; }

    public bool SkipNormalized { get; }
}
