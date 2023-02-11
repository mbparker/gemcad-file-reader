using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{
    using System;

    public class GeometryOperations : IGeometryOperations
    {
        private readonly IVectorOperations vectorOperations;
        
        public GeometryOperations(IVectorOperations vectorOperations)
        {
            this.vectorOperations = vectorOperations;
        }
        
        public double Length3d(Vertex3D p1, Vertex3D p2)
        {
            double x = Math.Pow(p2.X - p1.X, 2);
            double y = Math.Pow(p2.Y - p1.Y, 2);
            double z = Math.Pow(p2.Z - p1.Z, 2);
            return Math.Sqrt(x + y + z);
        }
        
        public Vertex3D ProjectPoint(Vertex3D center, double distance, double angle)
        {
            double radians = angle * Constants.Radian;
            double moveX = Math.Cos(radians) * distance;
            double moveY = Math.Sin(radians) * distance;
            return new Vertex3D { X = moveX + center.X, Y = moveY + center.Y, Z = center.Z };
        }
        
        public Vertex3D RotatePoint(Vertex3D point, double yaw, double roll, double pitch, Vertex3D center)
        {
            var result = new Vertex3D { X = point.X, Y = point.Y, Z = point.Z };
            if (Math.Abs(yaw) >= double.Epsilon || Math.Abs(roll) >= double.Epsilon || Math.Abs(pitch) >= double.Epsilon)
            {
                yaw = MathUtils.FilterAngle(yaw);
                roll = MathUtils.FilterAngle(roll);
                pitch = MathUtils.FilterAngle(pitch);

                result.X -= center.X;
                result.Y -= center.Y;
                result.Z -= center.Z;

                double yawCosine = Math.Cos(yaw * Constants.Radian);
                double yawSine = Math.Sin(yaw * Constants.Radian);
                double rollCosine = Math.Cos(roll * Constants.Radian);
                double rollSine = Math.Sin(roll * Constants.Radian);
                double pitchCosine = Math.Cos(pitch * Constants.Radian);
                double pitchSine = Math.Sin(pitch * Constants.Radian);

                double workX = (yawCosine * result.X) - (yawSine * result.Z);
                double workZ = (yawSine * result.X) + (yawCosine * result.Z);
                result.X = (rollCosine * workX) + (rollSine * result.Y);
                double workY = (rollCosine * result.Y) - (rollSine * workX);
                result.Z = (pitchCosine * workZ) - (pitchSine * workY);
                result.Y = (pitchSine * workZ) + (pitchCosine * workY);

                result.X += center.X;
                result.Y += center.Y;
                result.Z += center.Z;
            }
            return result;
        }
        
        public double GetAngle2d(Vertex3D p1, Vertex3D p2)
        {
            var vp1 = new Vertex3D { X = p2.X - p1.X, Y = p2.Y - p1.Y, Z = 0 };
            var vp2 = new Vertex3D();
            var vp3 = new Vertex3D { X = 25 };

            double angle = AngleBetweenConnectedVectors(vp1, vp2, vp3);

            if (vp1.Y < 0)
            {
                return -(180 - angle);
            }

            return 180 - angle;
        }
        
        public double AngleBetweenConnectedVectors(Vertex3D p1, Vertex3D p2, Vertex3D p3)
        {
            double tb3 = 0;
            double a = Length3d(p1, p2);
            double b = Length3d(p2, p3);
            double dc = a * b;
            if (Math.Abs(dc) < double.Epsilon)
            {
                return -1;
            }
            double nc = (p2.X - p1.X) * (p3.X - p2.X);
            nc += (p2.Y - p1.Y) * (p3.Y - p2.Y);
            nc += (p2.Z - p1.Z) * (p3.Z - p2.Z);
            double ic = nc / dc;

            if ((ic <= -1) || (ic >= 1))
            {
                if (ic <= -1)
                {
                    tb3 = 180;
                }
                if (ic >= 1)
                {
                    tb3 = 0;
                }
            }
            else
            {
                a = Math.Sqrt((-ic * ic) + 1);
                if (Math.Abs(a) < double.Epsilon)
                {
                    return -1;
                }
                tb3 = 90 - ((1 / (Math.PI / 180)) * (Math.Atan(ic / a)));
            }

            return Math.Abs(tb3);
        }
        
        public Vertex3D ProjectPointAlongVector(Vertex3D p1, Vertex3D p2, double distance)
        {
            var result = new Vertex3D { X = p1.X, Y = p1.Y, Z = p1.Z };
            if (Math.Abs(distance) >= double.Epsilon)
            {
                double x = p1.X - p2.X;
                double y = p1.Y - p2.Y;
                double z = p1.Z - p2.Z;
                double q = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2)) - distance;
                double denom = distance + q;

                if (Math.Abs(denom) >= double.Epsilon)
                {
                    result.X = (q * p1.X + distance * p2.X) / denom;
                    result.Y = (q * p1.Y + distance * p2.Y) / denom;
                    result.Z = (q * p1.Z + distance * p2.Z) / denom;
                }
            }

            return result;
        }
        
        public Triangle CreateTriangleFromPoints(Vertex3D p1, Vertex3D p2, Vertex3D p3, bool ensureWindingOrder)
        {
            var triangle = new Triangle();
            triangle.P1 = new PolygonVertex(p1);
            triangle.P2 = new PolygonVertex(p2);
            triangle.P3 = new PolygonVertex(p3);

            if (ensureWindingOrder)
            {
                // Don't assume the winding is correct, because it's probably not for half the polys.
                // Check both directions, and take the normal with the end furthest from 0,0,0
                var normal1 =
                    vectorOperations.CalculateNormal(triangle.P1.Vertex, triangle.P2.Vertex,
                        triangle.P3.Vertex);
                var normalEnd1 = vectorOperations.Add(normal1, triangle.P1.Vertex);
                var dist1 = Length3d(normalEnd1, new Vertex3D());

                var normal2 =
                    vectorOperations.CalculateNormal(triangle.P3.Vertex, triangle.P2.Vertex,
                        triangle.P1.Vertex);
                var normalEnd2 = vectorOperations.Add(normal2, triangle.P1.Vertex);
                var dist2 = Length3d(normalEnd2, new Vertex3D());
                
                if (Math.Abs(dist2) > Math.Abs(dist1))
                {
                    triangle.Reverse();
                    triangle.Normal = normal2;
                }
                else
                {
                    triangle.Normal = normal1;
                }
            }
            else
            {
                triangle.Normal =
                    vectorOperations.CalculateNormal(triangle.P1.Vertex, triangle.P2.Vertex, triangle.P3.Vertex);
            }

            triangle.P1.Normal = triangle.Normal;
            triangle.P2.Normal = triangle.Normal;
            triangle.P3.Normal = triangle.Normal;

            return triangle;
        }  
    }
}