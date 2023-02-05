using LibGemcadFileReader.Models.Geometry;

namespace LibGemcadFileReader.Abstract
{
    public interface IGemCadFileImport
    {
        GemCadFileData Import(string filename);
    }
}