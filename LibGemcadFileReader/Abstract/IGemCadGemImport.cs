using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IGemCadGemImport
    {
        PolygonContainer Import(string filename);
    }
}