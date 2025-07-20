using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAutomation.Infrastructure;
using GameAutomation.Services;

namespace GameAutomation.Examples
{
    /// <summary>
    /// Demonstrates the performance and memory optimization features in Phase 6
    /// </summary>
    public partial class PerformanceOptimizedDemo : Form
    {
        private readonly MemoryManager _memoryManager;
        private readonly ObjectPool<TestObject> _objectPool;
        private readonly StringCache _stringCache;
        private readonly StringBuilderPool _stringBuilderPool;
        private readonly CooldownService _cooldownService;
        private readonly ConfigurationService _configurationService;

        private Label _memoryStatsLabel = null!;
        private Label _poolStatsLabel = null!;
        private Label _stringCacheStatsLabel = null!;
        private Label _cooldownStatsLabel = null!;
        private Button _createObjectsButton = null!;
        private Button _cleanupButton = null!;
        private Button _optimizeMemoryButton = null!;
        private System.Windows.Forms.Timer _updateTimer = null!;

        private int _objectsCreated = 0;
        private long _initialMemory = 0;

        public PerformanceOptimizedDemo()
        {
            // Initialize infrastructure
            _memoryManager = new MemoryManager(TimeSpan.FromSeconds(10)); // More frequent cleanup for demo
            _objectPool = new ObjectPool<TestObject>(
                objectGenerator: () => new TestObject(),
                resetAction: obj => obj.Reset(),
                maxSize: 100
            );
            _stringCache = new StringCache(500);
            _stringBuilderPool = new StringBuilderPool(20);
            
            // Initialize services with optimization
            _configurationService = new ConfigurationService();
            _cooldownService = new CooldownService(_configurationService, _memoryManager);

            InitializeComponent();
            InitializeDemo();
        }

        private void InitializeComponent()
        {
            Text = "Performance & Memory Optimization Demo - Phase 6";
            Size = new Size(800, 700);
            StartPosition = FormStartPosition.CenterScreen;

            CreateMemoryControls();
            CreatePoolControls();
            CreateStringCacheControls();
            CreateCooldownControls();
            CreateActionButtons();
            CreateInstructions();
            LayoutControls();

            // Update timer for real-time stats
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000; // Update every second
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void CreateMemoryControls()
        {
            var memoryLabel = new Label
            {
                Text = "Memory Statistics:",
                Location = new Point(10, 10),
                Size = new Size(150, 20),
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };

            _memoryStatsLabel = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(760, 60),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightGray
            };

            Controls.Add(memoryLabel);
            Controls.Add(_memoryStatsLabel);
        }

        private void CreatePoolControls()
        {
            var poolLabel = new Label
            {
                Text = "Object Pool Statistics:",
                Location = new Point(10, 105),
                Size = new Size(200, 20),
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };

            _poolStatsLabel = new Label
            {
                Location = new Point(10, 130),
                Size = new Size(760, 40),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightBlue
            };

            Controls.Add(poolLabel);
            Controls.Add(_poolStatsLabel);
        }

        private void CreateStringCacheControls()
        {
            var stringLabel = new Label
            {
                Text = "String Cache Statistics:",
                Location = new Point(10, 180),
                Size = new Size(200, 20),
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };

            _stringCacheStatsLabel = new Label
            {
                Location = new Point(10, 205),
                Size = new Size(760, 40),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightGreen
            };

            Controls.Add(stringLabel);
            Controls.Add(_stringCacheStatsLabel);
        }

        private void CreateCooldownControls()
        {
            var cooldownLabel = new Label
            {
                Text = "Cooldown Service Statistics:",
                Location = new Point(10, 255),
                Size = new Size(200, 20),
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };

            _cooldownStatsLabel = new Label
            {
                Location = new Point(10, 280),
                Size = new Size(760, 40),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightYellow
            };

            Controls.Add(cooldownLabel);
            Controls.Add(_cooldownStatsLabel);
        }

        private void CreateActionButtons()
        {
            _createObjectsButton = new Button
            {
                Text = "Create 1000 Objects",
                Location = new Point(10, 330),
                Size = new Size(150, 30)
            };
            _createObjectsButton.Click += CreateObjectsButton_Click;

            _cleanupButton = new Button
            {
                Text = "Force Cleanup",
                Location = new Point(170, 330),
                Size = new Size(150, 30)
            };
            _cleanupButton.Click += CleanupButton_Click;

            _optimizeMemoryButton = new Button
            {
                Text = "Optimize Memory",
                Location = new Point(330, 330),
                Size = new Size(150, 30)
            };
            _optimizeMemoryButton.Click += OptimizeMemoryButton_Click;

            Controls.Add(_createObjectsButton);
            Controls.Add(_cleanupButton);
            Controls.Add(_optimizeMemoryButton);
        }

        private void CreateInstructions()
        {
            var instructionLabel = new Label
            {
                Text = "Performance Optimization Demo:\n\n" +
                       "• Object Pool: Reuses TestObject instances to reduce GC pressure\n" +
                       "• Memory Manager: Automatic cleanup with configurable intervals\n" +
                       "• String Cache: Interns frequently used strings to save memory\n" +
                       "• Enhanced CooldownService: LRU eviction and configurable limits\n" +
                       "• StringBuilder Pool: Reuses StringBuilder instances for string operations\n\n" +
                       "Click buttons to see memory usage changes and optimization effects.",
                Location = new Point(10, 370),
                Size = new Size(760, 140),
                ForeColor = Color.DarkBlue,
                Font = new Font("Microsoft Sans Serif", 9F)
            };

            Controls.Add(instructionLabel);
        }

