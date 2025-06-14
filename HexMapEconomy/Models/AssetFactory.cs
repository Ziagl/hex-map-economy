using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Interfaces;

namespace HexMapEconomy.Models;

internal class AssetFactory : IAssetFactory
{
    private Dictionary<Guid, Asset> _assetStore;

    public AssetFactory(Dictionary<Guid, Asset> assetStore)
    {
        _assetStore = assetStore;
    }

    void IAssetFactory.CreateAsset(CubeCoordinates position, int type, int ownerId)
    {
        // create new Asset instance
        var asset = new Asset(position, type, ownerId);

        _assetStore.Add(asset.Id, asset);
    }
}
