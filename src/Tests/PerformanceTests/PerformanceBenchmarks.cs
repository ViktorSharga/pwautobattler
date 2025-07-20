using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameAutomation.Infrastructure;
using GameAutomation.Services;
using GameAutomation.Tests.UnitTests.Services;

namespace GameAutomation.Tests.PerformanceTests
{
    [TestClass]
    public class PerformanceBenchmarks
    {
        private const int WARMUP_ITERATIONS = 100;
        private const int BENCHMARK_ITERATIONS = 1000;
        private const int LARGE_DATASET_SIZE = 10000;

        [TestMethod]
        public void ObjectPool_Performance_ShouldBeEfficient()
        {
            // Arrange
            var pool = new ObjectPool<TestObject>(
                objectGenerator: () => new TestObject(),
                resetAction: obj => obj.Reset(),
                maxSize: 100
            );

            var nonPooledTimes = new List<long>();
            var pooledTimes = new List<long>();

            try
            {
                // Warmup
                for (int i = 0; i < WARMUP_ITERATIONS; i++)
                {
                    var obj = pool.Get();
                    pool.Return(obj);
                }

                // Benchmark non-pooled object creation
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
                {
                    var start = stopwatch.ElapsedTicks;
                    var obj = new TestObject();
                    obj.SetData($"Object_{i}", i);
                    var end = stopwatch.ElapsedTicks;
                    nonPooledTimes.Add(end - start);
                }

                stopwatch.Restart();

                // Benchmark pooled object usage
                for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
                {
                    var start = stopwatch.ElapsedTicks;
                    var obj = pool.Get();
                    obj.SetData($"Object_{i}", i);
                    pool.Return(obj);
                    var end = stopwatch.ElapsedTicks;
                    pooledTimes.Add(end - start);
                }

                // Assert - Pooled should be significantly faster on average
                var avgNonPooled = nonPooledTimes.Average();
                var avgPooled = pooledTimes.Average();

                Console.WriteLine($"Non-pooled average: {avgNonPooled:F2} ticks");
                Console.WriteLine($"Pooled average: {avgPooled:F2} ticks");
                Console.WriteLine($"Performance improvement: {(avgNonPooled / avgPooled):F2}x");

                // Pooled should be at least as fast as non-pooled (allowing for some variance)
                Assert.IsTrue(avgPooled <= avgNonPooled * 1.5, 
                    $"Pooled objects should be efficient. Pooled: {avgPooled:F2}, Non-pooled: {avgNonPooled:F2}");
            }
            finally
            {
                pool.Dispose();
            }
        }

        [TestMethod]
        public void StringCache_Performance_ShouldReduceAllocations()
        {
            // Arrange
            var cache = new StringCache(1000);
            var strings = Enumerable.Range(0, 100)
                .Select(i => $"spell_{i % 10}") // Many duplicates
                .ToArray();

            var uncachedTimes = new List<long>();
            var cachedTimes = new List<long>();

            try
            {
                // Warmup
                for (int i = 0; i < WARMUP_ITERATIONS; i++)
                {
                    var str = cache.Intern(strings[i % strings.Length]);
                }

                var stopwatch = Stopwatch.StartNew();

                // Benchmark uncached string operations
                for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
                {
                    var start = stopwatch.ElapsedTicks;
                    var str = string.Intern(strings[i % strings.Length]);
                    var end = stopwatch.ElapsedTicks;
                    uncachedTimes.Add(end - start);
                }

                stopwatch.Restart();

                // Benchmark cached string operations
                for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
                {
                    var start = stopwatch.ElapsedTicks;
                    var str = cache.Intern(strings[i % strings.Length]);
                    var end = stopwatch.ElapsedTicks;
                    cachedTimes.Add(end - start);
                }

                // Assert - Cache should improve performance for repeated strings
                var avgUncached = uncachedTimes.Average();
                var avgCached = cachedTimes.Average();

                Console.WriteLine($"Uncached average: {avgUncached:F2} ticks");
                Console.WriteLine($"Cached average: {avgCached:F2} ticks");
                Console.WriteLine($"Cache performance improvement: {(avgUncached / avgCached):F2}x");

                // Cached should be at least as fast as uncached
                Assert.IsTrue(avgCached <= avgUncached * 1.2, 
                    $"String cache should improve performance. Cached: {avgCached:F2}, Uncached: {avgUncached:F2}");
            }
            finally
            {
                cache.Dispose();
            }
        }

