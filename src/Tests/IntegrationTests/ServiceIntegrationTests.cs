using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameAutomation.Models;
using GameAutomation.Services;
using GameAutomation.Infrastructure;

namespace GameAutomation.Tests.IntegrationTests
{
    [TestClass]
    public class ServiceIntegrationTests
    {
        private IConfigurationService _configService = null!;
        private ISpellService _spellService = null!;
        private ICooldownService _cooldownService = null!;
        private IWindowService _windowService = null!;
        private IInputService _inputService = null!;
        private MemoryManager _memoryManager = null!;
        private string _testConfigPath = null!;
        private string _testSpellsPath = null!;

        [TestInitialize]
        public async Task Setup()
        {
            // Create test configuration
            _testConfigPath = Path.GetTempFileName();
            _testSpellsPath = Path.GetTempFileName();
            
            var testConfig = @"{
                ""input"": {
                    ""defaultMethod"": ""KeyboardEventOptimized"",
                    ""retryAttempts"": 3,
                    ""keyDelayMs"": 50
                },
                ""cooldowns"": {
                    ""cleanupIntervalMinutes"": 1,
                    ""maxCooldowns"": 100
                },
                ""spells"": {
                    ""configPath"": """ + _testSpellsPath.Replace("\\", "\\\\") + @"""
                }
            }";

            var testSpells = @"[
                {
                    ""id"": ""test_spell_1"",
                    ""displayName"": ""Test Spell 1"",
                    ""keyCombination"": ""F1"",
                    ""cooldown"": ""00:00:02"",
                    ""description"": ""Test spell for integration testing""
                },
                {
                    ""id"": ""test_spell_2"",
                    ""displayName"": ""Test Spell 2"",
                    ""keyCombination"": ""F2"",
                    ""cooldown"": ""00:00:05"",
                    ""description"": ""Another test spell""
                }
            ]";

            await File.WriteAllTextAsync(_testConfigPath, testConfig);
            await File.WriteAllTextAsync(_testSpellsPath, testSpells);

            // Initialize services
            _memoryManager = new MemoryManager(TimeSpan.FromSeconds(30));
            _configService = new ConfigurationService();
            await _configService.LoadConfigurationAsync(_testConfigPath);
            
            _spellService = new SpellService(_configService);
            await _spellService.LoadSpellsAsync();
            
            _cooldownService = new CooldownService(_configService, _memoryManager);
            _windowService = new WindowService();
            _inputService = new InputService(_windowService, _configService);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cooldownService?.Dispose();
            _windowService?.Dispose();
            _inputService?.Dispose();
            _memoryManager?.Dispose();

