using System;
using System.Diagnostics;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.GE;

public static class Test_StructHeapRaw
{
    [Test]
    public static void Test_StructHeapRaw_increase_entity_capacity()
    {
        var store       = new RawEntityStore();
        var arch1       = store.GetArchetype(Signature.Get<Position>());
        int count       = 2000;
        var ids         = new int[count];
        for (int n = 0; n < count; n++)
        {
            var id  = store.CreateEntity(arch1);
            ids[n]  = id;
            AreSame(arch1,              store.GetEntityArchetype(id));
            AreEqual(n + 1,             arch1.EntityCount);
            AreEqual(new Position(),    store.EntityComponentRef<Position>(id)); // Position is present & default
            store.EntityComponentRef<Position>(id).x = n;
        }
        AreEqual(2048, arch1.Capacity);
        for (int n = 0; n < count; n++) {
            AreEqual(n, store.EntityComponentRef<Position>(ids[n]).x);
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_shrink_entity_capacity()
    {
        var store       = new RawEntityStore();
        var arch1       = store.GetArchetype(Signature.Get<Position>());
        int count       = 2000;
        var ids         = new int[count];
        for (int n = 0; n < count; n++)
        {
            var id = store.CreateEntity(arch1);
            ids[n] = id;
            store.EntityComponentRef<Position>(id).x = n;
        }
        return;
        // --- delete majority of entities
        const int remaining = 500;
        for (int n = remaining; n < count; n++) {
            // store.DeleteEntity(ids[n]); // todo implement DeleteEntity
            AreEqual(count + remaining - n - 1, arch1.EntityCount);
        }
        AreEqual(1024, arch1.Capacity);
        for (int n = 0; n < remaining; n++) {
            AreEqual(n, store.EntityComponentRef<Position>(ids[n]).x);
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_CreateEntity_Perf()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<Position>());
        _ = store.CreateEntity(arch1); // warmup
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        int count       = 10; // 10_000_000 ~ 1424 ms
        for (int n = 0; n < count; n++) {
            _ = store.CreateEntity(arch1);
        }
        Console.WriteLine($"CreateEntity() - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        AreEqual(count + 1, arch1.EntityCount);
    }
    
    // [Test]
    public static void Test_StructHeapRaw_Query_Perf()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<Position, Rotation>());
        int count   = 10; //   CreateEntity() 10_000_000 ~ ??? ms      Query() foreach: 10_000_000 ~ ??? ms
        var query   = store.Query(Signature.Get<Position, Rotation>());
        foreach (var (position, rotation) in query) { } // force one time allocation
        {
            _ = store.CreateEntity(arch1); // warmup
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int n = 0; n < count; n++) {
                _ = store.CreateEntity(arch1);
            }
            Console.WriteLine($"CreateEntity() - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
            AreEqual(count + 1, arch1.EntityCount);
        }
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int n = 0;
            var memStart = Mem.GetAllocatedBytes();
            foreach (var (position, rotation) in query) {
                n++;
            }
            Mem.AssertNoAlloc(memStart);
            AreEqual(count, n);
            Console.WriteLine($"Query() foreach - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_invalid_store()
    {
        var store1      = new RawEntityStore();
        var store2      = new RawEntityStore();
        var arch1       = store1.GetArchetype(Signature.Get<Position>());
        var e = Throws<ArgumentException>(() => {
            store2.CreateEntity(arch1);
        });
        AreEqual("entity is owned by a different store (Parameter 'archetype')", e!.Message);
    }
}

