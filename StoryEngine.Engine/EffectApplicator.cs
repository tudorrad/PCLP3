using StoryEngine.Model;

namespace StoryEngine.Engine;

/// <summary>
/// Aplica efectele unei decizii asupra starii jocului.
/// </summary>
public static class EffectApplicator
{
    public static void Apply(IEnumerable<EffectDefinition> effects, GameState state,
        IEnumerable<StatePropertyDefinition> propDefs)
    {
        var defMap = propDefs.ToDictionary(p => p.Key);

        foreach (var effect in effects)
        {
            if (!defMap.TryGetValue(effect.Property, out var def)) continue;

            double current = state.Get(effect.Property);
            double newVal = effect.Type switch
            {
                "SET" => effect.Value,
                "ADD" => current + effect.Value,
                _     => current + effect.Value
            };

            state.SetClamped(effect.Property, newVal, def.Min, def.Max);
        }
    }
}
