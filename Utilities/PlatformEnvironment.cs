

using System;
using System.Reflection;

namespace Rumble.Platform.Common.Utilities;

/// <summary>
/// .NET doesn't always like to play nice with Environment Variables.  Conventional wisdom is to set them in the
/// appsettings.json file, but secrets (e.g. connection strings) are supposed to be handled in the .NET user secrets
/// tool.  After a couple hours of unsuccessful fiddling to get it to cooperate in Rider, I decided to do it the
/// old-fashioned way, by parsing a local file and ignoring it in .gitignore.  This class operates in much the same way
/// that RumbleJson does, allowing developers to take advantage of Require<T>() and Optional<T>().  It also contains custom
/// environment variable serialization for common platform environment variables, allowing us to configure any number of services
/// from a group-level CI variable in gitlab.
/// </summary>
public static class PlatformEnvironment // TODO: Add method to build a url out for service interop
{
    public static bool IsOffCluster => false;
    // DynamicConfig
    // .Instance
    // ?.GetValuesFor(Audience.GameClient)
    // ?.Optional<string>("gameServerUrl")
    // ?.Contains(ClusterUrl) ?? true;
    
        public static readonly string Version = Assembly
        .GetEntryAssembly()
        ?.GetName()
        .Version
        ?.ToString()
        ?? "Unknown";

    public static readonly string CommonVersion = ReadCommonVersion();

    private static string ReadCommonVersion()
    {
        Version v = Assembly.GetExecutingAssembly().GetName().Version;

        return v != null
            ? $"{v.Major}.{v.Minor}.{v.Build}"
            : "Unknown";
    }
};

// TODO: Incorporate DynamicConfigService as fallback values?