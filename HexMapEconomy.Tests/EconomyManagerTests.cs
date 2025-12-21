using com.hexagonsimulations.HexMapBase.Models;
using com.hexagonsimulations.HexMapEconomy.Models;

namespace com.hexagonsimulations.HexMapEconomy.Tests;

[TestClass]
public sealed class EconomyManagerTests
{
    [TestMethod]
    public void EconomyManagerBasics()
    {
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
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
        int type = TestUtils.LUMBERJACK;
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
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // factory tests
        position = new CubeCoordinates(1, 2, -3);
        int type = TestUtils.LUMBERJACK;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        Assert.IsTrue(success, "Factory should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        manager.ProcessFactories();
        Assert.AreEqual(1, factory.Productivity, "Factory should have produced one output.");
        // tests a factory that needs input
        position = new CubeCoordinates(1, 1, -2);
        type = TestUtils.SAWMILL;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        Assert.IsTrue(success, "Factory should be created successfully.");
        factory = manager.GetFactoriesByPosition(position).First();
        manager.GetWarehouseById(factory.WarehouseId).Stock.Clear();
        manager.ProcessFactories();
        Assert.AreEqual(0, factory.Productivity, "Factory should not produce output without input.");
        success = manager.GetWarehouseById(factory.WarehouseId).Stock.Add(TestUtils.CreateAssets(1, 1, position, ownerId).First()); // add wood to stock
        Assert.IsTrue(success, "Wood should be added to factory stock.");
        manager.ProcessFactories();
        Assert.AreEqual(0.5f, factory.Productivity, "Factory should have produced one output.");
    }

    [TestMethod]
    public void FactoryStock()
    {
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // factories
        position = new CubeCoordinates(1, 1, -2);
        var type = TestUtils.SAWMILL;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        Assert.IsTrue(success, "Factory should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        int added = manager.GetWarehouseById(factory.WarehouseId).Stock.AddRange(TestUtils.CreateAssets(1, 3, position, ownerId));
        Assert.AreEqual(3, added, "Factory should accept wood into stock.");
        success = manager.GetWarehouseById(factory.WarehouseId).Stock.Has(new Dictionary<int, int> { { 1, 3 } });
        Assert.IsTrue(success, "Factory stock should have 3 wood.");
        success = manager.GetWarehouseById(factory.WarehouseId).Stock.Has(new Dictionary<int, int> { { 2, 1 } });
        Assert.IsFalse(success, "Factory stock should not have any planks.");
        added = manager.GetWarehouseById(factory.WarehouseId).Stock.AddRange(TestUtils.CreateAssets(2, 1, position, ownerId));
        Assert.AreEqual(1, added, "Factory should accept mixed stock.");
        manager.ProcessFactories(); // process factory to make assets available
        var entries = manager.GetWarehouseById(factory.WarehouseId).Stock.Take(3, 2);
        Assert.IsTrue(entries.Count == 0, "Factory should not take stock entries that do not exist.");
        entries =  manager.GetWarehouseById(factory.WarehouseId).Stock.Take(2, 4);
        Assert.IsTrue(entries.Count == 0, "Factory should not take stock entries if the requested amount is not available.");
        entries = manager.GetWarehouseById(factory.WarehouseId).Stock.Take(1, 2); // only get 2, because ProcessFactories also consumes one of 3 in store!
        Assert.IsTrue(entries.Count == 2 && entries.All(x => x.Type == 1), "Factory should take stock entries that exist and have enough amount.");
    }

    [TestMethod]
    public void FactoryDemands()
    {
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(1, 0, -1);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // add a generator
        position = new CubeCoordinates(0, 0, 0);
        int type = TestUtils.LUMBERJACK;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        var generator = manager.GetFactoriesByPosition(position).First();
        Assert.IsTrue(success, "Factory (generator) should be created successfully.");
        // add a producer
        position = new CubeCoordinates(1, 1, -2);
        type = TestUtils.SAWMILL;
        success = manager.CreateFactory(position, type, ownerId, warehouse);
        Assert.IsTrue(success, "Factory (producer) should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        // compute 1 turn
        // generator creates 1 wood and puts it into stock of warehouse
        manager.ProcessFactories();
        Assert.AreEqual(1, manager.GetWarehouseById(factory.WarehouseId).Stock.Assets.Count, "Factory should have 1 wood in stock after processing.");
        manager.ProcessFactories();
        Assert.AreEqual(1, manager.GetWarehouseById(factory.WarehouseId).Stock.Assets.Where(a => a.Type == 1).ToList().Count, "Factory should have 1 new wood in stock after second processing.");
        Assert.AreEqual(1, manager.GetWarehouseById(factory.WarehouseId).Stock.Assets.Where(a => a.Type == 2).ToList().Count, "Factory should have 1 new plank in stock after second processing.");
        manager.ProcessFactories();
        Assert.AreEqual(2, manager.GetWarehouseById(factory.WarehouseId).Stock.Assets.Where(a => a.Type == 2).ToList().Count, "Factory should have 2 planks in output stock after third processing.");
    }

    [TestMethod]
    public void WarehousesHandleDemands()
    {
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse 1 should be created successfully.");
        var warehouse1 = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse1, "Warehouse 1 should be found by position.");
        position = new CubeCoordinates(2, 0, -2);
        success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse 2 should be created successfully.");
        var warehouse2 = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse2, "Warehouse 2 should be found by position.");
        // factories
        position = new CubeCoordinates(-1, 1, 0);
        int type = TestUtils.LUMBERJACK;
        success = manager.CreateFactory(position, type, ownerId, warehouse1);
        var generator = manager.GetFactoriesByPosition(position).First();
        Assert.IsTrue(success, "Factory (generator) should be created successfully.");
        position = new CubeCoordinates(1, 1, -2);
        type = TestUtils.SAWMILL;
        success = manager.CreateFactory(position, type, ownerId, warehouse2);
        Assert.IsTrue(success, "Factory (producer) should be created successfully.");
        var factory = manager.GetFactoriesByPosition(position).First();
        // transportation tests
        manager.ProcessFactories(); // process factories to generate assets
        Assert.AreEqual(0, warehouse1.Stock.Assets.Count, "Stock of Warehouse 1 should be empty.");
        Assert.AreEqual(1, warehouse2.Stock.Assets.Count, "Stock of Warehouse 2 should not be empty.");
        Assert.AreEqual(1, warehouse2.Stock.Assets.First().Type, "Asset has to be of type 1.");
        manager.ProcessFactories(); // process factories to generate assets
        Assert.AreEqual(0, warehouse1.Stock.Assets.Count, "Stock of Warehouse 1 should be empty.");
        Assert.AreEqual(2, warehouse2.Stock.Assets.Count, "Stock of Warehouse 2 should not be empty.");
    }

    [TestMethod]
    public void EstimateDeliveryTime()
    {
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        // add 3 warehouses with different distances to first warehouse
        var position1 = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position1, ownerId, 10);
        Assert.IsTrue(success, "Warehouse 1 should be created successfully.");
        var warehouse1 = manager.GetWarehouseByPosition(position1);
        Assert.IsNotNull(warehouse1, "Warehouse 1 should be found by position.");
        warehouse1.Stock.AddRange(TestUtils.CreateAssets(1, 1, position1, ownerId));
        var position2 = new CubeCoordinates(5, 0, -5);
        success = manager.CreateWarehouse(position2, ownerId, 10);
        Assert.IsTrue(success, "Warehouse 2 should be created successfully.");
        var warehouse2 = manager.GetWarehouseByPosition(position2);
        Assert.IsNotNull(warehouse2, "Warehouse 2 should be found by position.");
        warehouse2.Stock.AddRange(TestUtils.CreateAssets(1, 1, position2, ownerId));
        var position3 = new CubeCoordinates(10, 0, -10);
        success = manager.CreateWarehouse(position3, ownerId, 10);
        Assert.IsTrue(success, "Warehouse 3 should be created successfully.");
        var warehouse3 = manager.GetWarehouseByPosition(position3);
        Assert.IsNotNull(warehouse3, "Warehouse 3 should be found by position.");
        warehouse3.Stock.AddRange(TestUtils.CreateAssets(1, 1, position3, ownerId));
        // test delivery time estimation
        var availability = manager.EstimateDeliveryTime(new List<RecipeIngredient>() { new RecipeIngredient() { Type = 1, Amount = 1 } }, ownerId, position1);
        Assert.IsTrue(0 == availability.Turns, "Delivery time for 1 asset should be 0 turns.");
        Assert.IsTrue(0 == availability.AvailabilityDetails[0].Turns, "Delivery time for first asset should be 0 turns.");
        availability = manager.EstimateDeliveryTime(new List<RecipeIngredient>() { new RecipeIngredient() { Type = 1, Amount = 2 } }, ownerId, position1);
        Assert.IsTrue(1 == availability.Turns, "Delivery time for 1 asset should be 1 turn.");
        Assert.IsTrue(1 == availability.AvailabilityDetails[0].Turns, "Delivery time for first asset should be 1 turn.");
        availability = manager.EstimateDeliveryTime(new List<RecipeIngredient>() { new RecipeIngredient() { Type = 1, Amount = 3 } }, ownerId, position1);
        Assert.IsTrue(2 == availability.Turns, "Delivery time for 1 asset should be 2 turns.");
        Assert.IsTrue(2 == availability.AvailabilityDetails[0].Turns, "Delivery time for first asset should be 2 turns.");
        availability = manager.EstimateDeliveryTime(new List<RecipeIngredient>() { new RecipeIngredient() { Type = 1, Amount = 1 }, new RecipeIngredient() { Type = 2, Amount = 1 } }, ownerId, position1);
        Assert.IsTrue(-1 == availability.Turns, "Delivery time for 1 asset should be -1 turns, because it is not available.");
        Assert.IsTrue(0 == availability.AvailabilityDetails[0].Turns, "Delivery time for first asset should be 0 turns.");
        Assert.IsTrue(-1 == availability.AvailabilityDetails[1].Turns, "Delivery time for second asset should be -1 turns, because it is not available.");
    }

