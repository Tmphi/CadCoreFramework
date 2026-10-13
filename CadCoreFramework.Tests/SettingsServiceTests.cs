using System;
using System.IO;
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

        private SettingsService CreateService(string? existingJson = null)
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
            public string Name { get; set; } = string.Empty;
        }

        // ─── Nhiều phiên AutoCAD (B-22) ─────────────────────────────────────────

        [Fact]
        public void Save_KeepsKeysSavedByAnotherSession_AndOverlaysOnlyChangedKeys()
        {
            var service = CreateService(JsonConvert.SerializeObject(new { shared = "startup", untouched = 1 }));

            // Phiên khác đã lưu SAU khi phiên này khởi động: đổi 'shared', thêm 'other'.
            _mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>()))
                .Returns(JsonConvert.SerializeObject(new { shared = "from-other", untouched = 1, other = true }));

            service.SetSetting("mine", 42);
            string? written = null;
            _mockFileSystem.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, content) => written = content);

            service.Save();

            var saved = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(written!)!;
            saved["mine"].Should().Be(42L);
            saved["other"].Should().Be(true, "khóa do phiên khác lưu không được mất");
            saved["shared"].Should().Be("from-other", "phiên này không đổi 'shared' nên không được ghi đè giá trị mới hơn");
            service.GetSetting<bool>("other").Should().BeTrue("sau Save, phiên này thấy cả khóa của phiên khác");
        }

        [Fact]
        public void Save_ChangedKeyWinsOverDiskValue()
        {
            var service = CreateService(JsonConvert.SerializeObject(new { shared = "startup" }));
            _mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>()))
                .Returns(JsonConvert.SerializeObject(new { shared = "from-other" }));

            service.SetSetting("shared", "mine");
            service.Save();

            _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(),
                It.Is<string>(c => c.Contains("\"mine\"") && !c.Contains("from-other"))), Times.Once());
        }

        [Fact]
        public void Save_WhenWriteFails_KeepsChangesForNextSave()
        {
            var service = CreateService();
            service.SetSetting("key", "value");
            _mockFileSystem.SetupSequence(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new IOException("disk full"))
                .Pass();

            service.Save();   // lỗi → log, không ném
            service.Save();   // lần sau phải ghi lại khóa chưa lưu được

            _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.Is<string>(c => c.Contains("value"))), Times.Exactly(2));
            _mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }

        [Fact]
        public void TwoRealSessions_SavingDifferentKeys_BothKeysSurvive()
        {
            string dir = Path.Combine(Path.GetTempPath(), "CadCoreSettingsTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var paths = new Mock<IPathProvider>();
                paths.Setup(p => p.GetAppDataDirectory()).Returns(dir);
                var fs = new FileSystemService();

                var sessionA = new SettingsService(_mockLogger.Object, paths.Object, fs);
                var sessionB = new SettingsService(_mockLogger.Object, paths.Object, fs);
                sessionA.SetSetting("fromA", "a");
                sessionB.SetSetting("fromB", "b");
                sessionA.Save();
                sessionB.Save();   // trước B-22: ghi đè mất 'fromA'

                var fresh = new SettingsService(_mockLogger.Object, paths.Object, fs);
                fresh.GetSetting<string>("fromA").Should().Be("a");
                fresh.GetSetting<string>("fromB").Should().Be("b");
                Directory.GetFiles(dir, "*.tmp").Should().BeEmpty("ghi atomic không được để lại tệp tạm");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
