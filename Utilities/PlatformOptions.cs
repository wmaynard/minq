using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maynard.Logging;
using Rumble.Platform.Common.Extensions;
using Maynard.Extensions;

namespace Rumble.Platform.Common.Utilities;

public class PlatformOptions
{
    // public const int MINIMUM_THROTTLE_THRESHOLD = 50;
    // public const int MINIMUM_THROTTLE_PERIOD = 300;

    public const int MINIMUM_THROTTLE_THRESHOLD = 10;
    public const int MINIMUM_THROTTLE_PERIOD    = 60;
    public const int DEFAULT_THROTTLE_THRESHOLD = 100;
    public const int DEFAULT_THROTTLE_PERIOD    = 3_600; // 1 hour

    internal string ServiceName { get; set; }
    internal Type[] DisabledServices { get; set; }

    internal bool WebServerEnabled { get; set; }
    internal int WarningThreshold { get; set; }
    internal int ErrorThreshold { get; set; }
    internal int CriticalThreshold { get; set; }
    internal int LogThrottleThreshold { get; set; }
    internal int LogThrottlePeriodSeconds { get; set; }
    internal string RegistrationName { get; set; }
    internal Func<Task> BeforeStartup { get; set; }
    internal Action<PlatformOptions> OnApplicationReady { get; set; }
    internal double MaximumRequestsPerSecond { get; set; }
    // internal bool StartupLogsSuppressed { get; private set; }
    
    internal bool AspNetServicesEnabled { get; set; }
    internal bool WipeLocalDatabases { get; set; }

    internal PlatformOptions()
    {
        CustomFilters = new List<Type>();
        DisabledServices = Array.Empty<Type>();
        WebServerEnabled = false;
        WarningThreshold = 30_000;
        ErrorThreshold = 60_000;
        CriticalThreshold = 90_000;
        ServiceName = null;
        LogThrottleThreshold = DEFAULT_THROTTLE_THRESHOLD;
        LogThrottlePeriodSeconds = DEFAULT_THROTTLE_PERIOD;
        AspNetServicesEnabled = true;
    }

    private static T GetFullSet<T>() where T : Enum => ((T[])Enum.GetValues(typeof(T))).First().FullSet();


    public PlatformOptions OnBeforeStartup(Func<Task> action)
    {
        BeforeStartup = action;
        return this;
    }

    public PlatformOptions OnReady(Action<PlatformOptions> action)
    {
        OnApplicationReady = action;
        return this;
    }

    /// <summary>
    /// This name is required for Dynamic Config to create a section for the service.  Ideally, this name is human-readable / friendly.
    /// For example, "Chat" instead of "Chat Service".  This name will appear in Portal for managing values.  Changing this name will alter
    /// which section Dynamic Config pulls values from.
    public PlatformOptions SetRegistrationName(string name)
    {
        RegistrationName = name;
        return this;
    }

    public List<Type> CustomFilters { get; private set; }

    public PlatformOptions DisableAspNetServices()
    {
        AspNetServicesEnabled = false;
        return this;
    }

    /// <summary>
    /// Enables the application to serve files out of the wwwroot directory.  Used for Portal.
    /// </summary>
    public PlatformOptions EnableWebServer()
    {
        WebServerEnabled = true;
        return this;
    }

    /// <summary>
    /// Sets the thresholds responsible for sending warnings / errors / critical errors to Loggly when endpoints take a long time to return.
    /// </summary>
    public PlatformOptions SetPerformanceThresholds(int warnMS, int errorMS, int criticalMS)
    {
        WarningThreshold = warnMS;
        ErrorThreshold = errorMS;
        CriticalThreshold = criticalMS;
        return this;
    }

    /// <summary>
    /// Sets the maximum individual requests per second per non-admin account per instance of this server.  As an example, if set to 0.5,
    /// prolonged activity of hitting the server more than twice per second will begin a cooloff period where the account cannot access ANY
    /// resources.  This RPS limit is more generous when servers scale up since there will be more servers to handle requests.  Returns an
    /// error code of HTTP 418 I'm a teapot when triggered.
    /// </summary>
    /// <param name="rps"></param>
    /// <returns></returns>
    public PlatformOptions SetIndividualRps(double rps)
    {
        MaximumRequestsPerSecond = rps;
        return this;
    }

    /// <summary>
    /// Initializes the log throttling.  With a suppressAfter of 50 and period of 3600, up to 50 messages in one hour will be allowed.
    /// After that, the next log to be sent will only send after one hour after the first message.  When the throttled log sends,
    /// the cache is reset. 
    /// </summary>
    /// <param name="suppressAfter">The number of messages to allow before throttling kicks in.</param>
    /// <param name="period">The length of time, in seconds, </param>
    /// <returns></returns>
    public PlatformOptions SetLogglyThrottleThreshold(int suppressAfter, int period)
    {
        LogThrottleThreshold = suppressAfter;
        LogThrottlePeriodSeconds = period;

        return this;
    }

    public PlatformOptions WipeLocalDatabasesOnStartup(bool wipe = true)
    {
        WipeLocalDatabases = wipe;
        return this;
    }

    internal PlatformOptions Validate()
    {
        if (DisabledServices.Any())
            Log.Verbose("Some platform-common services have been disabled.  If you block a service that is used in dependency injection, the application will fail to start.  Other side effects are also possible.", data: new
            {
                DisabledServices = DisabledServices.Select(type => type.Name)
            });
        if (LogThrottleThreshold < MINIMUM_THROTTLE_THRESHOLD)
        {
            Log.Info("The log throttling threshold is too low and will be set to a minimum.", data: new
            {
                MinimumThreshold = MINIMUM_THROTTLE_THRESHOLD
            });
            LogThrottleThreshold = MINIMUM_THROTTLE_THRESHOLD;
        }
        if (LogThrottlePeriodSeconds < MINIMUM_THROTTLE_PERIOD)
        {
            Log.Info("The log throttling period is too low and will be set to a minimum.", data: new
            {
                MinimumPeriod = MINIMUM_THROTTLE_PERIOD
            });
            LogThrottlePeriodSeconds = MINIMUM_THROTTLE_PERIOD;
        }
        // TODO: Add more logs / protection here
        return this;
    }

    private T[] Flags<T>(T enums) where T : Enum => ((T[])Enum.GetValues(typeof(T)))
        .Where(service => enums.HasFlag(service))
        .ToArray();
}