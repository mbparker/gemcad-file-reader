using LibGemcadFileReader.Models.Geometry;

namespace LibGemcadFileReader.Abstract
{
    public interface IGemCadGemImport
    {
        GemCadFileData Import(string filename);
    }
}