using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IGemCadAscImport
    {
        PolygonContainer Import(string filename);
    }
}