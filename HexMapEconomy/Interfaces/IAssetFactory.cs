using com.hexagonsimulations.HexMapBase.Models;

namespace HexMapEconomy.Interfaces;

public interface IAssetFactory
{
   public void CreateAsset(CubeCoordinates position, int type, int ownerId);
}
