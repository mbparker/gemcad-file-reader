using System.IO;
using LibGemcadFileReader.Abstract;

namespace LibGemcadFileReader.Concrete
{
    public class FileOperations : IFileOperations
    {
        public Stream CreateFileStream(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }
        
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}