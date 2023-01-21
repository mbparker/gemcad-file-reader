using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class Polygon : IEnumerable<PolygonVertex>
    {
        private readonly Vertex3D normal;
        private readonly List<PolygonVertex> vertices;

        public Polygon(int vertexCount)
        {
            normal = new Vertex3D();
            vertices = new List<PolygonVertex>(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                AddVertex(new PolygonVertex());
            }
        }

        public ulong Id { get; set; }

        public Vertex3D Normal
        {
            get => normal;
            set => normal.Assign(value);
        }

        public int VertexCount => vertices.Count;

        public PolygonVertex this[int index]
        {
            get => vertices[index];
            set => vertices[index].Assign(value);
        }

        public void Replace(IReadOnlyList<PolygonVertex> newVertices)
        {
            vertices.Clear();
            vertices.AddRange(newVertices);
        }

        public void RemoveAt(int index)
        {
            vertices.RemoveAt(index);
        }
        
        public void Add(PolygonVertex newVertex)
        {
            vertices.Add(newVertex);
        }

        public void Reverse()
        {
            vertices.Reverse();
        }

        public void Read(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Normal.Read(reader);
            for (int i = 0; i < vertices.Count; i++)
            {
                this[i].Read(reader);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            Normal.Write(writer);
            for (int i = 0; i < vertices.Count; i++)
            {
                this[i].Write(writer);
            }
        }

        public IEnumerator<PolygonVertex> GetEnumerator()
        {
            return vertices.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return vertices.GetEnumerator();
        }

        private void AddVertex(PolygonVertex vertex)
        {
            vertices.Add(vertex);
        }
    }
}