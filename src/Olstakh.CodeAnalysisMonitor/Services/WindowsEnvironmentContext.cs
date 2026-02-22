using System.Runtime.Versioning;
using System.Security.Principal;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Provides real environment context using system APIs.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsEnvironmentContext : IEnvironmentContext
{
    /// <inheritdoc />
    public bool IsRunningAsAdministrator
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
