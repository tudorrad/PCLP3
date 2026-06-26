using StoryEngine.Model;

namespace StoryEngine.Engine;

/// <summary>
/// Motorul principal al povestii interactive.
/// </summary>
public class StoryRuntime
{
    private readonly StoryDefinition _story;
    private GameState _state;
    private StoryBlock _currentBlock;

    public event Action<StoryBlock, IReadOnlyList<DecisionDefinition>>? BlockChanged;
    public event Action<string>? GameOver;

    public GameState State => _state;
    public StoryBlock CurrentBlock => _currentBlock;

    public StoryRuntime(StoryDefinition story)
    {
        _story  = story;
        _state  = new GameState();
        _currentBlock = GetBlock(story.StartBlock)
            ?? throw new InvalidOperationException($"Block '{story.StartBlock}' not found.");

        // Initializeaza toate proprietatile
        foreach (var prop in story.Properties)
            _state.SetClamped(prop.Key, prop.Initial, prop.Min, prop.Max);
    }

    /// <summary>
    /// Porneste povestea – notifica blocul initial.
    /// </summary>
    public void Start()
    {
        NotifyBlock();
    }

    /// <summary>
    /// Utilizatorul alege o decizie.
    /// </summary>
    public void Choose(DecisionDefinition decision)
    {
        // Aplica efectele
        EffectApplicator.Apply(decision.Effects, _state, _story.Properties);

        // Verifica daca vreo proprietate a ajuns la min/max
        foreach (var prop in _story.Properties)
        {
            double val = _state.Get(prop.Key);
            if (!string.IsNullOrEmpty(prop.OnMinBlock) && Math.Abs(val - prop.Min) < 1e-9)
            {
                NavigateTo(prop.OnMinBlock);
                return;
            }
            if (!string.IsNullOrEmpty(prop.OnMaxBlock) && Math.Abs(val - prop.Max) < 1e-9)
            {
                NavigateTo(prop.OnMaxBlock);
                return;
            }
        }

        NavigateTo(decision.TargetBlock);
    }

    private void NavigateTo(string blockId)
    {
        var block = GetBlock(blockId);
        if (block == null)
        {
            GameOver?.Invoke($"Block '{blockId}' not found.");
            return;
        }
        _currentBlock = block;
        NotifyBlock();
    }

    private void NotifyBlock()
    {
        if (_currentBlock.IsFinal)
        {
            BlockChanged?.Invoke(_currentBlock, Array.Empty<DecisionDefinition>());
            GameOver?.Invoke(_currentBlock.Id);
            return;
        }

        // Filtreaza deciziile in functie de conditii
        var available = _currentBlock.Decisions
            .Where(d => ConditionEvaluator.Evaluate(d.Condition, _state))
            .ToList();

        BlockChanged?.Invoke(_currentBlock, available);
    }

    private StoryBlock? GetBlock(string id) =>
        _story.Blocks.FirstOrDefault(b => b.Id == id);

    /// <summary>
    /// Returneaza toate blocurile (pentru editor).
    /// </summary>
    /// 
    public void RestoreState(string blockId, Dictionary<string, double> values)
    {
        var block = GetBlock(blockId)
            ?? throw new InvalidOperationException($"Block '{blockId}' not found in save file.");

        _currentBlock = block;

        foreach (var kv in values)
            _state.Set(kv.Key, kv.Value);

        NotifyBlock();
    }
    public IReadOnlyList<StoryBlock> AllBlocks => _story.Blocks;
    public StoryDefinition Definition => _story;
}
