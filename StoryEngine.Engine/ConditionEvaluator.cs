using StoryEngine.Model;

namespace StoryEngine.Engine;

/// <summary>
/// Evalueaza conditii AST recursiv.
/// </summary>
public static class ConditionEvaluator
{
    public static bool Evaluate(ConditionNode? node, GameState state)
    {
        if (node == null) return true;

        return node.Type switch
        {
            "COMPARISON" => EvaluateComparison(node, state),
            "AND"        => node.Conditions?.All(c => Evaluate(c, state)) ?? true,
            "OR"         => node.Conditions?.Any(c => Evaluate(c, state)) ?? false,
            _            => true
        };
    }

    private static bool EvaluateComparison(ConditionNode node, GameState state)
    {
        if (node.Property == null || node.Operator == null || node.Value == null)
            return true;

        double left  = state.Get(node.Property);
        double right = node.Value.Value;

        return node.Operator switch
        {
            "<"  => left <  right,
            "<=" => left <= right,
            ">"  => left >  right,
            ">=" => left >= right,
            "==" => Math.Abs(left - right) < 1e-9,
            "!=" => Math.Abs(left - right) >= 1e-9,
            _    => true
        };
    }
}
