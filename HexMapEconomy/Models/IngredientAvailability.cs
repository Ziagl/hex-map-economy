namespace HexMapEconomy.Models;

public record IngredientAvailability
{
    public int Type { get; init; } // type of ingredient
    public int Turns { get; init; } // number of turns ingredient is available
}
