using com.hexagonsimulations.HexMapBase.Models;

namespace HexMapEconomy.Models;

// a building that generates Assets or combines them to other Assets
public class Factory : EconomyBase
{
    public CubeCoordinates Position { get; init; }
    public Recipe Recipe { get; init; }
    public float Productivity { get => _lastTenTurnsOutput.Sum() / _lastTenTurnsOutput.Count(); }
    private readonly Queue<int> _lastTenTurnsOutput = new(10);

    public Factory(Recipe recipe, CubeCoordinates position, int type, int ownerId) : base(type,ownerId)
    {
        Position = position;
        Recipe = recipe;
    }

    public void Process()
    {
        // do recipe TODO -> output is 1 or 0 for productivity
        bool success = true;
        
        AddOutput(success ? 1 : 0);
    }

    private void AddOutput(int value)
    {
        if (_lastTenTurnsOutput.Count == 10)
            _lastTenTurnsOutput.Dequeue();
        _lastTenTurnsOutput.Enqueue(value);
    }
}
