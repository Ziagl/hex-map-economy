namespace HexMapEconomy.Models;

public class Stock
{
    public List<StockEntry> Entries { get; } = new();
    public int StockLimit { get; init; }

    public Stock(int stockLimit = 0)
    {
        StockLimit = stockLimit;
    }

    /// <summary>
    /// Adds an entry to the stock of this factory.
    /// </summary>
    /// <param name="entry">A stock entry to be added.</param>
    /// <returns>true: entry was added, false: it was not added, stock is full.</returns>
    public bool Add(StockEntry entry)
    {
        // check if stock limit is reached
        if (Entries.Sum(s => s.Amount) + entry.Amount >= StockLimit)
        {
            return false;
        }

        // check if the stock entry already exists
        var existingEntry = Entries.FirstOrDefault(s => s.Type == entry.Type);
        if (existingEntry != null)
        {
            // update existing entry
            Entries.Remove(existingEntry);
            Entries.Add(new StockEntry { Type = entry.Type, Amount = existingEntry.Amount + entry.Amount });
        }
        else
        {
            // add new entry
            Entries.Add(entry);
        }
        return true;
    }

    /// <summary>
    /// Removes a specified amount of stock entry from the stock.
    /// </summary>
    /// <param name="type">The type of stock entry that should be taken.</param>
    /// <param name="amount">The amount of stock entry that should be taken.</param>
    /// <returns>List of entries from stock or an empty list if it was not possible to take that type with given amount.</returns>
    public List<StockEntry> Take(int type, int amount)
    {
        var stockEntry = Entries.FirstOrDefault(s => s.Type == type);
        if (stockEntry != null && stockEntry.Amount >= amount)
        {
            Entries.Remove(stockEntry);
            int remaining = stockEntry.Amount - amount;
            if (remaining > 0)
            {
                Entries.Add(new StockEntry { Type = type, Amount = remaining });
            }
            return new List<StockEntry> { new StockEntry { Type = type, Amount = amount } };
        }
        return new List<StockEntry>();
    }
}
