using HexMapEconomy.Models;

namespace HexMapEconomy;

public class AssetManager
{
    private Dictionary<Guid, Asset> _assetStore = new();

    public AssetManager()
    {
        
    }
}
