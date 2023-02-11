using System.IO;

namespace LibGemcadFileReader.Abstract
{
    public interface IFileOperations
    {
        Stream CreateFileStream(string path, FileMode mode);
        bool FileExists(string path);
        string CreateTempFilename();
        void DeleteFile(string path);
    }
}