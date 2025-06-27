using com.hexagonsimulations.HexMapBase.Models;

namespace HexMapEconomy.Models;

// a warehouse that stores Assets of attached factories
public class Warehouse : EconomyBase
{
    public CubeCoordinates Position { get; init; }  // map position of this warehouse
    public Stock Stock { get; } = new();            // stock for this warehouse

    internal List<Demand> Demands { get; } = new();   // list of demands for this warehouse

    /// <summary>
    /// Creates a new Warehouse. A Warehouse has always type 0, because there are not types yet.
    /// </summary>
    public Warehouse(CubeCoordinates position, int ownerId, int stockLimit) : base(0, ownerId)
    {
        Position = position;
        Stock = new Stock(stockLimit);
    }

    /// <summary>
    /// A factory can add a demand to its warehouse.
    /// </summary>
    /// <param name="demand">Demand object represents Asset that is needed but not in stock.</param>
    /// <returns>true if demand was added, false if factory does not belong to this warehouse.</returns>
    internal bool AddDemand(Demand demand)
    {
        if (demand.Factory.Warehouse.Id != Id)
        {
            return false;
        }
        Demands.Add(demand);
        return true;
    }
}
