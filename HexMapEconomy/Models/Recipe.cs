namespace HexMapEconomy.Models;

// a description for a factory how to behave in the simulation
public class Recipe
{
    public List<RecipeIngredient> Inputs { get; init; } // list of input assets as asset type and amount
    public List<RecipeIngredient> Outputs { get; init; } // list of output assets, each as asset type and amount
    public int Duration { get; init; } // duration of the recipe in time units (e.g. turns, seconds)

    public Recipe(List<RecipeIngredient> inputs, List<RecipeIngredient> outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
    }
}
