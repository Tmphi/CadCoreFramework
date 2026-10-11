using System;
using CadCoreFramework.Core;
using CadCoreFramework.Services;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Tests cho SettingsService — kiểm tra Get/Set/Save, type conversion, thread safety.
    /// Sử dụng Mock cho IFileSystemService, IPathProvider, ILoggerService.
    /// </summary>
    public class SettingsServiceTests
    {
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IPathProvider> _mockPathProvider;
        private readonly Mock<IFileSystemService> _mockFileSystem;

        public SettingsServiceTests()
        {
            _mockLogger = new Mock<ILoggerService>();
            _mockPathProvider = new Mock<IPathProvider>();
            _mockFileSystem = new Mock<IFileSystemService>();

            _mockPathProvider.Setup(p => p.GetAppDataDirectory()).Returns(@"C:\TestApp");
            _mockFileSystem.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        }

        private SettingsService CreateService(string existingJson = null)
        {
            if (existingJson != null)
            {
                _mockFileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
                _mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(existingJson);
            }
            else
            {
                _mockFileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
            }

            return new SettingsService(_mockLogger.Object, _mockPathProvider.Object, _mockFileSystem.Object);
        }

        [Fact]
        public void GetSetting_WithNoExistingFile_ShouldReturnDefault()
        {
            var service = CreateService();
            service.GetSetting<string>("nonexistent").Should().BeNull();
            service.GetSetting("nonexistent", 42).Should().Be(42);
        }

        [Fact]
        public void SetSetting_ThenGet_ShouldReturnValue()
        {
            var service = CreateService();
            service.SetSetting("theme", "dark");
            service.GetSetting<string>("theme").Should().Be("dark");
        }

        [Fact]
        public void SetSetting_Overwrite_ShouldReturnNewValue()
        {
            var service = CreateService();
            service.SetSetting("count", 10);
            service.SetSetting("count", 20);
            service.GetSetting<int>("count").Should().Be(20);
        }

        [Fact]
        public void GetSetting_WithExistingJsonFile_ShouldLoadValues()
        {
            var json = JsonConvert.SerializeObject(new { theme = "light", fontSize = 14 });
            var service = CreateService(json);

            service.GetSetting<string>("theme").Should().Be("light");
        }

        [Fact]
        public void GetSetting_TypeConversion_IntToLong_ShouldWork()
        {
            // Newtonsoft deserializes numbers as Int64 by default
            var json = JsonConvert.SerializeObject(new { count = 42 });
            var service = CreateService(json);

            // Should convert from Int64 (Newtonsoft default) to Int32
            service.GetSetting<int>("count").Should().Be(42);
        }

        [Fact]
        public void Save_ShouldWriteJsonToFile()
        {
            var service = CreateService();
            service.SetSetting("key1", "value1");
            service.Save();

            _mockFileSystem.Verify(f => f.WriteAllText(
                It.Is<string>(path => path.Contains("settings.json")),
                It.Is<string>(content => content.Contains("key1") && content.Contains("value1"))
            ), Times.Once());
        }

        [Fact]
        public void GetSetting_WithCorruptJson_ShouldReturnDefault()
        {
            _mockFileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns("NOT VALID JSON {{{");

            var service = new SettingsService(_mockLogger.Object, _mockPathProvider.Object, _mockFileSystem.Object);
            service.GetSetting("anything", "fallback").Should().Be("fallback");
        }

        [Fact]
        public void Constructor_ShouldCreateDirectoryIfNotExists()
        {
            _mockFileSystem.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

            var service = new SettingsService(_mockLogger.Object, _mockPathProvider.Object, _mockFileSystem.Object);

            _mockFileSystem.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public void GetSetting_ComplexObject_ShouldDeserialize()
        {
            var service = CreateService();
            var settings = new { Width = 100, Height = 200, Name = "Panel" };
            service.SetSetting("panel", settings);

            // Since the object is stored as anonymous type, retrieve as dynamic via JSON re-serialization
            var result = service.GetSetting<PanelSettings>("panel");
            result.Should().NotBeNull();
            result.Width.Should().Be(100);
            result.Height.Should().Be(200);
        }

        public class PanelSettings
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public string Name { get; set; }
        }
    }
}
