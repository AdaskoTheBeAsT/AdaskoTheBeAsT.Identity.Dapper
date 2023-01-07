using System.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class IdentityClassGeneratorBase
{
    protected void GenerateUsing(StringBuilder sb)
    {
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine();
    }

    protected void GenerateNamespaceStart(StringBuilder sb, string namespaceName) =>
        sb.AppendLine(
            $@"namespace {namespaceName}
{{");

    protected void GenerateClassStart(StringBuilder sb, string className, string interfaceName) =>
        sb.AppendLine(
            $@"    public class {className}
        : {interfaceName}
    {{");

    protected void GenerateClassEnd(StringBuilder sb) =>
        sb.AppendLine("    }");

    protected void GenerateNamespaceEnd(StringBuilder sb) =>
        sb.AppendLine("}");
}
