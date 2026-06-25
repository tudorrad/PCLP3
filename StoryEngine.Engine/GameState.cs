namespace StoryEngine.Engine;

/// <summary>
/// Starea curenta a jocului - valorile proprietatilor numerice.
/// </summary>
public class GameState
{
    private readonly Dictionary<string, double> _values = new();

    public IReadOnlyDictionary<string, double> Values => _values;

    public void Set(string key, double value) => _values[key] = value;

    public double Get(string key) => _values.TryGetValue(key, out var v) ? v : 0;

    public bool Has(string key) => _values.ContainsKey(key);

    /// <summary>
    /// Seteaza valoarea limitand-o la [min, max].
    /// </summary>
    public void SetClamped(string key, double value, double min, double max)
    {
        _values[key] = Math.Max(min, Math.Min(max, value));
    }
}
