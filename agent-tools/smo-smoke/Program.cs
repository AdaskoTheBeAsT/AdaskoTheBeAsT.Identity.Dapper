using System;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

class Program
{
    static void Main()
    {
        try
        {
            var cs = "Server=localhost;Database=master;User Id=sa;Password=TestPass123!;TrustServerCertificate=True;Connect Timeout=2";
            using var conn = new SqlConnection(cs);
            var sc = new ServerConnection(conn);
            Console.WriteLine("ServerConnection ctor OK: " + sc.GetType().FullName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL: " + ex.GetType().FullName);
            Console.WriteLine(ex.Message);
            if (ex.InnerException != null) Console.WriteLine("Inner: " + ex.InnerException.Message);
            Environment.Exit(1);
        }
    }
}
