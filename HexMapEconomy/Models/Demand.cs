namespace HexMapEconomy.Models;

internal class Demand
{
    internal Factory Factory { get; init; }
    internal RecipeIngredient Ingredient { get; init; }

    internal Demand(Factory factory, RecipeIngredient value)
    {
        Factory = factory;
        Ingredient = value;
    }   
}
