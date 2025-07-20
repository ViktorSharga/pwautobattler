using System;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameAutomation.Models;
using GameAutomation.Services;

namespace GameAutomation.Tests.UnitTests.Services
{
    [TestClass]
    public class WindowServiceTests
    {
        private WindowService _windowService = null!;

        [TestInitialize]
        public void Setup()
        {
            _windowService = new WindowService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _windowService?.Dispose();
        }

        [TestMethod]
        public void RegisterWindow_ValidWindow_ShouldAddToCollection()
        {
            // Arrange
            var handle = new IntPtr(12345);
            var title = "Test Window";
            var rect = new Rectangle(0, 0, 800, 600);

            // Act
            _windowService.RegisterWindow(handle, title, rect);

            // Assert
            var windows = _windowService.GetAllWindows();
            Assert.AreEqual(1, windows.Count());
            
            var window = windows.First();
            Assert.AreEqual(handle, window.WindowHandle);
            Assert.AreEqual(title, window.Title);
            Assert.AreEqual(rect, window.WindowRect);
        }

        [TestMethod]
        public void RegisterWindow_DuplicateHandle_ShouldUpdateExisting()
        {
            // Arrange
            var handle = new IntPtr(12345);
            var originalTitle = "Original Title";
            var newTitle = "Updated Title";
            var rect = new Rectangle(0, 0, 800, 600);

            // Act
            _windowService.RegisterWindow(handle, originalTitle, rect);
            _windowService.RegisterWindow(handle, newTitle, rect);

            // Assert
            var windows = _windowService.GetAllWindows();
            Assert.AreEqual(1, windows.Count());
            Assert.AreEqual(newTitle, windows.First().Title);
        }

        [TestMethod]
        public void UnregisterWindow_ExistingWindow_ShouldRemoveFromCollection()
        {
            // Arrange
            var handle = new IntPtr(12345);
            _windowService.RegisterWindow(handle, "Test", new Rectangle(0, 0, 800, 600));

            // Act
            var result = _windowService.UnregisterWindow(handle);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _windowService.GetAllWindows().Count());
        }

        [TestMethod]
        public void UnregisterWindow_NonExistentWindow_ShouldReturnFalse()
        {
            // Arrange
            var handle = new IntPtr(99999);

            // Act
            var result = _windowService.UnregisterWindow(handle);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetWindow_ExistingHandle_ShouldReturnWindow()
        {
            // Arrange
            var handle = new IntPtr(12345);
            var title = "Test Window";
            var rect = new Rectangle(0, 0, 800, 600);
            _windowService.RegisterWindow(handle, title, rect);

            // Act
            var window = _windowService.GetWindow(handle);

            // Assert
            Assert.IsNotNull(window);
            Assert.AreEqual(handle, window.WindowHandle);
            Assert.AreEqual(title, window.Title);
        }

        [TestMethod]
        public void GetWindow_NonExistentHandle_ShouldReturnNull()
        {
            // Arrange
            var handle = new IntPtr(99999);

            // Act
            var window = _windowService.GetWindow(handle);

            // Assert
            Assert.IsNull(window);
        }

        [TestMethod]
        public void ClearAllWindows_ShouldRemoveAllWindows()
        {
            // Arrange
            _windowService.RegisterWindow(new IntPtr(1), "Window1", new Rectangle(0, 0, 800, 600));
            _windowService.RegisterWindow(new IntPtr(2), "Window2", new Rectangle(0, 0, 800, 600));
            _windowService.RegisterWindow(new IntPtr(3), "Window3", new Rectangle(0, 0, 800, 600));

            // Act
            _windowService.ClearAllWindows();

            // Assert
            Assert.AreEqual(0, _windowService.GetAllWindows().Count());
        }

        [TestMethod]
        public void RegisterWindow_InvalidHandle_ShouldThrowException()
        {
            // Arrange
            var handle = IntPtr.Zero;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _windowService.RegisterWindow(handle, "Test", new Rectangle(0, 0, 800, 600)));
        }

        [TestMethod]
        public void RegisterWindow_NullOrEmptyTitle_ShouldThrowException()
        {
            // Arrange
            var handle = new IntPtr(12345);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _windowService.RegisterWindow(handle, "", new Rectangle(0, 0, 800, 600)));

            Assert.ThrowsException<ArgumentNullException>(() =>
                _windowService.RegisterWindow(handle, null!, new Rectangle(0, 0, 800, 600)));
        }
    }
}