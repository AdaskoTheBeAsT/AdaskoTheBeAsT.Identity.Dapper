namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class IdentityDapperOptions
{
    public IdentityDapperOptions(
        string schema,
        bool skipNormalized,
        string storeBooleanAs)
    {
        Schema = schema;
        SkipNormalized = skipNormalized;
        StoreBooleanAs = storeBooleanAs;
    }

    public string Schema { get; }

    public bool SkipNormalized { get; }

    public string? StoreBooleanAs { get; set; }
}
