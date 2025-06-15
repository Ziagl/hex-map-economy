using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;

namespace HexMapEconomy.Tests;

[TestClass]
public sealed class FactoryManagerTests
{
    private readonly int LUMBERJACK = 1;
    private readonly int SAWMILL = 2;
    private readonly AssetManager _assetManager = new AssetManager();

    [TestMethod]
    public void FactoryManagerBasics()
    {
        var factoryManager = new FactoryManager(GenerateFactoryTypes(), _assetManager);
        var position = new CubeCoordinates(0, 0, 0);
        int type = LUMBERJACK;
        int ownerId = 123;
        bool success = factoryManager.CreateFactory(position, type, ownerId);
        Assert.IsTrue(success, "Factory should be created successfully.");
        success = factoryManager.CreateFactory(position, 99, ownerId);
        Assert.IsFalse(success, "Factory should not be created with an unknown type.");
        Assert.AreEqual(1, factoryManager.CountFactories(), "Exactly one Factory should be in store.");
        var factories = factoryManager.GetFactoriesByPosition(position);
        Assert.AreEqual(1, factories.Count, "There should be one factory at the specified position.");
        success = factoryManager.ChangeFactoryOwner(factories.First().Id, 456);
        Assert.IsTrue(success, "Factory owner should be changed successfully.");
        Assert.AreEqual(456, factories.First().OwnerId, "Factory owner ID should be updated.");
        success = factoryManager.RemoveFactory(factories.First().Id);
        Assert.IsTrue(success, "Factory should be removed successfully.");
        Assert.AreEqual(0, factoryManager.CountFactories(), "Factory store should be empty after removal.");
    }

    [TestMethod]
    public void FactoryProcess()
    {
        // tests a factory that has no input
        var factoryManager = new FactoryManager(GenerateFactoryTypes(), _assetManager);
        var position = new CubeCoordinates(0, 0, 0);
        int type = LUMBERJACK;
        int ownerId = 1;
        bool success = factoryManager.CreateFactory(position, type, ownerId);
        Assert.IsTrue(success, "Factory should be created successfully.");
        var factory = factoryManager.GetFactoriesByPosition(position).First();
        factory.Process();
        Assert.AreEqual(1, factory.Productivity, "Factory should have produced one output.");
        // tests a factory that needs input
        position = new CubeCoordinates(1, 1, 1);
        type = SAWMILL;
        success = factoryManager.CreateFactory(position, type, ownerId);
        Assert.IsTrue(success, "Factory should be created successfully.");
        factory = factoryManager.GetFactoriesByPosition(position).First();
        factory.Process();
        // TODO: add input to the factory, so it can produce output
        Assert.AreEqual(1, factory.Productivity, "Factory should have produced one output.");
    }

    private Dictionary<int, Recipe> GenerateFactoryTypes()
    {
        return new Dictionary<int, Recipe>
        {
            { LUMBERJACK, new Recipe(
                new List<Tuple<int, int>>(), 
                new List<Tuple<int, int>>(){ new Tuple<int, int>(1, 1) }
                ) },    // lumberjack creates wood (abstract)
            { SAWMILL, new Recipe(
                new List<Tuple<int, int>>() { new Tuple<int, int>(1, 1) }, 
                new List<Tuple<int, int>>() { new Tuple<int, int>(2, 1) }
                ) },    // sawmill creates a plank from a wood (abstract)
        };
    }
}
