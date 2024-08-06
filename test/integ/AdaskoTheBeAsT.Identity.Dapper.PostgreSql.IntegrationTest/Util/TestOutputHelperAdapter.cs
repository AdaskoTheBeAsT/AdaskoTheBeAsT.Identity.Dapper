using DbUp.Engine.Output;
using Xunit.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Util;

public class TestOutputHelperAdapter
    : IUpgradeLog
{
    private readonly List<string> _messageCache = new();
    private ITestOutputHelper? _realOutputHelper;

    public ITestOutputHelper? RealOutputHelper
    {
        get => _realOutputHelper;
        set
        {
            _realOutputHelper = value;
            if (_realOutputHelper != null)
            {
                foreach (var message in _messageCache)
                {
                    _realOutputHelper.WriteLine(message);
                }

                _messageCache.Clear();
            }
        }
    }

    public void WriteLine(
        string format,
        params object[] args)
    {
        if (_realOutputHelper != null)
        {
            _realOutputHelper.WriteLine(format, args);
        }
        else
        {
            _messageCache.Add(string.Format(format, args));
        }
    }

    public void WriteInformation(string format, params object[] args)
    {
        WriteLine(format, args);
    }

    public void WriteError(string format, params object[] args)
    {
        WriteLine("ERROR: " + format, args);
    }

    public void WriteWarning(string format, params object[] args)
    {
        WriteLine("WARNING: " + format, args);
    }
}
