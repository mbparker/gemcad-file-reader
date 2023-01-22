using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IGeometryOperations
    {
        double Length3d(Vertex3D p1, Vertex3D p2);
        Vertex3D ProjectPoint(Vertex3D center, double distance, double angle);
        Vertex3D RotatePoint(Vertex3D point, double yaw, double roll, double pitch, Vertex3D center);
    }
}