using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{
    using System;
    using System.Collections.Generic;

    public class GeometryOperations : IGeometryOperations
    {
        public double Length3d(Vertex3D p1, Vertex3D p2)
        {
            double x = Math.Pow(p2.X - p1.X, 2);
            double y = Math.Pow(p2.Y - p1.Y, 2);
            double z = Math.Pow(p2.Z - p1.Z, 2);
            return Math.Sqrt(x + y + z);
        }

        public double TrueAngleBetweenVectors(Vertex3D p1, Vertex3D p2, Vertex3D p3)
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
    }
}