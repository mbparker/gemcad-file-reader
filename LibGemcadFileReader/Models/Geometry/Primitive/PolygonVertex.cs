using System.IO;

namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class PolygonVertex
    {
        private readonly Vertex3D normal;
        private readonly Vertex3D vertex;

        public PolygonVertex()
        {
            normal = new Vertex3D();
            vertex = new Vertex3D();
        }
        
        public PolygonVertex(Vertex3D point)
            : this()
        {
            Vertex = point;
        }        

        public Vertex3D Normal
        {
            get => normal;
            set => normal.Assign(value);
        }

        public Vertex3D Vertex
        {
            get => vertex;
            set => vertex.Assign(value);
        }

        public void Assign(object source)
        {
            if (source is PolygonVertex src)
            {
                Vertex = src.Vertex;
                Normal = src.Normal;
            }
        }

        public void Read(BinaryReader reader)
        {
            Vertex.Read(reader);
            Normal.Read(reader);
        }

        public void Write(BinaryWriter writer)
        {
            Vertex.Write(writer);
            Normal.Write(writer);
        }
    }
}