namespace HexMapEconomy.Models;

// a description for a factory how to behave in the simulation
public class Recipe
{
    public List<StockEntry> Inputs { get; init; } // list of input assets, each tuple is (asset type, amount)
    public List<StockEntry> Outputs { get; init; } // list of output assets, each tuple is (asset type, amount)
    public int Duration { get; init; } // duration of the recipe in time units (e.g. turns, seconds)

    public Recipe(List<StockEntry> inputs, List<StockEntry> outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
    }
}