        private void LayoutControls()
        {
            // Controls are already positioned in creation methods
        }

        private async void InitializeDemo()
        {
            try
            {
                await _configurationService.LoadConfigurationAsync();
                var stats = _memoryManager.GetMemoryStats();
                _initialMemory = stats.ManagedMemoryBytes;
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CreateObjectsButton_Click(object? sender, EventArgs e)
        {
            _createObjectsButton.Enabled = false;
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Create objects using the pool
                var pooledObjects = new PooledObject<TestObject>[1000];
                for (int i = 0; i < 1000; i++)
                {
                    pooledObjects[i] = _objectPool.GetPooled();
                    pooledObjects[i].Object.SetData($"Object_{i}", i);
                    
                    // Test string cache
                    var key = _stringCache.CreateCooldownKey(new IntPtr(i), $"spell_{i % 10}");
                    _stringCache.Intern(key);
                    
                    _objectsCreated++;
                }
                
                stopwatch.Stop();
                
                // Return objects to pool
                foreach (var pooled in pooledObjects)
                {
                    pooled.Dispose();
                }
                
                var message = $"Created and pooled 1000 objects in {stopwatch.ElapsedMilliseconds}ms";
                MessageBox.Show(message, "Performance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                await Task.Delay(100); // Allow UI to update
            }
            finally
            {
                _createObjectsButton.Enabled = true;
            }
        }

        private async void CleanupButton_Click(object? sender, EventArgs e)
        {
            _cleanupButton.Enabled = false;
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                _memoryManager.ForceCleanup();
                _stringCache.Clear();
                await _cooldownService.CleanupExpiredCooldownsAsync();
                
                stopwatch.Stop();
                
                var message = $"Cleanup completed in {stopwatch.ElapsedMilliseconds}ms";
                MessageBox.Show(message, "Cleanup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                _cleanupButton.Enabled = true;
            }
        }

        private async void OptimizeMemoryButton_Click(object? sender, EventArgs e)
        {
            _optimizeMemoryButton.Enabled = false;
            
            try
            {
                var beforeStats = _memoryManager.GetMemoryStats();
                var stopwatch = Stopwatch.StartNew();
                
                await _memoryManager.OptimizeMemoryAsync();
                
                stopwatch.Stop();
                var afterStats = _memoryManager.GetMemoryStats();
                
                var beforeMB = beforeStats.ManagedMemoryBytes / (1024 * 1024);
                var afterMB = afterStats.ManagedMemoryBytes / (1024 * 1024);
                var savedMB = beforeMB - afterMB;
                
                var message = $"Memory optimization completed in {stopwatch.ElapsedMilliseconds}ms\n" +
                             $"Memory usage: {beforeMB}MB → {afterMB}MB (saved {savedMB}MB)";
                MessageBox.Show(message, "Optimization", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                _optimizeMemoryButton.Enabled = true;
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateStats();
        }

        private void UpdateStats()
        {
            try
            {
                var memoryStats = _memoryManager.GetMemoryStats();
                var workingSetMB = memoryStats.WorkingSetBytes / (1024 * 1024);
                var managedMB = memoryStats.ManagedMemoryBytes / (1024 * 1024);
                var initialMB = _initialMemory / (1024 * 1024);
                var deltaGB = (memoryStats.ManagedMemoryBytes - _initialMemory) / (1024.0 * 1024.0 * 1024.0);

                _memoryStatsLabel.Text = 
                    $"Working Set: {workingSetMB:N0} MB  |  Managed: {managedMB:N0} MB  |  Delta: {deltaGB:F3} GB\n" +
                    $"Gen0: {memoryStats.Gen0Collections}  |  Gen1: {memoryStats.Gen1Collections}  |  Gen2: {memoryStats.Gen2Collections}\n" +
                    $"Tracked Objects: {memoryStats.TotalObjects}";

                _poolStatsLabel.Text = 
                    $"Pool Size: {_objectPool.Count}/{_objectPool.MaxSize}  |  Objects Created: {_objectsCreated:N0}";

                _stringCacheStatsLabel.Text = 
                    $"Cache Size: {_stringCache.CacheSize}/{_stringCache.MaxCacheSize}  |  Builder Pool: {_stringBuilderPool.PoolSize}";

                _cooldownStatsLabel.Text = 
                    $"Active Cooldowns: {_cooldownService.GetCooldownCount()}  |  Cleanup Runs: {_cooldownService.GetCleanupCount()}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stats update error: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _memoryManager?.Dispose();
            _objectPool?.Dispose();
            _stringCache?.Dispose();
            _stringBuilderPool?.Dispose();
            _cooldownService?.Dispose();
            
            base.OnFormClosed(e);
        }
    }

    /// <summary>
    /// Test object for demonstrating object pooling
    /// </summary>
    public class TestObject
    {
        public string Name { get; private set; } = "";
        public int Value { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public TestObject()
        {
            Reset();
        }

        public void SetData(string name, int value)
        {
            Name = name;
            Value = value;
        }

        public void Reset()
        {
            Name = "";
            Value = 0;
            CreatedAt = DateTime.UtcNow;
        }
    }
}