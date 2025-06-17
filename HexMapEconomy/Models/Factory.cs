using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Interfaces;

namespace HexMapEconomy.Models;

// a building that generates Assets or combines them to other Assets
public class Factory : EconomyBase
{
    public CubeCoordinates Position { get; init; }
    public Recipe Recipe { get; init; }
    public List<StockEntry> Stock { get; } = new();
    public int StockLimit { get; init; }
    private IAssetFactory _assetFactory { get; init; }
    public float Productivity { get => (float)_lastTenTurnsOutput.Sum() / (float)_lastTenTurnsOutput.Count(); }
    private readonly Queue<int> _lastTenTurnsOutput = new(10);

    public Factory(Recipe recipe, CubeCoordinates position, int type, int ownerId, IAssetFactory assetFactory, int stockLimit = 0) : base(type,ownerId)
    {
        Position = position;
        Recipe = recipe;
        _assetFactory = assetFactory;
        StockLimit = stockLimit;
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
            success = Recipe.Inputs.All(input =>
            {
                var stockEntry = Stock.FirstOrDefault(s => s.Type == input.Type);
                return stockEntry != null && stockEntry.Amount >= input.Amount;
            });

            if (success)
            {
                // consume the required inputs from stock
                foreach (var input in Recipe.Inputs)
                {
                    var stockEntry = Stock.First(s => s.Type == input.Type);
                    Stock.Remove(stockEntry);
                    int remaining = stockEntry.Amount - input.Amount;
                    if (remaining > 0)
                    {
                        Stock.Add(new StockEntry { Type = input.Type, Amount = remaining });
                    }
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
                    _assetFactory.CreateAsset(Position, output.Type, OwnerId);
                }
            });
        }

        // store meta data for productivity calculation
        AddOutput(success ? 1 : 0);
    }

    /// <summary>
    /// Adds an entry to the stock of this factory.
    /// </summary>
    /// <param name="entry">A stock entry to be added.</param>
    /// <returns>true: entry was added, false: it was not added, stock is full.</returns>
    public bool AddToStock(StockEntry entry)
    {
        // check if stock limit is reached
        if (Stock.Sum(s => s.Amount) + entry.Amount >= StockLimit)
        {
            return false;
        }

        // check if the stock entry already exists
        var existingEntry = Stock.FirstOrDefault(s => s.Type == entry.Type);
        if (existingEntry != null)
        {
            // update existing entry
            Stock.Remove(existingEntry);
            Stock.Add(new StockEntry { Type = entry.Type, Amount = existingEntry.Amount + entry.Amount });
        }
        else
        {
            // add new entry
            Stock.Add(entry);
        }
        return true;
    }

    // keeps track of the last 10 turns output for productivity calculation
    private void AddOutput(int value)
    {
        if (_lastTenTurnsOutput.Count == 10)
            _lastTenTurnsOutput.Dequeue();
        _lastTenTurnsOutput.Enqueue(value);
    }
}
