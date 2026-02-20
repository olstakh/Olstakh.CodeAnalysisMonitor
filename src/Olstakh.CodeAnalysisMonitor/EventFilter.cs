namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Parses and evaluates key=value payload filters against ETW event payloads.
/// All filters must match (AND semantics) for an event to pass.
/// </summary>
internal sealed class EventFilter
{
    private readonly IReadOnlyList<KeyValuePair<string, string>> _filters;

    private EventFilter(IReadOnlyList<KeyValuePair<string, string>> filters)
    {
        _filters = filters;
    }

    /// <summary>
    /// Parses raw filter strings in "key=value" format.
    /// </summary>
    /// <param name="rawFilters">Filter strings, each in the format "key=value".</param>
    /// <returns>A new <see cref="EventFilter"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when a filter string is not in the expected format.</exception>
    public static EventFilter Parse(IReadOnlyList<string> rawFilters)
    {
        var filters = new List<KeyValuePair<string, string>>(rawFilters.Count);

        foreach (var raw in rawFilters)
        {
            var separatorIndex = raw.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex < 1)
            {
                throw new ArgumentException(
                    $"Invalid filter format: '{raw}'. Expected 'key=value' (e.g., generatorName=MyGenerator).",
                    nameof(rawFilters));
            }

            var key = raw[..separatorIndex];
            var value = raw[(separatorIndex + 1)..];
            filters.Add(new KeyValuePair<string, string>(key, value));
        }

        return new EventFilter(filters);
    }

    /// <summary>
    /// Creates an empty filter that matches all events.
    /// </summary>
    public static EventFilter None { get; } = new([]);

    /// <summary>
    /// Returns true if the event's payloads satisfy all filter criteria.
    /// </summary>
    /// <param name="payloadAccessor">
    /// A function that retrieves a payload value by name from the trace event.
    /// </param>
    public bool Matches(Func<string, object?> payloadAccessor)
    {
        foreach (var filter in _filters)
        {
            var payloadValue = payloadAccessor(filter.Key);
            if (payloadValue is null)
            {
                return false;
            }

            if (!string.Equals(payloadValue.ToString(), filter.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
