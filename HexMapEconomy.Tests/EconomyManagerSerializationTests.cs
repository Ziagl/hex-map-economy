using com.hexagonsimulations.HexMapBase.Models;
using com.hexagonsimulations.HexMapEconomy.Models;
using System.Collections;
using System.Reflection;

namespace com.hexagonsimulations.HexMapEconomy.Tests;

[TestClass]
public sealed class EconomyManagerSerializationTests
{
    [TestMethod]
    public void SerializationDeserializationJson()
    {
        var economyManager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        PrepareEconomyManager(economyManager);
        var json = economyManager.ToJson();
        Assert.IsFalse(string.IsNullOrWhiteSpace(json), "JSON should not be empty.");

        // dump this map as JSON to disk
        var filePath = @"C:\Temp\EconomyManager.json";
        File.WriteAllText(filePath, json);

        var roundTripped = EconomyManager.FromJson(json);
        Assert.IsNotNull(roundTripped, "Deserialized CityManager should not be null.");

        AssertEconomyManagerEqual(economyManager, roundTripped);
    }

    /*[TestMethod]
    public void SerializationDeserializationBinary()
    {
        var economyManager = new EconomyManager(TestUtils.GenerateFactoryTypes());
        PrepareEconomyManager(economyManager);
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            economyManager.Write(writer);
        }

        // dump this map as BIN to disk
        //var filePath = @"C:\Temp\EconomyManager.bin";
        //File.WriteAllBytes(filePath, ms.ToArray());

        ms.Position = 0;
        EconomyManager roundTripped;
        using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            roundTripped = EconomyManager.Read(reader);
        }

        Assert.IsNotNull(roundTripped, "Binary deserialized EconomyManager should not be null.");
        AssertEconomyManagerEqual(economyManager, roundTripped);
    }*/

    private void PrepareEconomyManager(EconomyManager manager)
    {
        // warehouse
        var position = new CubeCoordinates(0, 0, 0);
        int ownerId = 1;
        bool success = manager.CreateWarehouse(position, ownerId, 10);
        Assert.IsTrue(success, "Warehouse should be created successfully.");
        var warehouse = manager.GetWarehouseByPosition(position);
        Assert.IsNotNull(warehouse, "Warehouse should be found by position.");
        // factories
        position = new CubeCoordinates(1, 1, -2);
        success = manager.CreateFactory(
            position,
            TestUtils.SAWMILL,
            ownerId,
            warehouse);
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
        // add a second factory
        position = new CubeCoordinates(1, 2, -3);
        success = manager.CreateFactory(
            position,
            TestUtils.SAWMILL,
            ownerId,
            warehouse);
        manager.ProcessFactories(); // process factory to make assets available
    }

    private static void AssertEconomyManagerEqual(EconomyManager expected, EconomyManager actual)
    {
        Assert.IsNotNull(expected, "Expected EconomyManager is null");
        Assert.IsNotNull(actual, "Actual EconomyManager is null");

        // ---- Helper local functions ----
        static T GetField<T>(object instance, string name)
        {
            var f = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field '{name}' not found on type {instance.GetType().Name}");
            var v = f!.GetValue(instance);
            Assert.IsNotNull(v, $"Field '{name}' on {instance.GetType().Name} is null");
            return (T)v!;
        }

        static object? TryGetField(object instance, string name)
        {
            var f = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return f?.GetValue(instance);
        }

        static void AssertRecipesEqual(Dictionary<int, Recipe> a, Dictionary<int, Recipe> b)
        {
            Assert.AreEqual(a.Count, b.Count, "Recipe count mismatch");
            foreach (var (key, ra) in a)
            {
                Assert.IsTrue(b.ContainsKey(key), $"Recipe key {key} missing in actual");
                var rb = b[key];
                Assert.AreEqual(ra.Duration, rb.Duration, $"Recipe {key} duration mismatch");
                static List<(int t, int amt)> Normalize(List<RecipeIngredient>? list) =>
                    list?.Select(i => (i.Type, i.Amount)).OrderBy(x => x.Type).ThenBy(x => x.Amount).ToList()
                    ?? new List<(int t, int amt)>();
                var aIn = Normalize(ra.Inputs);
                var bIn = Normalize(rb.Inputs);
                CollectionAssert.AreEqual(aIn, bIn, $"Recipe {key} inputs mismatch");
                var aOut = Normalize(ra.Outputs);
                var bOut = Normalize(rb.Outputs);
                CollectionAssert.AreEqual(aOut, bOut, $"Recipe {key} outputs mismatch");
            }
        }

        static void AssertCubeEqual(object a, object b, string ctx)
        {
            Assert.AreEqual(a, b, $"CubeCoordinates mismatch for {ctx}");
        }

        static void AssertFactoryEqual(Factory fa, Factory fb)
        {
            Assert.AreEqual(fa.GetType(), fb.GetType(), "Factory type mismatch (CLR)");
            // Public / exposed data
            var idA = (Guid)fa.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fa)!;
            var idB = (Guid)fb.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb)!;
            Assert.AreEqual(idA, idB, "Factory Id mismatch");

