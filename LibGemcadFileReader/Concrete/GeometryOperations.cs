using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{
    using System;

    public class GeometryOperations : IGeometryOperations
    {
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
    }
}