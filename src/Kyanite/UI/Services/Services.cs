using System.Reflection;

namespace Kyanite.Services;

public static class AppServices
{
    public static IPlatformService PlatformService { get; set; } = null!;

    public static Assembly NickelAsm { get; set; } = null!;
    public static string GamePath { get; set; } = null!;
}