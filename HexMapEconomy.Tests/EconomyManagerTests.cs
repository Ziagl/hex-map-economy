using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;

namespace HexMapEconomy.Tests;

[TestClass]
public sealed class EconomyManagerTests
{
    private readonly int LUMBERJACK = 1;
    private readonly int SAWMILL = 2;

    [TestMethod]
    public void EconomyManagerBasics()
    {
        var manager = new EconomyManager(GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsFalse(success, "Warehouse should not be created at the same position.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // factories
        position = new CubeCoordinates(1, 1, -2);
        int type = LUMBERJACK;
        success = manager.CreateFactory(position, type, 2, warehouse);
        Assert.IsFalse(success, "Factory should not be created with a different owner than warehouse.");
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        Assert.IsTrue(success, "Factory should be created successfully.");
        success = manager.CreateFactory(position, 99, ownerId, warehouse);
        Assert.IsFalse(success, "Factory should not be created with an unknown type.");
        Assert.AreEqual(1, manager.CountFactories(), "Exactly one Factory should be in store.");
        var factories = manager.GetFactoriesByPosition(position);
        Assert.AreEqual(1, factories.Count, "There should be one factory at the specified position.");
        success = manager.ChangeFactoryOwner(factories.First().Id, 456);
        Assert.IsTrue(success, "Factory owner should be changed successfully.");
        Assert.AreEqual(456, factories.First().OwnerId, "Factory owner ID should be updated.");
        success = manager.RemoveFactory(factories.First().Id);
        Assert.IsTrue(success, "Factory should be removed successfully.");
        Assert.AreEqual(0, manager.CountFactories(), "Factory store should be empty after removal.");
        
    }

    [TestMethod]
    public void FactoryProcess()
    {
        var manager = new EconomyManager(GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // factory tests
        position = new CubeCoordinates(1, 2, -3);
        int type = LUMBERJACK;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        Assert.IsTrue(success, "Factory should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        manager.ProcessFactories();
        Assert.AreEqual(1, factory.Productivity, "Factory should have produced one output.");
        // tests a factory that needs input
        position = new CubeCoordinates(1, 1, -2);
        type = SAWMILL;
        success = manager.CreateFactory(position, type, ownerId, warehouse, 5);
        Assert.IsTrue(success, "Factory should be created successfully.");
        factory = manager.GetFactoriesByPosition(position).First();
        manager.ProcessFactories();
        Assert.AreEqual(0, factory.Productivity, "Factory should not produce output without input.");
        success = factory.InputStock.Add(CreateAssets(1, 1, position, ownerId).First()); // add wood to stock
        Assert.IsTrue(success, "Wood should be added to factory stock.");
        manager.ProcessFactories();
        Assert.AreEqual(0.5f, factory.Productivity, "Factory should have produced one output.");
    }

    [TestMethod]
    public void FactoryStock()
    {
        var manager = new EconomyManager(GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // factories
        position = new CubeCoordinates(1, 1, -2);
        var type = SAWMILL;
        int stockLimit = 5;
        success = manager.CreateFactory(position, type, ownerId, warehouse, stockLimit);
        Assert.IsTrue(success, "Factory should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        int added = factory.InputStock.AddRange(CreateAssets(1, 3, position, ownerId));
        Assert.AreEqual(3, added, "Factory should accept wood into stock.");
        added = factory.InputStock.AddRange(CreateAssets(1, 3, position, ownerId));
        Assert.AreEqual(0, added, "Factory should not accept more wood than stock limit.");
        added = factory.InputStock.AddRange(CreateAssets(2, 1, position, ownerId));
        Assert.AreEqual(1, added, "Factory should accept mixed stock.");
        added = factory.InputStock.AddRange(CreateAssets(2, 2, position, ownerId));
        Assert.AreEqual(0, added, "Factory should not accept more stock entires than stock limit.");
        manager.ProcessFactories(); // process factory to make assets available
        var entries = factory.InputStock.Take(3, 2);
        Assert.IsTrue(entries.Count == 0, "Factory should not take stock entries that do not exist.");
        entries = factory.InputStock.Take(2, 4);
        Assert.IsTrue(entries.Count == 0, "Factory should not take stock entries if the requested amount is not available.");
        entries = factory.InputStock.Take(1, 2); // only get 2, because ProcessFactories also consumes one of 3 in store!
        Assert.IsTrue(entries.Count == 2 && entries.All(x => x.Type == 1), "Factory should take stock entries that exist and have enough amount.");
    }

    [TestMethod]
    public void FactoryDemands()
    {
        var manager = new EconomyManager(GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(1, 0, -1);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // add a generator
        position = new CubeCoordinates(0, 0, 0);
        int type = LUMBERJACK;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        var generator = manager.GetFactoriesByPosition(position).First();
        Assert.IsTrue(success, "Factory (generator) should be created successfully.");
        // add a producer
        position = new CubeCoordinates(1, 1, -2);
        type = SAWMILL;
        int stockLimit = 2;
        int areaOfInfluence = 1;
        success = manager.CreateFactory(position, type, ownerId, warehouse, stockLimit, areaOfInfluence);
        Assert.IsTrue(success, "Factory (producer) should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        // compute 1 turn
        // generator creates 1 wood and puts it into stock of producer -> distance is 2
        manager.ProcessFactories();
        Assert.AreEqual(1, factory.InputStock.Assets.Count, "Factory should have 1 wood in stock after processing.");
        Assert.AreEqual(2, factory.InputStock.Assets.First().TurnsUntilAvailable, "Asset should have an available counter of 2.");
        manager.ProcessFactories();
        Assert.AreEqual(2, factory.InputStock.Assets.Count, "Factory should still have 2 wood in stock after second processing.");
        Assert.AreEqual(0, factory.OutputStock.Assets.Count, "Factory should have an empty output stock after second processing.");
        manager.ProcessFactories();
        Assert.AreEqual(1, factory.OutputStock.Assets.Count, "Factory should have 1 plank in output stock after third processing.");
    }

    private List<Asset> CreateAssets(int type, int amount, CubeCoordinates position, int ownerId, int distance = 1)
    {
        var assets = new List<Asset>();
        for (int i = 0; i < amount; i++)
        {
            var asset = new Asset(position, type, ownerId);
            asset.InitializeTransport(position, distance);
            assets.Add(asset);
        }

        return assets;
    }

    private Dictionary<int, Recipe> GenerateFactoryTypes()
    {
        return new Dictionary<int, Recipe>
        {
            { LUMBERJACK, new Recipe(
                new List<RecipeIngredient>(), 
                new List<RecipeIngredient>(){ new RecipeIngredient() { Type = 1, Amount = 1 } }
                ) },    // lumberjack creates wood (abstract)
            { SAWMILL, new Recipe(
                new List<RecipeIngredient>() { new RecipeIngredient() { Type = 1, Amount = 1 } }, 
                new List<RecipeIngredient>() { new RecipeIngredient() { Type = 2, Amount = 1 } }
                ) },    // sawmill creates a plank from a wood (abstract)
        };
    }
}