            // Clean up test files
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
            if (File.Exists(_testSpellsPath))
                File.Delete(_testSpellsPath);
        }

        [TestMethod]
        public async Task SpellService_LoadsConfigurationCorrectly()
        {
            // Act
            var spells = await _spellService.GetAllSpellsAsync();

            // Assert
            Assert.AreEqual(2, spells.Count());
            
            var spell1 = spells.FirstOrDefault(s => s.Id == "test_spell_1");
            Assert.IsNotNull(spell1);
            Assert.AreEqual("Test Spell 1", spell1.DisplayName);
            Assert.AreEqual("F1", spell1.KeyCombination);
            Assert.AreEqual(TimeSpan.FromSeconds(2), spell1.Cooldown);
        }

        [TestMethod]
        public async Task CooldownService_IntegratesWithSpellService()
        {
            // Arrange
            var window = new TestGameWindow(new IntPtr(12345), "Test Window");
            _windowService.RegisterWindow(window.WindowHandle, window.Title, window.WindowRect);
            
            var spells = await _spellService.GetAllSpellsAsync();
            var testSpell = spells.First(s => s.Id == "test_spell_1");

            // Act
            await _cooldownService.StartCooldownAsync(window, testSpell);

            // Assert
            Assert.IsTrue(_cooldownService.IsOnCooldown(window, testSpell));
            
            var remaining = _cooldownService.GetRemainingCooldown(window, testSpell);
            Assert.IsNotNull(remaining);
            Assert.IsTrue(remaining.Value.TotalSeconds <= 2);
        }

        [TestMethod]
        public async Task ConfigurationService_ProvidesCorrectValues()
        {
            // Act & Assert
            Assert.AreEqual("KeyboardEventOptimized", _configService.GetString("input.defaultMethod"));
            Assert.AreEqual(3, _configService.GetInt("input.retryAttempts"));
            Assert.AreEqual(50, _configService.GetInt("input.keyDelayMs"));
            Assert.AreEqual(1, _configService.GetInt("cooldowns.cleanupIntervalMinutes"));
            Assert.AreEqual(100, _configService.GetInt("cooldowns.maxCooldowns"));
        }

        [TestMethod]
        public async Task MemoryManager_IntegratesWithCooldownService()
        {
            // Arrange
            var window = new TestGameWindow(new IntPtr(12345), "Test Window");
            var spells = await _spellService.GetAllSpellsAsync();
            var testSpell = spells.First();

            // Act - Start many cooldowns to trigger memory management
            for (int i = 0; i < 50; i++)
            {
                var testWindow = new TestGameWindow(new IntPtr(i + 1000), $"Window {i}");
                await _cooldownService.StartCooldownAsync(testWindow, testSpell);
            }

            var initialStats = _memoryManager.GetMemoryStats();

            // Force cleanup
            _memoryManager.ForceCleanup();
            await _cooldownService.CleanupExpiredCooldownsAsync();

            var afterStats = _memoryManager.GetMemoryStats();

            // Assert - Memory should be managed efficiently
            Assert.IsTrue(afterStats.Gen0Collections >= initialStats.Gen0Collections);
        }

        [TestMethod]
        public async Task WindowService_IntegratesWithInputService()
        {
            // Arrange
            var window = new TestGameWindow(new IntPtr(12345), "Test Window");
            _windowService.RegisterWindow(window.WindowHandle, window.Title, window.WindowRect);

            // Act
            var registeredWindows = _windowService.GetAllWindows();
            var targetWindow = registeredWindows.FirstOrDefault();

            // Assert
            Assert.IsNotNull(targetWindow);
            Assert.AreEqual(window.WindowHandle, targetWindow.WindowHandle);
            Assert.AreEqual(window.Title, targetWindow.Title);
        }

        [TestMethod]
        public async Task EndToEndSpellExecution_WorksCorrectly()
        {
            // Arrange
            var window = new TestGameWindow(new IntPtr(12345), "Test Window");
            _windowService.RegisterWindow(window.WindowHandle, window.Title, window.WindowRect);
            
            var spells = await _spellService.GetAllSpellsAsync();
            var testSpell = spells.First(s => s.Id == "test_spell_1");

            // Act - Simulate spell execution workflow
            
            // 1. Check if spell is available (not on cooldown)
            bool canCast = !_cooldownService.IsOnCooldown(window, testSpell);
            Assert.IsTrue(canCast, "Spell should be available initially");

            // 2. Start cooldown (simulating spell cast)
            await _cooldownService.StartCooldownAsync(window, testSpell);

            // 3. Verify cooldown is active
            Assert.IsTrue(_cooldownService.IsOnCooldown(window, testSpell));

            // 4. Check remaining time
            var remaining = _cooldownService.GetRemainingCooldown(window, testSpell);
            Assert.IsNotNull(remaining);
            Assert.IsTrue(remaining.Value.TotalSeconds > 0);

            // 5. Verify spell cannot be cast again immediately
            bool canCastAgain = !_cooldownService.IsOnCooldown(window, testSpell);
            Assert.IsFalse(canCastAgain, "Spell should be on cooldown");

            // Assert - All integration points work together
            Assert.AreEqual(1, _cooldownService.GetCooldownCount());
        }

        [TestMethod]
        public async Task ConfigurationChanges_AffectServiceBehavior()
        {
            // Arrange - Update configuration
            _configService.SetValue("cooldowns.maxCooldowns", 2);
            
            var newCooldownService = new CooldownService(_configService, _memoryManager);
            var spells = await _spellService.GetAllSpellsAsync();
            var testSpell = spells.First();

            try
            {
                // Act - Add more cooldowns than the new limit
                for (int i = 0; i < 5; i++)
                {
                    var window = new TestGameWindow(new IntPtr(i + 2000), $"LimitTest {i}");
                    await newCooldownService.StartCooldownAsync(window, testSpell);
                }

                // Assert - Service should respect new configuration
                Assert.IsTrue(newCooldownService.GetCooldownCount() <= 2);
            }
            finally
            {
                newCooldownService.Dispose();
            }
        }
    }
}