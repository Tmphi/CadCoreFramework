using System.Collections.Generic;
using System.IO;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Lớp triển khai IFileSystemService thực hiện các thao tác tệp và thư mục trên hệ thống tệp đĩa vật lý của hệ điều hành.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
        }

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

        public IEnumerable<string> GetFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(path)) return new string[0];
            return Directory.GetFiles(path, searchPattern, searchOption);
        }
    }
}
