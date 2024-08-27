using DbUp.Engine.Output;
using Xunit.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Util;

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

    public void LogTrace(
        string format,
        params object[] args)
    {
        WriteLine("Trace:" + format, args);
    }

    public void LogDebug(
        string format,
        params object[] args)
    {
        WriteLine("Debug:" + format, args);
    }

    public void LogInformation(
        string format,
        params object[] args)
    {
        WriteLine("Information:" + format, args);
    }

    public void LogWarning(
        string format,
        params object[] args)
    {
        WriteLine("Warning:" + format, args);
    }

    public void LogError(
        string format,
        params object[] args)
    {
        WriteLine("Error:" + format, args);
    }

    public void LogError(
        Exception ex,
        string format,
        params object[] args)
    {
        WriteLine("Error:" + format, args);
    }
}