    [TestMethod]
    public void TradeAssetsForOtherAssets()
    {
        var manager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        // add warehouse
        bool success = manager.CreateWarehouse(position, ownerId, 20);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // test 1: Basic 1:1 trade (3 wood -> 3 planks)
        var woodAssets = TestUtils.CreateAssets(1, 3, position, ownerId);
        warehouse.Stock.AddRange(woodAssets);
        manager.ProcessFactories();
        Assert.AreEqual(3, warehouse.Stock.Assets.Count, "Stock should have 3 wood assets.");
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, woodAssets, 2, tradeFactor: 1);
        Assert.IsTrue(success, "Trade should succeed with 1:1 ratio.");
        Assert.AreEqual(3, warehouse.Stock.Assets.Count, "Stock should still have 3 assets after trade.");
        Assert.AreEqual(0, warehouse.Stock.GetCount(1), "Stock should have no wood after trade.");
        Assert.AreEqual(3, warehouse.Stock.GetCount(2), "Stock should have 3 planks after trade.");
        // test 2: 2:1 trade (4 planks -> 2 iron)
        warehouse.Stock.Clear();
        var plankAssets = TestUtils.CreateAssets(2, 4, position, ownerId);
        warehouse.Stock.AddRange(plankAssets);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, plankAssets, 3, tradeFactor: 2);
        Assert.IsTrue(success, "Trade should succeed with 2:1 ratio.");
        Assert.AreEqual(2, warehouse.Stock.Assets.Count, "Stock should have 2 assets after 2:1 trade.");
        Assert.AreEqual(0, warehouse.Stock.GetCount(2), "Stock should have no planks after trade.");
        Assert.AreEqual(2, warehouse.Stock.GetCount(3), "Stock should have 2 iron after trade.");
        // test 3: Insufficient assets for trade factor (3 assets with 4:1 factor should fail to create new asset but still work with 0 output)
        warehouse.Stock.Clear();
        var ironAssets = TestUtils.CreateAssets(3, 3, position, ownerId);
        warehouse.Stock.AddRange(ironAssets);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, ironAssets, 4, tradeFactor: 4);
        Assert.IsFalse(success, "Trade should fail when not enough assets to create at least 1 new asset.");
        Assert.AreEqual(3, warehouse.Stock.Assets.Count, "Stock should remain unchanged after failed trade.");
        // test 4: Trade with non-existent assets should fail
        warehouse.Stock.Clear();
        var fakeAssets = TestUtils.CreateAssets(5, 2, position, ownerId);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, fakeAssets, 6, tradeFactor: 1);
        Assert.IsFalse(success, "Trade should fail when assets don't exist in stock.");
        Assert.AreEqual(0, warehouse.Stock.Assets.Count, "Stock should remain empty after failed trade.");
        // test 5: Trade with unavailable assets should fail
        warehouse.Stock.Clear();
        var unavailableAssets = new List<Asset>
        {
            new Asset(Guid.NewGuid(), position, 1, ownerId, turnsUntilAvailable: 3, isAvailable: false),
            new Asset(Guid.NewGuid(), position, 1, ownerId, turnsUntilAvailable: 2, isAvailable: false)
        };
        warehouse.Stock.AddRange(unavailableAssets);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, unavailableAssets, 2, tradeFactor: 1);
        Assert.IsFalse(success, "Trade should fail when assets are not available.");
        Assert.AreEqual(2, warehouse.Stock.Assets.Count, "Stock should remain unchanged.");
        Assert.AreEqual(2, warehouse.Stock.GetCount(1), "All original assets should still be in stock.");
        // test 6: Trade with null parameters should fail
        warehouse.Stock.Clear();
        success = manager.TradeAssetsForOtherAssets(null!, new List<Asset>(), 1);
        Assert.IsFalse(success, "Trade should fail with null stock.");
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, null!, 1);
        Assert.IsFalse(success, "Trade should fail with null asset list.");
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, new List<Asset>(), 1);
        Assert.IsFalse(success, "Trade should fail with empty asset list.");
        // test 7: Trade with invalid trade factor should fail
        warehouse.Stock.Clear();
        var testAssets = TestUtils.CreateAssets(1, 2, position, ownerId);
        warehouse.Stock.AddRange(testAssets);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, testAssets, 2, tradeFactor: 0);
        Assert.IsFalse(success, "Trade should fail with zero trade factor.");
        Assert.AreEqual(2, warehouse.Stock.Assets.Count, "Stock should remain unchanged.");
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, testAssets, 2, tradeFactor: -1);
        Assert.IsFalse(success, "Trade should fail with negative trade factor.");
        Assert.AreEqual(2, warehouse.Stock.Assets.Count, "Stock should remain unchanged.");
        // test 8: Trade respects stock limit
        warehouse.Stock.Clear();
        var manager2 = new EconomyManager(TestUtils.GenerateFactoryTypes());
        success = manager2.CreateWarehouse(new CubeCoordinates(1, 0, -1), ownerId, 5); // Small stock limit
        var smallWarehouse = manager2.GetWarehouseByPosition(new CubeCoordinates(1, 0, -1));
        Assert.IsNotNull(smallWarehouse, "Small warehouse should exist.");
        var manyAssets = TestUtils.CreateAssets(1, 4, new CubeCoordinates(1, 0, -1), ownerId);
        smallWarehouse.Stock.AddRange(manyAssets);
        manager2.ProcessFactories();
        // try to trade 4 assets (2:1) which would create 2 new assets, but stock only has room for 1 more
        success = manager2.TradeAssetsForOtherAssets(smallWarehouse.Stock, manyAssets.Take(2).ToList(), 2, tradeFactor: 1);
        Assert.IsTrue(success, "Trade should succeed when within stock limit.");
        Assert.AreEqual(4, smallWarehouse.Stock.Assets.Count, "Stock should have correct count.");
        // now stock is at 4/5, try to trade remaining 2 for 2 other
        var remainingAssets = smallWarehouse.Stock.Assets.Where(a => a.Type == 1).ToList();
        success = manager2.TradeAssetsForOtherAssets(smallWarehouse.Stock, remainingAssets, 3, tradeFactor: 1);
        Assert.IsTrue(success, "Trade should succeed when within stock limit.");
        Assert.AreEqual(4, smallWarehouse.Stock.Assets.Count, "Stock should have correct count.");
        // test 9: Large trade factor (10:1)
        warehouse.Stock.Clear();
        var bulkAssets = TestUtils.CreateAssets(1, 20, position, ownerId);
        warehouse.Stock.AddRange(bulkAssets);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, bulkAssets, 2, tradeFactor: 10);
        Assert.IsTrue(success, "Trade should succeed with 10:1 ratio.");
        Assert.AreEqual(2, warehouse.Stock.Assets.Count, "Stock should have 2 assets after 10:1 trade.");
        Assert.AreEqual(0, warehouse.Stock.GetCount(1), "Stock should have no original assets.");
        Assert.AreEqual(2, warehouse.Stock.GetCount(2), "Stock should have 2 new assets.");
        // test 10: Partial trade (5 assets with 2:1 should create 2 new assets, 1 asset remainder is lost)
        warehouse.Stock.Clear();
        var partialAssets = TestUtils.CreateAssets(1, 5, position, ownerId);
        warehouse.Stock.AddRange(partialAssets);
        manager.ProcessFactories();
        success = manager.TradeAssetsForOtherAssets(warehouse.Stock, partialAssets, 2, tradeFactor: 2);
        Assert.IsTrue(success, "Trade should succeed even with remainder.");
        Assert.AreEqual(2, warehouse.Stock.Assets.Count, "Stock should have 2 new assets (5/2 = 2).");
        Assert.AreEqual(2, warehouse.Stock.GetCount(2), "All new assets should be of the traded type.");
    }
}
