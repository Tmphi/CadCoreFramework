using System.IO;
using System.Linq;
using CadCoreFramework.Services;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Pure-C# tests cho FileSystemService — chạy trong temp directory để không chạm vào production FS.
    /// </summary>
    public class FileSystemServiceTests
    {
        private static string NewTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "CadCoreFrameworkTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public void FileExists_ReturnsFalse_ForMissingFile()
        {
            var fs = new FileSystemService();
            fs.FileExists(Path.Combine(Path.GetTempPath(), "no_such_file_12345.txt")).Should().BeFalse();
        }

        [Fact]
        public void WriteAllText_ThenReadAllText_RoundTrips()
        {
            var dir = NewTempDir();
            try
            {
                var fs = new FileSystemService();
                var file = Path.Combine(dir, "roundtrip.txt");
                fs.WriteAllText(file, "hello cad");
                fs.FileExists(file).Should().BeTrue();
                fs.ReadAllText(file).Should().Be("hello cad");
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void DeleteFile_IsIdempotent_WhenFileMissing()
        {
            var fs = new FileSystemService();
            var ghost = Path.Combine(Path.GetTempPath(), "ghost_" + Path.GetRandomFileName());
            fs.Invoking(f => f.DeleteFile(ghost)).Should().NotThrow();
        }

        [Fact]
        public void CreateDirectory_IsIdempotent()
        {
            var dir = NewTempDir();
            try
            {
                var fs = new FileSystemService();
                fs.Invoking(f => f.CreateDirectory(dir)).Should().NotThrow();
                fs.DirectoryExists(dir).Should().BeTrue();
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void GetFiles_ReturnsEmpty_ForMissingDirectory()
        {
            var fs = new FileSystemService();
            var ghost = Path.Combine(Path.GetTempPath(), "no_such_dir_" + Path.GetRandomFileName());
            fs.GetFiles(ghost).Should().BeEmpty();
        }

        [Fact]
        public void GetFiles_ReturnsMatchingFiles_InDirectory()
        {
            var dir = NewTempDir();
            try
            {
                var fs = new FileSystemService();
                fs.WriteAllText(Path.Combine(dir, "a.txt"), "x");
                fs.WriteAllText(Path.Combine(dir, "b.txt"), "x");
                fs.WriteAllText(Path.Combine(dir, "c.log"), "x");

                var txtFiles = fs.GetFiles(dir, "*.txt").ToList();
                txtFiles.Should().HaveCount(2);
                txtFiles.Should().OnlyContain(f => f.EndsWith(".txt"));
            }
            finally { Directory.Delete(dir, recursive: true); }
        }
    }
}
