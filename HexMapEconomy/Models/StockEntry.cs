namespace HexMapEconomy.Models;

public record StockEntry
{
    public int Type { get; init; } // type of the asset
    public int Amount { get; init; } // amount of the asset in stock
}
