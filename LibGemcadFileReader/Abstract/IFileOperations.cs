using System.IO;

namespace LibGemcadFileReader.Abstract
{
    public interface IFileOperations
    {
        Stream CreateFileStream(string path, FileMode mode);
    }
}