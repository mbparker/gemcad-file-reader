using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IVectorOperations
    {
        Vertex3D Add(Vertex3D p1, Vertex3D p2);

        Vertex3D CalculateNormal(Vertex3D p1, Vertex3D p2, Vertex3D p3);

        Vertex3D CalculateNormal(Vertex3D p1, Vertex3D p2, Vertex3D p3, Vertex3D p4);

        Vertex3D CrossProduct(Vertex3D p1, Vertex3D p2);

        double DotProduct(Vertex3D p1, Vertex3D p2);

        Vertex3D Divide(Vertex3D p, double quo);

        Vertex3D MultiplyScalar(Vertex3D p, double f);

        void Normalize(Vertex3D p);

        Vertex3D Subtract(Vertex3D p1, Vertex3D p2);

        double Length(Vertex3D p);

        double AngleBetween(Vertex3D p1, Vertex3D p2);
        Vertex3D FindRayPlaneIntersection(Vertex3D rayOrigin, Vertex3D rayDirection, Triangle plane);
    }
}