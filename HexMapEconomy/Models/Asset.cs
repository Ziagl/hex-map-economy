using com.hexagonsimulations.HexMapBase.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HexMapEconomy.Tests")]

namespace HexMapEconomy.Models;

// an object that takes part in the economy
public class Asset : EconomyBase
{
    public CubeCoordinates Position { get; }

    internal Asset(CubeCoordinates position, int type, int ownerId) : base(type, ownerId)
    {
        Position = position;
    }
}
