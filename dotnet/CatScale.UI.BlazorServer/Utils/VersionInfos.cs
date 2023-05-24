using System.Reflection;

namespace CatScale.UI.BlazorServer.Utils;

public static class VersionInfos
{
    public static string Version { get; }
    public static string BuildTime { get; }

    static VersionInfos()
    {
        var assembly = typeof(VersionInfos).Assembly;

        Version = assembly
            .GetName()
            .Version?
            .ToString()
                  ?? "unknown";

        BuildTime = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
                    ?? "unknown";
    }
}