            var typeA = (int)fa.GetType().GetProperty("Type", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fa)!;
            var typeB = (int)fb.GetType().GetProperty("Type", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb)!;
            Assert.AreEqual(typeA, typeB, $"Factory {idA} Type mismatch");

            var ownerA = (int)fa.GetType().GetProperty("OwnerId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fa)!;
            var ownerB = (int)fb.GetType().GetProperty("OwnerId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fb)!;
            Assert.AreEqual(ownerA, ownerB, $"Factory {idA} OwnerId mismatch");

            var posA = fa.GetType().GetProperty("Position")?.GetValue(fa)!;
            var posB = fb.GetType().GetProperty("Position")?.GetValue(fb)!;
            AssertCubeEqual(posA, posB, $"Factory {idA}");

            var recipeA = (Recipe)fa.GetType().GetProperty("Recipe")!.GetValue(fa)!;
            var recipeB = (Recipe)fb.GetType().GetProperty("Recipe")!.GetValue(fb)!;
            Assert.AreEqual(recipeA.Duration, recipeB.Duration, $"Factory {idA} Recipe duration mismatch");
            // Ingredients already validated at manager-level; shallow check here
            var whA = (Guid)fa.GetType().GetProperty("WarehouseId")!.GetValue(fa)!;
            var whB = (Guid)fb.GetType().GetProperty("WarehouseId")!.GetValue(fb)!;
            Assert.AreEqual(whA, whB, $"Factory {idA} WarehouseId mismatch");

            // Productivity (public field)
            var prodField = fa.GetType().GetField("Productivity", BindingFlags.Public | BindingFlags.Instance);
            if (prodField != null)
            {
                var prodA = prodField.GetValue(fa);
                var prodB = prodField.GetValue(fb);
                Assert.AreEqual(prodA, prodB, $"Factory {idA} Productivity mismatch");
            }

            // LastTenOutputs
            var ltoA = (IEnumerable<int>)fa.GetType().GetProperty("LastTenOutputs")!.GetValue(fa)!;
            var ltoB = (IEnumerable<int>)fb.GetType().GetProperty("LastTenOutputs")!.GetValue(fb)!;
            CollectionAssert.AreEqual(ltoA.ToList(), ltoB.ToList(), $"Factory {idA} LastTenOutputs mismatch");
        }

        static void AssertStockEqual(object stockA, object stockB, string ctx)
        {
            if (stockA is null && stockB is null) return;
            Assert.IsNotNull(stockA, $"{ctx} Stock A null");
            Assert.IsNotNull(stockB, $"{ctx} Stock B null");

            // Try obvious public members first
            var dictA = stockA.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.GetValue(stockA))
                .OfType<IDictionary>()
                .FirstOrDefault(d => d.GetType().GetGenericArguments().Length == 2 &&
                                     d.GetType().GetGenericArguments()[0] == typeof(int) &&
                                     d.GetType().GetGenericArguments()[1] == typeof(int));
            var dictB = stockB.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.GetValue(stockB))
                .OfType<IDictionary>()
                .FirstOrDefault(d => d.GetType().GetGenericArguments().Length == 2 &&
                                     d.GetType().GetGenericArguments()[0] == typeof(int) &&
                                     d.GetType().GetGenericArguments()[1] == typeof(int));

