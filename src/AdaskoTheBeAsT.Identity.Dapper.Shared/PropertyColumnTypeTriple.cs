namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class PropertyColumnTypeTriple
{
    public PropertyColumnTypeTriple(
        string propertyName,
        string propertyType,
        string columnName)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
        ColumnName = columnName;
    }

    public string PropertyName { get; set; }

    public string PropertyType { get; set; }

    public string ColumnName { get; set; }
}
