namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class PropertyColumnPair
{
    public PropertyColumnPair(
        string propertyName,
        string columnName)
    {
        PropertyName = propertyName;
        ColumnName = columnName;
    }

    public string PropertyName { get; set; }

    public string ColumnName { get; set; }
}
