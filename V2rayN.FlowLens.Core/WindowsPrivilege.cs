using System.Runtime.Versioning;
using System.Security.Principal;

namespace V2rayN.FlowLens.Core;

public static class WindowsPrivilege
{
    public static bool IsAdministrator()
    {
        return OperatingSystem.IsWindows() && IsAdministratorOnWindows();
    }

    [SupportedOSPlatform("windows")]
    private static bool IsAdministratorOnWindows()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
