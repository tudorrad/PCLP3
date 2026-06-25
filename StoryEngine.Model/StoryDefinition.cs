using System.Text.Json.Serialization;

namespace StoryEngine.Model;

/// <summary>
/// Definitia completa a unei povesti interactive.
/// </summary>
public class StoryDefinition
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("startBlock")]
    public string StartBlock { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<StatePropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("blocks")]
    public List<StoryBlock> Blocks { get; set; } = new();
}

/// <summary>
/// Un bloc narativ din poveste.
/// </summary>
public class StoryBlock
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("isFinal")]
    public bool IsFinal { get; set; } = false;

    [JsonPropertyName("backgroundImage")]
    public string? BackgroundImage { get; set; }

    [JsonPropertyName("decisions")]
    public List<DecisionDefinition> Decisions { get; set; } = new();
}

/// <summary>
/// O decizie posibila pentru utilizator.
/// </summary>
public class DecisionDefinition
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("targetBlock")]
    public string TargetBlock { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("condition")]
    public ConditionNode? Condition { get; set; }

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; set; } = new();
}

/// <summary>
/// Definitia unei proprietati de stare.
/// </summary>
public class StatePropertyDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("hudLabel")]
    public string HudLabel { get; set; } = string.Empty;

    [JsonPropertyName("min")]
    public double Min { get; set; } = 0;

    [JsonPropertyName("max")]
    public double Max { get; set; } = 100;

    [JsonPropertyName("initial")]
    public double Initial { get; set; } = 0;

    [JsonPropertyName("visibleInHud")]
    public bool VisibleInHud { get; set; } = true;

    [JsonPropertyName("hudOrder")]
    public int HudOrder { get; set; } = 99;

    [JsonPropertyName("onMinBlock")]
    public string? OnMinBlock { get; set; }

    [JsonPropertyName("onMaxBlock")]
    public string? OnMaxBlock { get; set; }
}

/// <summary>
/// Un efect aplicat asupra starii povestii.
/// </summary>
public class EffectDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ADD"; // ADD sau SET

    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; } = 0;
}

/// <summary>
/// Nod din AST-ul conditiei.
/// </summary>
public class ConditionNode
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // COMPARISON, AND, OR

    // Pentru COMPARISON
    [JsonPropertyName("property")]
    public string? Property { get; set; }

    [JsonPropertyName("operator")]
    public string? Operator { get; set; } // <, <=, >, >=, ==, !=

    [JsonPropertyName("value")]
    public double? Value { get; set; }

    // Pentru AND / OR
    [JsonPropertyName("conditions")]
    public List<ConditionNode>? Conditions { get; set; }
}
