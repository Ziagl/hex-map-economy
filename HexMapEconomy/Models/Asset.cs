using com.hexagonsimulations.HexMapBase.Models;

namespace HexMapEconomy.Models;

// an object that takes part in the economy
public class Asset : EconomyBase
{
    public CubeCoordinates Position { get; }

    public Asset(CubeCoordinates position, int type, int ownerId) : base(type, ownerId)
    {
        Position = position;
    }
}
