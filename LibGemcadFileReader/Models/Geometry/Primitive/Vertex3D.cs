using System;
using System.IO;

namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class Vertex3D
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public Vertex3D()
        {
        }
        
        public Vertex3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }        

        public T Clone<T>() where T : class, new()
        {
            if (typeof(T).Name != GetType().Name)
            {
                throw new InvalidOperationException($"Cannot clone a {GetType().Name} to a {typeof(T).Name}");
            }

            return Clone() as T;
        }

        public object Clone()
        {
            return new Vertex3D { X = X, Y = Y, Z = Z };
        }

        public void Assign(object source)
        {
            if (source is Vertex3D p)
            {
                X = p.X;
                Y = p.Y;
                Z = p.Z;
                return;
            }

            throw new InvalidOperationException($"Cannot assign a {source.GetType().Name} to a {GetType().Name}");
        }

        public override string ToString()
        {
            return $"{X};{Y};{Z}";
        }

        public string ToSortString()
        {
            return $"{X.ToString("F4")},{Y.ToString("F4")},{Z.ToString("F4")}";
        }

        public void Read(BinaryReader reader)
        {
            X = reader.ReadDouble();
            Y = reader.ReadDouble();
            Z = reader.ReadDouble();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }
    }
}