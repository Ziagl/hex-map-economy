using com.hexagonsimulations.HexMapBase.Models;

namespace HexMapEconomy.Models;

// a building that generates Assets or combines them to other Assets
public class Factory : EconomyBase
{
    public CubeCoordinates Position { get; init; }
    public Recipe Recipe { get; init; }
    public Stock Stock { get; } = new();

    public float Productivity { get => (float)_lastTenTurnsOutput.Sum() / (float)_lastTenTurnsOutput.Count(); }
    private readonly Queue<int> _lastTenTurnsOutput = new(10);

    public Factory(Recipe recipe, CubeCoordinates position, int type, int ownerId, int stockLimit = 0) : base(type,ownerId)
    {
        Position = position;
        Recipe = recipe;
        Stock = new Stock(stockLimit);
    }

    /// <summary>
    /// Processes the recipe for this factory.
    /// </summary>
    internal void Process()
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
            success = Recipe.Inputs.All(input =>
            {
                var assets = Stock.Assets.Where(s => s.Type == input.Type);
                return assets.Count() >= input.Amount;
            });

            if (success)
            {
                // consume the required inputs from stock
                foreach (var input in Recipe.Inputs)
                {
                    var takenEntries = Stock.Take(input.Type, input.Amount);
                    // Only proceed if the required amount was actually taken
                    if (takenEntries.Count == 0)
                    {
                        success = false;
                        break;
                    }
                    // from here taken stock entries are not needed anymore
                }
            }
        }

        // create output according to receipe
        if (success)
        {
            Recipe.Outputs.ForEach(output =>
            {
                for (int i = 0; i < output.Amount; i++)
                {
                    var asset = new Asset(Position, output.Type, OwnerId);
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
