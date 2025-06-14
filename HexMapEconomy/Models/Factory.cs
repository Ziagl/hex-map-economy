using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Interfaces;

namespace HexMapEconomy.Models;

// a building that generates Assets or combines them to other Assets
public class Factory : EconomyBase
{
    public CubeCoordinates Position { get; init; }
    public Recipe Recipe { get; init; }
    private IAssetFactory _assetFactory { get; init; }
    public float Productivity { get => _lastTenTurnsOutput.Sum() / _lastTenTurnsOutput.Count(); }
    private readonly Queue<int> _lastTenTurnsOutput = new(10);

    public Factory(Recipe recipe, CubeCoordinates position, int type, int ownerId, IAssetFactory assetFactory) : base(type,ownerId)
    {
        Position = position;
        Recipe = recipe;
        _assetFactory = assetFactory;
    }

    /// <summary>
    /// Processes the recipe for this factory.
    /// </summary>
    public void Process()
    {
        bool success = false;

        // if a receipe has no inputs, the factury is a generator, like a mine or a lumberjack
        if (Recipe.Inputs.Count == 0)
        {
            // generator logic, e.g. mine or lumberjack
            success = true;
        }
        // otherwise this factory does manufacturing and needs inputs to generate outputs
        else
        {
            // only if all inputs are available
            // TODO: check inputs
        }

        // create output according to receipe
        if (success)
        {
            Recipe.Outputs.ForEach(output =>
            {
                for (int i = 0; i < output.Item2; i++)
                {
                    _assetFactory.CreateAsset(Position, output.Item1, OwnerId);
                }
            });
        }

        // store meta data for productivity calculation
        AddOutput(success ? 1 : 0);
    }

    // keeps track of the last 10 turns output for productivity calculation
    private void AddOutput(int value)
    {
        if (_lastTenTurnsOutput.Count == 10)
            _lastTenTurnsOutput.Dequeue();
        _lastTenTurnsOutput.Enqueue(value);
    }
}
