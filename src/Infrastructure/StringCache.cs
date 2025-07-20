using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GameAutomation.Infrastructure
{
    /// <summary>
    /// String interning and caching utility to reduce memory allocations
    /// </summary>
    public class StringCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, string> _internedStrings;
        private readonly int _maxCacheSize;
        private bool _disposed = false;

        public StringCache(int maxCacheSize = 1000)
        {
            _maxCacheSize = maxCacheSize > 0 ? maxCacheSize : throw new ArgumentOutOfRangeException(nameof(maxCacheSize));
            _internedStrings = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Gets an interned version of the string to reduce memory usage
        /// </summary>
        /// <param name="value">The string to intern</param>
        /// <returns>An interned string instance</returns>
        public string Intern(string value)
        {
            if (string.IsNullOrEmpty(value) || _disposed)
                return value;

            // Use GetOrAdd to ensure we only store one instance
            return _internedStrings.GetOrAdd(value, s =>
            {
                // If cache is getting too large, clear it periodically
                if (_internedStrings.Count > _maxCacheSize)
                {
                    Clear();
                }
                
                return string.Intern(s);
            });
        }

        /// <summary>
        /// Creates a formatted string key efficiently
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Arguments</param>
        /// <returns>Cached formatted string</returns>
        public string Format(string format, params object[] args)
        {
            if (_disposed)
                return string.Format(format, args);

            var key = string.Format(format, args);
            return Intern(key);
        }

        /// <summary>
        /// Combines strings efficiently for use as dictionary keys
        /// </summary>
        /// <param name="parts">String parts to combine</param>
        /// <returns>Combined and interned string</returns>
        public string Combine(params string[] parts)
        {
            if (parts == null || parts.Length == 0 || _disposed)
                return string.Empty;

            if (parts.Length == 1)
                return Intern(parts[0]);

            var combined = string.Join(":", parts);
            return Intern(combined);
        }

        /// <summary>
        /// Creates a key for cooldown tracking
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="spellId">Spell identifier</param>
        /// <returns>Cached key string</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateCooldownKey(IntPtr windowHandle, string spellId)
        {
            if (_disposed)
                return $"{windowHandle}:{spellId}";

            return Format("{0}:{1}", windowHandle, spellId);
        }

        /// <summary>
        /// Gets the current cache size
        /// </summary>
        public int CacheSize => _internedStrings.Count;

        /// <summary>
        /// Gets the maximum cache size
        /// </summary>
        public int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Clears the string cache
        /// </summary>
        public void Clear()
        {
            if (!_disposed)
            {
                _internedStrings.Clear();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Thread-safe string builder pool for efficient string operations
    /// </summary>
    public class StringBuilderPool : IDisposable
    {
        private readonly ObjectPool<System.Text.StringBuilder> _pool;
        private bool _disposed = false;

        public StringBuilderPool(int maxPoolSize = 50)
        {
            _pool = new ObjectPool<System.Text.StringBuilder>(
                objectGenerator: () => new System.Text.StringBuilder(256),
                resetAction: sb => sb.Clear(),
                maxSize: maxPoolSize
            );
        }

        /// <summary>
        /// Gets a StringBuilder from the pool
        /// </summary>
        /// <returns>A pooled StringBuilder wrapped in a disposable container</returns>
        public PooledObject<System.Text.StringBuilder> Get()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StringBuilderPool));

            return _pool.GetPooled();
        }

        /// <summary>
        /// Builds a string efficiently using a pooled StringBuilder
        /// </summary>
        /// <param name="buildAction">Action to build the string</param>
        /// <returns>The built string</returns>
        public string Build(Action<System.Text.StringBuilder> buildAction)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StringBuilderPool));

            using var pooled = Get();
            buildAction(pooled.Object);
            return pooled.Object.ToString();
        }

        /// <summary>
        /// Gets the current pool size
        /// </summary>
        public int PoolSize => _pool.Count;

        public void Dispose()
        {
            if (!_disposed)
            {
                _pool.Dispose();
                _disposed = true;
            }
        }
    }
}