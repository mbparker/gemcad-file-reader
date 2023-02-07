using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IGeometryOperations
    {
        double Length3d(Vertex3D p1, Vertex3D p2);
        Vertex3D ProjectPoint(Vertex3D center, double distance, double angle);
        Vertex3D RotatePoint(Vertex3D point, double yaw, double roll, double pitch, Vertex3D center);
        
        /// <summary>
        /// Calculates the angle between two vectors that share an endpoint. (eg. two sides of a triangle)
        /// </summary>
        /// <param name="p1">Endpoint 1 of vector 1</param>
        /// <param name="p2">The shared endpoint of vectors 1 and 2</param>
        /// <param name="p3">Endpoint 2 of vector 2</param>
        /// <returns>The angle in degrees</returns>
        double AngleBetweenConnectedVectors(Vertex3D p1, Vertex3D p2, Vertex3D p3);
        
        double GetAngle2d(Vertex3D p1, Vertex3D p2);
        Vertex3D ProjectPointAlongVector(Vertex3D p1, Vertex3D p2, double distance);
        Triangle CreateTriangleFromPoints(Vertex3D p1, Vertex3D p2, Vertex3D p3, bool ensureWindingOrder);
    }
}