            // If not found via fields, try properties
            dictA ??= stockA.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(p => p.GetValue(stockA))
                .OfType<IDictionary>()
                .FirstOrDefault(d => d.GetType().GetGenericArguments().Length == 2 &&
                                     d.GetType().GetGenericArguments()[0] == typeof(int) &&
                                     d.GetType().GetGenericArguments()[1] == typeof(int));
            dictB ??= stockB.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(p => p.GetValue(stockB))
                .OfType<IDictionary>()
                .FirstOrDefault(d => d.GetType().GetGenericArguments().Length == 2 &&
                                     d.GetType().GetGenericArguments()[0] == typeof(int) &&
                                     d.GetType().GetGenericArguments()[1] == typeof(int));

            if (dictA is null || dictB is null)
            {
                // Fallback: we cannot assert deeper
                return;
            }

            Assert.AreEqual(dictA.Count, dictB.Count, $"{ctx} Stock item count mismatch");
            foreach (DictionaryEntry entry in dictA)
            {
                Assert.IsTrue(dictB.Contains(entry.Key), $"{ctx} Stock missing key {entry.Key}");
                Assert.AreEqual(entry.Value, dictB[entry.Key], $"{ctx} Stock amount mismatch for key {entry.Key}");
            }
        }

        static void AssertDemandsEqual(object warehouseA, object warehouseB, string ctx)
        {
            var demandsAObj = warehouseA.GetType().GetField("Demands", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(warehouseA)
                              ?? warehouseA.GetType().GetProperty("Demands", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(warehouseA);
            var demandsBObj = warehouseB.GetType().GetField("Demands", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(warehouseB)
                              ?? warehouseB.GetType().GetProperty("Demands", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(warehouseB);

            if (demandsAObj is System.Collections.IEnumerable ea && demandsBObj is System.Collections.IEnumerable eb)
            {
                var listA = ea.Cast<object>().ToList();
                var listB = eb.Cast<object>().ToList();
                Assert.AreEqual(listA.Count, listB.Count, $"{ctx} Demand count mismatch");
                // If Demand exposes Ingredient or Type/Amount we can try to compare shallowly
                for (int i = 0; i < listA.Count; i++)
                {
                    var dA = listA[i];
                    var dB = listB[i];
                    // Compare simple known fields if exist
                    var typePropA = dA.GetType().GetProperty("Type");
                    var typePropB = dB.GetType().GetProperty("Type");
                    if (typePropA != null && typePropB != null)
                    {
                        Assert.AreEqual(typePropA.GetValue(dA), typePropB.GetValue(dB), $"{ctx} Demand {i} Type mismatch");
                    }
                    var amountPropA = dA.GetType().GetProperty("Amount");
                    var amountPropB = dB.GetType().GetProperty("Amount");
                    if (amountPropA != null && amountPropB != null)
                    {
                        Assert.AreEqual(amountPropA.GetValue(dA), amountPropB.GetValue(dB), $"{ctx} Demand {i} Amount mismatch");
                    }
                }
            }
        }

        static void AssertWarehouseEqual(Warehouse wa, Warehouse wb)
        {
            var idA = (Guid)wa.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(wa)!;
            var idB = (Guid)wb.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(wb)!;
            Assert.AreEqual(idA, idB, "Warehouse Id mismatch");

            var ownerA = (int)wa.GetType().GetProperty("OwnerId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(wa)!;
            var ownerB = (int)wb.GetType().GetProperty("OwnerId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(wb)!;
            Assert.AreEqual(ownerA, ownerB, $"Warehouse {idA} OwnerId mismatch");

            var posA = wa.GetType().GetProperty("Position")!.GetValue(wa)!;
            var posB = wb.GetType().GetProperty("Position")!.GetValue(wb)!;
            AssertCubeEqual(posA, posB, $"Warehouse {idA}");

            var stockLimitA = (int)wa.GetType().GetProperty("StockLimit")!.GetValue(wa)!;
            var stockLimitB = (int)wb.GetType().GetProperty("StockLimit")!.GetValue(wb)!;
            Assert.AreEqual(stockLimitA, stockLimitB, $"Warehouse {idA} StockLimit mismatch");

            var stockA = wa.GetType().GetProperty("Stock")?.GetValue(wa);
            var stockB = wb.GetType().GetProperty("Stock")?.GetValue(wb);
            AssertStockEqual(stockA!, stockB!, $"Warehouse {idA}");

            AssertDemandsEqual(wa, wb, $"Warehouse {idA}");
        }

        // ---- Compare TransportationPerTurn (public property or private backing) ----
        var tptProp = expected.GetType().GetProperty("TransportationPerTurn");
        if (tptProp != null)
        {
            var expTpt = tptProp.GetValue(expected);
            var actTpt = tptProp.GetValue(actual);
            Assert.AreEqual(expTpt, actTpt, "TransportationPerTurn mismatch");
        }
        else
        {
            // fallback to private field
            var fExp = TryGetField(expected, "_transportationPerTurn");
            var fAct = TryGetField(actual, "_transportationPerTurn");
            Assert.AreEqual(fExp, fAct, "_transportationPerTurn mismatch");
        }

        // ---- Compare Recipes ----
        Dictionary<int, Recipe>? recipesExpected = null;
        Dictionary<int, Recipe>? recipesActual = null;

        var recipesProp = expected.GetType().GetProperty("Recipes");
        if (recipesProp?.GetValue(expected) is Dictionary<int, Recipe> rp1 &&
            recipesProp.GetValue(actual) is Dictionary<int, Recipe> rp2)
        {
            recipesExpected = rp1;
            recipesActual = rp2;
        }
        else
        {
            recipesExpected = GetField<Dictionary<int, Recipe>>(expected, "_recipeStore");
            recipesActual = GetField<Dictionary<int, Recipe>>(actual, "_recipeStore");
        }
        AssertRecipesEqual(recipesExpected!, recipesActual!);

        // ---- Compare Warehouses ----
        var warehousesExpected = GetField<Dictionary<Guid, Warehouse>>(expected, "_warehouseStore");
        var warehousesActual = GetField<Dictionary<Guid, Warehouse>>(actual, "_warehouseStore");
        Assert.AreEqual(warehousesExpected.Count, warehousesActual.Count, "Warehouse count mismatch");
        foreach (var (id, whExp) in warehousesExpected)
        {
            Assert.IsTrue(warehousesActual.ContainsKey(id), $"Warehouse {id} missing in actual");
            var whAct = warehousesActual[id];
            AssertWarehouseEqual(whExp, whAct);
        }

        // ---- Compare Factories ----
        var factoriesExpected = GetField<Dictionary<Guid, Factory>>(expected, "_factoryStore");
        var factoriesActual = GetField<Dictionary<Guid, Factory>>(actual, "_factoryStore");
        Assert.AreEqual(factoriesExpected.Count, factoriesActual.Count, "Factory count mismatch");
        foreach (var (id, fExp) in factoriesExpected)
        {
            Assert.IsTrue(factoriesActual.ContainsKey(id), $"Factory {id} missing in actual");
            var fAct = factoriesActual[id];
            AssertFactoryEqual(fExp, fAct);
        }
    }
}
