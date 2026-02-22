namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Abstracts environment checks (e.g. admin privileges) for testability.
/// </summary>
internal interface IEnvironmentContext
{
    /// <summary>Gets whether the current process is running with administrator privileges.</summary>
    bool IsRunningAsAdministrator { get; }
}
