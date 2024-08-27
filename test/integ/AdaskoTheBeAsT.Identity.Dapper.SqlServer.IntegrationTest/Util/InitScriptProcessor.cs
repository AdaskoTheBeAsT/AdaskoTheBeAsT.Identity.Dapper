using System.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Util;

public static class InitScriptProcessor
{
    private const string SpecificLine01 = "PRINT N'Creating Table";
    private const string SpecificLine02 = "PRINT N'Creating Schema";
    private const string SpecificLine03 = "PRINT N'Creating SqlSchema";
    private const string SpecificLine04 = "PRINT N'Creating SqlTable";

    public static string PreprocessInitScript(string scriptPath, string dbName)
    {
        var found = false;
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE DATABASE [{dbName}];");
        sb.AppendLine("GO");
        sb.AppendLine($"USE [{dbName}];");
        sb.AppendLine("GO");

#pragma warning disable SCS0018
        using var sr = new StreamReader(scriptPath);
#pragma warning restore SCS0018

        while (sr.ReadLine() is { } line)
        {
            if (line.Contains(SpecificLine01, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(SpecificLine02, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(SpecificLine03, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(SpecificLine04, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
            }

            if (!found)
            {
                continue;
            }

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    public static string PreprocessInitDataScript(string scriptPath, string dbName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"USE [{dbName}];");
        sb.AppendLine("GO");

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
        using var sr = new StreamReader(scriptPath);
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.

        while (sr.ReadLine() is { } line)
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
