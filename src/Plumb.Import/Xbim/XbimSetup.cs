using Microsoft.Extensions.Logging;
using Xbim.Common.Configuration;

namespace Plumb.Import.Xbim;

public static class XbimSetup
{
    /// <summary>
    /// Routes xBIM's internal logging into the app's logger factory. Call once at startup; later calls are ignored.
    /// </summary>
    public static void Configure(ILoggerFactory loggerFactory)
    {
        if (XbimServices.Current.IsConfigured)
        {
            return;
        }

        XbimServices.Current.ConfigureServices(services =>
            services.AddXbimToolkit(builder => builder.AddLoggerFactory(loggerFactory)));
    }
}
