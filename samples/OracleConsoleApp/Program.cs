using AdaskoTheBeAsT.Identity.Dapper.Oracle;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace OracleConsoleApp
{
    internal static class Program
    {
        private static void Main()
        {
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
            SqlMapper.AddTypeHandler(typeof(Guid), new OracleGuidTypeHandler(
                p =>
                {
                    if (p is OracleParameter oracleParameter)
                    {
                        oracleParameter.OracleDbType = OracleDbType.Raw;
                    }
                }));
            var connStr = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=XEPDB1)));User Id=mydeveloper;Password=mypassword;";
            using (var conn = new OracleConnection(connStr))
            {
                var first = conn.QueryFirstOrDefault<Sample>("SELECT ID AS Id, STRID AS StrId FROM ADASKO");
                var second = conn.QueryFirstOrDefault<Sample>(
                    "SELECT ID AS Id, STRID AS StrId FROM ADASKO WHERE ID=:Id",
                    new { first.Id });
                var str = second.Id.ToString();
                Console.WriteLine(str);
            }

            Console.WriteLine("Hello, World!");
        }
    }
}
