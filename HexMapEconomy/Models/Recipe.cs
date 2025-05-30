namespace HexMapEconomy.Models;

// a description for a factory how to behave in the simulation
public class Recipe
{
    public List<Tuple<int, int>> Input { get; init; } // list of input assets, each tuple is (asset type, amount)
    public List<Tuple<int, int>> Output { get; init; } // list of output assets, each tuple is (asset type, amount)
    public int Duration { get; init; } // duration of the recipe in time units (e.g. turns, seconds)

    public Recipe(List<Tuple<int, int>> input, List<Tuple<int, int>> output)
    {
        Input = input;
        Output = output;
    }
}