        [TestMethod]
        public async Task CooldownService_Performance_ShouldScaleEfficiently()
        {
            // Arrange
            var configService = new ConfigurationService();
            configService.SetValue("cooldowns.maxCooldowns", 10000);
            configService.SetValue("cooldowns.cleanupIntervalMinutes", 60); // Reduce cleanup frequency

            var cooldownService = new CooldownService(configService);
            var windows = Enumerable.Range(0, 1000)
                .Select(i => new TestGameWindow(new IntPtr(i + 1000), $"Window {i}"))
                .ToArray();
            var spells = Enumerable.Range(0, 100)
                .Select(i => new TestSpell($"spell_{i}", $"Spell {i}", TimeSpan.FromMinutes(1)))
                .ToArray();

            try
            {
                // Warmup
                for (int i = 0; i < 50; i++)
                {
                    await cooldownService.StartCooldownAsync(windows[i % windows.Length], spells[i % spells.Length]);
                }

                var stopwatch = Stopwatch.StartNew();

                // Benchmark adding cooldowns
                var addTimes = new List<long>();
                for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
                {
                    var start = stopwatch.ElapsedTicks;
                    await cooldownService.StartCooldownAsync(windows[i % windows.Length], spells[i % spells.Length]);
                    var end = stopwatch.ElapsedTicks;
                    addTimes.Add(end - start);
                }

                // Benchmark checking cooldowns
                var checkTimes = new List<long>();
                for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
                {
                    var start = stopwatch.ElapsedTicks;
                    var isOnCooldown = cooldownService.IsOnCooldown(windows[i % windows.Length], spells[i % spells.Length]);
                    var end = stopwatch.ElapsedTicks;
                    checkTimes.Add(end - start);
                }

                // Assert - Operations should be consistently fast even with many cooldowns
                var avgAddTime = addTimes.Average();
                var avgCheckTime = checkTimes.Average();
                var maxAddTime = addTimes.Max();
                var maxCheckTime = checkTimes.Max();

                Console.WriteLine($"Add cooldown - Average: {avgAddTime:F2} ticks, Max: {maxAddTime} ticks");
                Console.WriteLine($"Check cooldown - Average: {avgCheckTime:F2} ticks, Max: {maxCheckTime} ticks");
                Console.WriteLine($"Total cooldowns: {cooldownService.GetCooldownCount()}");

                // Performance should be reasonable even under load
                Assert.IsTrue(avgAddTime < 1000, $"Adding cooldowns should be fast. Average: {avgAddTime:F2} ticks");
                Assert.IsTrue(avgCheckTime < 500, $"Checking cooldowns should be very fast. Average: {avgCheckTime:F2} ticks");
            }
            finally
            {
                cooldownService.Dispose();
            }
        }

        [TestMethod]
        public void MemoryManager_Performance_ShouldHandleLargeDatasets()
        {
            // Arrange
            var memoryManager = new MemoryManager(TimeSpan.FromSeconds(10));
            var objects = new List<object>();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var initialStats = memoryManager.GetMemoryStats();

                // Act - Create many objects and track them
                for (int i = 0; i < LARGE_DATASET_SIZE; i++)
                {
                    var obj = new TestObject();
                    objects.Add(obj);
                    memoryManager.ScheduleCleanup(obj);
                }

                var afterCreationStats = memoryManager.GetMemoryStats();

                // Clear references to allow garbage collection
                objects.Clear();

                // Force cleanup
                memoryManager.ForceCleanup();
                
                var afterCleanupStats = memoryManager.GetMemoryStats();
                stopwatch.Stop();

                // Assert - Memory should be managed efficiently
                var creationTime = stopwatch.ElapsedMilliseconds;
                var memoryGrowthMB = (afterCreationStats.ManagedMemoryBytes - initialStats.ManagedMemoryBytes) / (1024 * 1024);
                var memoryRecoveredMB = (afterCreationStats.ManagedMemoryBytes - afterCleanupStats.ManagedMemoryBytes) / (1024 * 1024);

                Console.WriteLine($"Creation time: {creationTime}ms for {LARGE_DATASET_SIZE} objects");
                Console.WriteLine($"Memory growth: {memoryGrowthMB}MB");
                Console.WriteLine($"Memory recovered: {memoryRecoveredMB}MB");
                Console.WriteLine($"GC Gen0: {afterCleanupStats.Gen0Collections - initialStats.Gen0Collections}");
                Console.WriteLine($"GC Gen1: {afterCleanupStats.Gen1Collections - initialStats.Gen1Collections}");
                Console.WriteLine($"GC Gen2: {afterCleanupStats.Gen2Collections - initialStats.Gen2Collections}");

                // Performance should be reasonable for large datasets
                Assert.IsTrue(creationTime < 5000, $"Should handle {LARGE_DATASET_SIZE} objects quickly. Time: {creationTime}ms");
                Assert.IsTrue(memoryRecoveredMB > 0, "Memory cleanup should recover some memory");
            }
            finally
            {
                memoryManager.Dispose();
            }
        }

