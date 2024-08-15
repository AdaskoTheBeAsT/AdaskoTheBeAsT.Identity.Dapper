using AdaskoTheBeAsT.Identity.Dapper.Oracle;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace OracleConsoleApp;

internal static class Program
{
    private static void Main()
    {
        OracleDapperConfig.ConfigureTypeHandlers(BooleanAs.Char);
        var connStr = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=XEPDB1)));User Id=mydeveloper;Password=mypassword;";
        using (var conn = new OracleConnection(connStr))
        {
            var first = conn.QueryFirstOrDefault<Sample>("SELECT ID AS Id, STRID AS StrId FROM ADASKO");
            if (first != null)
            {
                var second = conn.QueryFirstOrDefault<Sample>(
                    "SELECT ID AS Id, STRID AS StrId FROM ADASKO WHERE ID=:Id",
                    new { first.Id });
                if (second != null)
                {
                    var str = second.Id.ToString();
                    Console.WriteLine(str);
                }
            }
        }

        Console.WriteLine("Hello, World!");
    }
}
