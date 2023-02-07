using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{
    using System;

    public class VectorOperations : IVectorOperations
    {
        public Vertex3D Add(Vertex3D p1, Vertex3D p2)
        {
            var result = new Vertex3D();
            result.X = p1.X + p2.X;
            result.Y = p1.Y + p2.Y;
            result.Z = p1.Z + p2.Z;
            return result;
        }

        public Vertex3D CalculateNormal(Vertex3D p1, Vertex3D p2, Vertex3D p3)
        {
            Vertex3D v1 = Subtract(p1, p2);
            Vertex3D v2 = Subtract(p2, p3);
            Vertex3D normal = CrossProduct(v1, v2);
            Normalize(normal);
            return normal;
        }

        public Vertex3D CalculateNormal(Vertex3D p1, Vertex3D p2, Vertex3D p3, Vertex3D p4)
        {
            Vertex3D n1 = CalculateNormal(p1, p2, p4);
            Vertex3D n2 = CalculateNormal(p3, p3, p4);
            return Add(n1, n2);
        }

        public Vertex3D CrossProduct(Vertex3D p1, Vertex3D p2)
        {
            var result = new Vertex3D();
            result.X = (p1.Y * p2.Z) - (p1.Z * p2.Y);
            result.Y = (p1.Z * p2.X) - (p1.X * p2.Z);
            result.Z = (p1.X * p2.Y) - (p1.Y * p2.X);
            return result;
        }

        public double DotProduct(Vertex3D p1, Vertex3D p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z;
        }

        public Vertex3D Divide(Vertex3D p, double quo)
        {
            var result = new Vertex3D();
            result.X = p.X / quo;
            result.Y = p.Y / quo;
            result.Z = p.Z / quo;
            return result;
        }

        public Vertex3D MultiplyScalar(Vertex3D p, double f)
        {
            var result = new Vertex3D();
            result.X = p.X * f;
            result.Y = p.Y * f;
            result.Z = p.Z * f;
            return result;
        }

        public void Normalize(Vertex3D p)
        {
            var length = Math.Sqrt((p.X * p.X) + (p.Y * p.Y) + (p.Z * p.Z));
            if (length > 0)
            {
                p.X = p.X / length;
                p.Y = p.Y / length;
                p.Z = p.Z / length;
            }
        }

        public Vertex3D Subtract(Vertex3D p1, Vertex3D p2)
        {
            var result = new Vertex3D();
            result.X = p1.X - p2.X;
            result.Y = p1.Y - p2.Y;
            result.Z = p1.Z - p2.Z;
            return result;
        }

        public double Length(Vertex3D p)
        {
            return Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
        }

        public Vertex3D Negative(Vertex3D p)
        {
            return new Vertex3D(-p.X, -p.Y, -p.Z);
        }

        public double AngleBetween(Vertex3D p1, Vertex3D p2)
        {
            Normalize(p1);
            Normalize(p2);
            return (DotProduct(p1, p2) >= 0.0
                ? 2.0 * Math.Asin(Length(Subtract(p1, p2)) / 2.0)
                : Math.PI - 2.0 * Math.Asin(Length(Subtract(Negative(p1), p2)) / 2.0)) * (180.0 / Math.PI);
        }
        
        public Vertex3D FindRayPlaneIntersection(Vertex3D rayOrigin, Vertex3D rayDirection, Triangle plane)
        {
            var diff = Subtract(rayOrigin, plane.P1.Vertex);
            var prod1 = DotProduct(diff, plane.Normal);
            var prod2 = DotProduct(rayDirection, plane.Normal);
            var prod3 = prod1 / prod2;
            return MultiplyScalar(Subtract(rayOrigin, rayDirection), prod3);
        } 
    }
}