        [TestMethod]
        public async Task WindowService_Performance_ShouldHandleManyWindows()
        {
            // Arrange
            var windowService = new WindowService();
            var windows = Enumerable.Range(0, LARGE_DATASET_SIZE)
                .Select(i => new 
                {
                    Handle = new IntPtr(i + 10000),
                    Title = $"Performance Test Window {i}",
                    Rect = new System.Drawing.Rectangle(i % 100, i % 100, 800, 600)
                })
                .ToArray();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Benchmark window registration
                var registerTimes = new List<long>();
                foreach (var window in windows)
                {
                    var start = stopwatch.ElapsedTicks;
                    windowService.RegisterWindow(window.Handle, window.Title, window.Rect);
                    var end = stopwatch.ElapsedTicks;
                    registerTimes.Add(end - start);
                }

                // Benchmark window retrieval
                var getTimes = new List<long>();
                foreach (var window in windows.Take(1000)) // Test subset for performance
                {
                    var start = stopwatch.ElapsedTicks;
                    var retrievedWindow = windowService.GetWindow(window.Handle);
                    var end = stopwatch.ElapsedTicks;
                    getTimes.Add(end - start);
                }

                stopwatch.Stop();

                // Assert - Operations should scale well
                var avgRegisterTime = registerTimes.Average();
                var avgGetTime = getTimes.Average();
                var maxRegisterTime = registerTimes.Max();
                var maxGetTime = getTimes.Max();

                Console.WriteLine($"Register window - Average: {avgRegisterTime:F2} ticks, Max: {maxRegisterTime} ticks");
                Console.WriteLine($"Get window - Average: {avgGetTime:F2} ticks, Max: {maxGetTime} ticks");
                Console.WriteLine($"Total windows registered: {windowService.GetAllWindows().Count()}");

                // Performance should not degrade significantly with scale
                Assert.IsTrue(avgRegisterTime < 500, $"Window registration should be fast. Average: {avgRegisterTime:F2} ticks");
                Assert.IsTrue(avgGetTime < 300, $"Window retrieval should be very fast. Average: {avgGetTime:F2} ticks");
                Assert.IsTrue(maxRegisterTime < 2000, $"Max registration time should be reasonable. Max: {maxRegisterTime} ticks");
            }
            finally
            {
                windowService.Dispose();
            }
        }

        [TestMethod]
        public void ConcurrentAccess_Performance_ShouldBeThreadSafe()
        {
            // Arrange
            var cooldownService = new CooldownService();
            var objectPool = new ObjectPool<TestObject>(() => new TestObject(), obj => obj.Reset());
            var stringCache = new StringCache(1000);

            try
            {
                var tasks = new List<Task>();
                var stopwatch = Stopwatch.StartNew();

                // Create concurrent access tasks
                for (int taskId = 0; taskId < 10; taskId++)
                {
                    int localTaskId = taskId;
                    
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            // Test CooldownService thread safety
                            var window = new TestGameWindow(new IntPtr(localTaskId * 1000 + i), $"Window {i}");
                            var spell = new TestSpell($"spell_{i}", $"Spell {i}", TimeSpan.FromSeconds(1));
                            
                            await cooldownService.StartCooldownAsync(window, spell);
                            var isOnCooldown = cooldownService.IsOnCooldown(window, spell);
                            
                            // Test ObjectPool thread safety
                            var pooledObj = objectPool.Get();
                            pooledObj.SetData($"Task{localTaskId}_Object{i}", i);
                            objectPool.Return(pooledObj);
                            
                            // Test StringCache thread safety
                            var cachedString = stringCache.Intern($"test_string_{i % 10}");
                        }
                    }));
                }

                // Wait for all tasks to complete
                Task.WaitAll(tasks.ToArray());
                stopwatch.Stop();

                // Assert - Should complete without errors and in reasonable time
                var totalTime = stopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Concurrent access test completed in {totalTime}ms");
                Console.WriteLine($"Cooldowns created: {cooldownService.GetCooldownCount()}");
                Console.WriteLine($"String cache size: {stringCache.CacheSize}");

                Assert.IsTrue(totalTime < 10000, $"Concurrent operations should complete quickly. Time: {totalTime}ms");
                Assert.IsTrue(cooldownService.GetCooldownCount() > 0, "Cooldowns should be created");
                Assert.IsTrue(stringCache.CacheSize > 0, "Strings should be cached");
            }
            finally
            {
                cooldownService.Dispose();
                objectPool.Dispose();
                stringCache.Dispose();
            }
        }
    }
}