using Serilog;
using Serilog.Core;

namespace Passerelle.Host.Common;

public static class StaticLogger
{
    public static void EnsureInitialized()
    {
        if (Log.Logger is not Logger)
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();
    }
}