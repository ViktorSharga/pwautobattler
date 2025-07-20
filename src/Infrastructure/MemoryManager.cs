using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace GameAutomation.Infrastructure
{
    /// <summary>
    /// Manages memory usage and automatic cleanup for the application
    /// </summary>
    public class MemoryManager : IDisposable
    {
        private readonly Timer _cleanupTimer;
        private readonly List<WeakReference> _references;
        private readonly object _lock = new object();
        private bool _disposed = false;
        private long _lastCleanupTime = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Memory statistics
        /// </summary>
        public struct MemoryStats
        {
            public long WorkingSetBytes { get; init; }
            public long PrivateMemoryBytes { get; init; }
            public long ManagedMemoryBytes { get; init; }
            public int Gen0Collections { get; init; }
            public int Gen1Collections { get; init; }
            public int Gen2Collections { get; init; }
            public int TotalObjects { get; init; }
            public DateTime Timestamp { get; init; }
        }

        public MemoryManager(TimeSpan? cleanupInterval = null)
        {
            _references = new List<WeakReference>();
            
            var interval = cleanupInterval ?? TimeSpan.FromMinutes(5);
            _cleanupTimer = new Timer(OnCleanupTimer, null, interval, interval);
        }

        /// <summary>
        /// Schedules an object for cleanup when it becomes eligible for garbage collection
        /// </summary>
        /// <typeparam name="T">Type of object to track</typeparam>
        /// <param name="target">The object to track</param>
        public void ScheduleCleanup<T>(T target) where T : class
        {
            if (target == null || _disposed)
                return;

            lock (_lock)
            {
                _references.Add(new WeakReference(target));
            }
        }

        /// <summary>
        /// Schedules an object for cleanup with a custom cleanup action
        /// </summary>
        /// <typeparam name="T">Type of object to track</typeparam>
        /// <param name="target">The object to track</param>
        /// <param name="cleanupAction">Action to execute when object is collected</param>
        public void ScheduleCleanup<T>(T target, Action<T> cleanupAction) where T : class
        {
            if (target == null || cleanupAction == null || _disposed)
                return;

            lock (_lock)
            {
                _references.Add(new CleanupWeakReference<T>(target, cleanupAction));
            }
        }

        /// <summary>
        /// Forces immediate garbage collection and cleanup
        /// </summary>
        public void ForceCleanup()
        {
            if (_disposed)
                return;

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Clean up dead references
            CleanupDeadReferences();
        }

        /// <summary>
        /// Gets current memory statistics
        /// </summary>
        /// <returns>Memory usage statistics</returns>
        public MemoryStats GetMemoryStats()
        {
            var process = Process.GetCurrentProcess();
            
            return new MemoryStats
            {
                WorkingSetBytes = process.WorkingSet64,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                ManagedMemoryBytes = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalObjects = GetTotalObjectCount(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Triggers low-latency garbage collection mode
        /// </summary>
        public void EnableLowLatencyMode()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        }

        /// <summary>
        /// Restores normal garbage collection mode
        /// </summary>
        public void RestoreNormalMode()
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
        }

        /// <summary>
        /// Checks if memory usage is above the specified threshold
        /// </summary>
        /// <param name="thresholdMB">Threshold in megabytes</param>
        /// <returns>True if memory usage is above threshold</returns>
        public bool IsMemoryUsageHigh(int thresholdMB = 500)
        {
            var stats = GetMemoryStats();
            var usageMB = stats.WorkingSetBytes / (1024 * 1024);
            return usageMB > thresholdMB;
        }

        /// <summary>
        /// Optimizes memory usage by forcing collection and compaction
        /// </summary>
        public async Task OptimizeMemoryAsync()
        {
            await Task.Run(() =>
            {
                EnableLowLatencyMode();
                try
                {
                    ForceCleanup();
                    
                    // Compact large object heap
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                }
                finally
                {
                    RestoreNormalMode();
                }
            });
        }

        private void OnCleanupTimer(object? state)
        {
            try
            {
                CleanupDeadReferences();
                
                // If memory usage is high, trigger optimization
                if (IsMemoryUsageHigh())
                {
                    _ = Task.Run(OptimizeMemoryAsync);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Debug.WriteLine($"MemoryManager cleanup error: {ex.Message}");
            }
        }

        private void CleanupDeadReferences()
        {
            lock (_lock)
            {
                for (int i = _references.Count - 1; i >= 0; i--)
                {
                    var reference = _references[i];
                    
                    if (!reference.IsAlive)
                    {
                        // Execute cleanup action if it's a CleanupWeakReference
                        if (reference is ICleanupWeakReference cleanup)
                        {
                            try
                            {
                                cleanup.ExecuteCleanup();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Cleanup action failed: {ex.Message}");
                            }
                        }
                        
                        _references.RemoveAt(i);
                    }
                }
            }
            
            _lastCleanupTime = DateTime.UtcNow.Ticks;
        }

        private int GetTotalObjectCount()
        {
            lock (_lock)
            {
                return _references.Count(r => r.IsAlive);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                
                lock (_lock)
                {
                    _references.Clear();
                }
                
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Interface for weak references with cleanup actions
    /// </summary>
    internal interface ICleanupWeakReference
    {
        void ExecuteCleanup();
    }

    /// <summary>
    /// Weak reference that executes a cleanup action when the target is collected
    /// </summary>
    /// <typeparam name="T">Type of the target object</typeparam>
    internal class CleanupWeakReference<T> : WeakReference, ICleanupWeakReference where T : class
    {
        private readonly Action<T> _cleanupAction;
        private bool _cleanupExecuted = false;

        public CleanupWeakReference(T target, Action<T> cleanupAction) : base(target)
        {
            _cleanupAction = cleanupAction ?? throw new ArgumentNullException(nameof(cleanupAction));
        }

        public void ExecuteCleanup()
        {
            if (_cleanupExecuted)
                return;

            var target = Target as T;
            if (target != null)
            {
                _cleanupAction(target);
            }
            
            _cleanupExecuted = true;
        }
    }
}