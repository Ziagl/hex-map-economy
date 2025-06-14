using HexMapEconomy.Models;

namespace HexMapEconomy;

public class AssetManager
{
    private Dictionary<Guid, Asset> _assetStore = new();

    public AssetManager()
    {
    }

    public Dictionary<Guid, Asset> AssetStore { get { return _assetStore; } }
}
