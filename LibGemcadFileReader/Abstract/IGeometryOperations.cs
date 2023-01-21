using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IGeometryOperations
    {
        double Length3d(Vertex3D p1, Vertex3D p2);
        double TrueAngleBetweenVectors(Vertex3D p1, Vertex3D p2, Vertex3D p3);
    }
}