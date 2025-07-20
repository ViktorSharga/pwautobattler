using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameAutomation.Models;
using GameAutomation.Models.Spells;
using GameAutomation.Services;

namespace GameAutomation.Tests.UnitTests.Services
{
    [TestClass]
    public class CooldownServiceTests
    {
        private CooldownService _cooldownService = null!;
        private TestGameWindow _testWindow = null!;
        private TestSpell _testSpell = null!;

        [TestInitialize]
        public void Setup()
        {
            _cooldownService = new CooldownService();
            _testWindow = new TestGameWindow(new IntPtr(12345), "Test Window");
            _testSpell = new TestSpell("test_spell", "Test Spell", TimeSpan.FromSeconds(5));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cooldownService?.Dispose();
        }

        [TestMethod]
        public void IsOnCooldown_NoCooldownSet_ShouldReturnFalse()
        {
            // Act
            var result = _cooldownService.IsOnCooldown(_testWindow, _testSpell);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task StartCooldownAsync_ShouldSetCooldown()
        {
            // Act
            await _cooldownService.StartCooldownAsync(_testWindow, _testSpell);

            // Assert
            Assert.IsTrue(_cooldownService.IsOnCooldown(_testWindow, _testSpell));
        }

        [TestMethod]
        public async Task GetRemainingCooldown_ActiveCooldown_ShouldReturnTimeRemaining()
        {
            // Act
            await _cooldownService.StartCooldownAsync(_testWindow, _testSpell);
            var remaining = _cooldownService.GetRemainingCooldown(_testWindow, _testSpell);

            // Assert
            Assert.IsNotNull(remaining);
            Assert.IsTrue(remaining.Value.TotalSeconds > 0);
            Assert.IsTrue(remaining.Value.TotalSeconds <= 5);
        }

        [TestMethod]
        public void GetRemainingCooldown_NoCooldown_ShouldReturnNull()
        {
            // Act
            var remaining = _cooldownService.GetRemainingCooldown(_testWindow, _testSpell);

            // Assert
            Assert.IsNull(remaining);
        }

        [TestMethod]
        public async Task ResetCooldownAsync_ShouldRemoveCooldown()
        {
            // Arrange
            await _cooldownService.StartCooldownAsync(_testWindow, _testSpell);
            Assert.IsTrue(_cooldownService.IsOnCooldown(_testWindow, _testSpell));

            // Act
            await _cooldownService.ResetCooldownAsync(_testWindow, _testSpell);

            // Assert
            Assert.IsFalse(_cooldownService.IsOnCooldown(_testWindow, _testSpell));
        }

        [TestMethod]
        public async Task CleanupExpiredCooldownsAsync_ShouldRemoveExpiredCooldowns()
        {
            // Arrange
            var shortSpell = new TestSpell("short_spell", "Short Spell", TimeSpan.FromMilliseconds(50));
            await _cooldownService.StartCooldownAsync(_testWindow, shortSpell);

            // Wait for cooldown to expire
            await Task.Delay(100);

            // Act
            await _cooldownService.CleanupExpiredCooldownsAsync();

            // Assert
            Assert.IsFalse(_cooldownService.IsOnCooldown(_testWindow, shortSpell));
        }

        [TestMethod]
        public async Task CooldownEvents_ShouldBeRaised()
        {
            // Arrange
            bool cooldownStartedRaised = false;
            bool cooldownExpiredRaised = false;

            _cooldownService.CooldownStarted += (sender, args) => cooldownStartedRaised = true;
            _cooldownService.CooldownExpired += (sender, args) => cooldownExpiredRaised = true;

            // Act
            await _cooldownService.StartCooldownAsync(_testWindow, _testSpell);
            await _cooldownService.ResetCooldownAsync(_testWindow, _testSpell);

            // Assert
            Assert.IsTrue(cooldownStartedRaised);
            Assert.IsTrue(cooldownExpiredRaised);
        }

        [TestMethod]
        public async Task MaxCooldowns_ShouldTriggerCleanup()
        {
            // Arrange - Create many cooldowns to test cleanup
            var configService = new ConfigurationService();
            configService.SetValue("cooldowns.maxCooldowns", 5);
            
            var limitedCooldownService = new CooldownService(configService);

            try
            {
                // Act - Add more cooldowns than the limit
                for (int i = 0; i < 10; i++)
                {
                    var window = new TestGameWindow(new IntPtr(i + 1000), $"Window {i}");
                    var spell = new TestSpell($"spell_{i}", $"Spell {i}", TimeSpan.FromMinutes(1));
                    await limitedCooldownService.StartCooldownAsync(window, spell);
                }

                // Assert - Should have cleaned up excess cooldowns
                Assert.IsTrue(limitedCooldownService.GetCooldownCount() <= 5);
            }
            finally
            {
                limitedCooldownService.Dispose();
            }
        }
    }

    // Test helper classes
    public class TestGameWindow : IGameWindow
    {
        public IntPtr WindowHandle { get; }
        public string Title { get; }
        public System.Drawing.Rectangle WindowRect { get; set; }
        public bool IsMinimized { get; set; }

        public TestGameWindow(IntPtr handle, string title)
        {
            WindowHandle = handle;
            Title = title;
            WindowRect = new System.Drawing.Rectangle(0, 0, 800, 600);
        }
    }

    public class TestSpell : ISpell
    {
        public string Id { get; }
        public string DisplayName { get; }
        public TimeSpan Cooldown { get; }
        public string KeyCombination { get; set; } = "";
        public string Description { get; set; } = "";

        public TestSpell(string id, string displayName, TimeSpan cooldown)
        {
            Id = id;
            DisplayName = displayName;
            Cooldown = cooldown;
        }
    }
}