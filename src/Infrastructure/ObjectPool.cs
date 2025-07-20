using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GameAutomation.Infrastructure
{
    /// <summary>
    /// Thread-safe object pool to reduce garbage collection pressure
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public class ObjectPool<T> : IDisposable where T : class
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T>? _resetAction;
        private readonly Action<T>? _destroyAction;
        private readonly int _maxSize;
        private int _currentCount;
        private bool _disposed;

        /// <summary>
        /// Creates a new object pool
        /// </summary>
        /// <param name="objectGenerator">Function to create new objects</param>
        /// <param name="resetAction">Optional action to reset object state when returned</param>
        /// <param name="destroyAction">Optional action to cleanup object when pool is disposed</param>
        /// <param name="maxSize">Maximum number of objects to pool (default: 100)</param>
        public ObjectPool(Func<T> objectGenerator, Action<T>? resetAction = null, 
            Action<T>? destroyAction = null, int maxSize = 100)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _resetAction = resetAction;
            _destroyAction = destroyAction;
            _maxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize));
            _objects = new ConcurrentQueue<T>();
            _currentCount = 0;
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one
        /// </summary>
        /// <returns>An object instance</returns>
        public T Get()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObjectPool<T>));

            if (_objects.TryDequeue(out var item))
            {
                Interlocked.Decrement(ref _currentCount);
                return item;
            }

            return _objectGenerator();
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        /// <param name="item">The object to return</param>
        public void Return(T item)
        {
            if (_disposed || item == null)
                return;

            // Reset the object state if reset action is provided
            try
            {
                _resetAction?.Invoke(item);
            }
            catch
            {
                // If reset fails, don't return the object to pool
                return;
            }

            // Only add to pool if under max size
            if (_currentCount < _maxSize)
            {
                _objects.Enqueue(item);
                Interlocked.Increment(ref _currentCount);
            }
        }

        /// <summary>
        /// Gets the current number of objects in the pool
        /// </summary>
        public int Count => _currentCount;

        /// <summary>
        /// Gets the maximum pool size
        /// </summary>
        public int MaxSize => _maxSize;

        /// <summary>
        /// Clears all objects from the pool
        /// </summary>
        public void Clear()
        {
            while (_objects.TryDequeue(out var item))
            {
                Interlocked.Decrement(ref _currentCount);
                try
                {
                    _destroyAction?.Invoke(item);
                }
                catch
                {
                    // Ignore cleanup errors
                }
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
    /// Pooled object wrapper that automatically returns objects to pool when disposed
    /// </summary>
    /// <typeparam name="T">Type of pooled object</typeparam>
    public struct PooledObject<T> : IDisposable where T : class
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _object;
        private bool _returned;

        internal PooledObject(ObjectPool<T> pool, T @object)
        {
            _pool = pool;
            _object = @object;
            _returned = false;
        }

        /// <summary>
        /// Gets the pooled object
        /// </summary>
        public T Object => _object;

        /// <summary>
        /// Returns the object to the pool
        /// </summary>
        public void Dispose()
        {
            if (!_returned && _object != null)
            {
                _pool.Return(_object);
                _returned = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for easier object pool usage
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Gets a pooled object wrapped in a disposable container
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The object pool</param>
        /// <returns>A disposable wrapper that automatically returns the object to pool</returns>
        public static PooledObject<T> GetPooled<T>(this ObjectPool<T> pool) where T : class
        {
            var obj = pool.Get();
            return new PooledObject<T>(pool, obj);
        }
    }
}