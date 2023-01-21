using LibGemcadFileReader.Models.Geometry;

namespace LibGemcadFileReader.Abstract
{
    public interface IGemCadAscImport
    {
        GemCadFileData Import(string filename);
    }
}