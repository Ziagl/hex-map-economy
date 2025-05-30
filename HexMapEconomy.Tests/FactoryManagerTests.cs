using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;

namespace HexMapEconomy.Tests;

[TestClass]
public sealed class FactoryManagerTests
{
    [TestMethod]
    public void CreateFactory()
    {
        var factoryManager = new FactoryManager(GenerateFactoryTypes());
        var position = new CubeCoordinates(0, 0, 0);
        int type = 1;
        int ownerId = 123;
        bool success = factoryManager.CreateFactory(position, type, ownerId);
        Assert.IsTrue(success, "Factory should be created successfully.");
        success = factoryManager.CreateFactory(position, 99, ownerId);
        Assert.IsFalse(success, "Factory should not be created with an unknown type.");
    }

    private Dictionary<int, Recipe> GenerateFactoryTypes()
    {
        return new Dictionary<int, Recipe>
        {
            { 1, new Recipe(
                new List<Tuple<int, int>>(), 
                new List<Tuple<int, int>>(){ new Tuple<int, int>(1, 1) }
                ) },    // lumberjack creates wood (abstract)
            { 2, new Recipe(
                new List<Tuple<int, int>>() { new Tuple<int, int>(1, 1) }, 
                new List<Tuple<int, int>>() { new Tuple<int, int>(2, 1) }
                ) },    // sawmill creates a plank from a wood (abstract)
        };
    }
}
