using com.hexagonsimulations.HexMapBase.Models;

namespace HexMapEconomy.Models;

// a warehouse that stores Assets of attached factories
public class Warehouse : EconomyBase
{
    public CubeCoordinates Position { get; init; }  // map position of this warehouse
    public Stock Stock { get; } = new();            // stock for this warehouse

    /// <summary>
    /// Creates a new Warehouse. A Warehouse has always type 0, because there are not types yet.
    /// </summary>
    public Warehouse(CubeCoordinates position, int ownerId, int stockLimit) : base(0, ownerId)
    {
        Position = position;
        Stock = new Stock(stockLimit);
    }
}
