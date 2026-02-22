namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Reads keyboard input from <see cref="Console"/>.
/// </summary>
internal sealed class ConsoleKeyboardInput : IKeyboardInput
{
    /// <inheritdoc />
    public bool KeyAvailable => Console.KeyAvailable;

    /// <inheritdoc />
    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);
}
