using System;
using System.Collections.Generic;
using System.IO;

namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class Polygon
    {
        private readonly Vertex3D normal;
        private readonly List<PolygonVertex> vertices;
        
        public Polygon()
        {
            normal = new Vertex3D();
            vertices = new List<PolygonVertex>();            
        }
        
        public Polygon(int vertexCount)
            : this()
        {
            vertices = new List<PolygonVertex>(vertexCount); 
            for (int i = 0; i < vertexCount; i++)
            {
                vertices.Add(new PolygonVertex());
            }
        }

        public string Tag { get; set; } = "{}";

        public Vertex3D Normal
        {
            get => normal;
            set => normal.Assign(value);
        }

        public IReadOnlyList<PolygonVertex> Vertices => vertices;

        protected virtual bool PointCountImmutable => false;

        public void Replace(IReadOnlyList<PolygonVertex> newVertices)
        {
            if (vertices.Count != newVertices.Count)
            {
                ThrowIfPointCountImmutable();
            }
            
            vertices.Clear();
            vertices.AddRange(newVertices);
        }

        public void RemoveAt(int index)
        {
            ThrowIfPointCountImmutable();
            vertices.RemoveAt(index);
        }
        
        public void Add(PolygonVertex newVertex)
        {
            ThrowIfPointCountImmutable();
            vertices.Add(newVertex);
        }

        public void Reverse()
        {
            vertices.Reverse();
        }

        public void Read(BinaryReader reader)
        {
            Tag = reader.ReadString();
            Normal.Read(reader);
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertices[i].Read(reader);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Tag);
            Normal.Write(writer);
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertices[i].Write(writer);
            }
        }

        private void ThrowIfPointCountImmutable()
        {
            if (PointCountImmutable)
            {
                throw new InvalidOperationException($"The number of points cannot change in a polygon of type {GetType().Name}");
            }
        }
    }
}