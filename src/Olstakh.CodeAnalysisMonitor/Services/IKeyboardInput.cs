namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Abstracts keyboard input for testability.
/// </summary>
internal interface IKeyboardInput
{
    /// <summary>Gets whether a key press is available to read.</summary>
    bool KeyAvailable { get; }

    /// <summary>Reads the next key press without displaying it.</summary>
    ConsoleKeyInfo ReadKey();
}
