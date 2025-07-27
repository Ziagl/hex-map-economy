namespace HexMapEconomy.Models;

public class RecipeIngredientsAvailability
{
    public int Turns { get; set; } // number of turns whole recipe is available
    public List<IngredientAvailability> AvailabilityDetails { get; set; } = new(); // list of tuples with type and turn count availability
}
