using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class PolygonContainer
    {
        private readonly Dictionary<ulong, Polygon> polygons;
        private readonly Dictionary<ulong, Triangle> triangles;
        private readonly Dictionary<ulong, Quad> quads;

        private ulong lastId;
        private uint cachedTotalCount;

        public PolygonContainer()
        {
            polygons = new Dictionary<ulong, Polygon>();
            triangles = new Dictionary<ulong, Triangle>();
            quads = new Dictionary<ulong, Quad>();
        }

        public uint Id { get; set; }

        public uint LayerId { get; set; }

        public uint MaterialId { get; set; }

        public bool UseVertexNormals { get; set; }

        public IEnumerable<Polygon> Polygons => polygons.Values;

        public IEnumerable<Triangle> Triangles => triangles.Values;

        public IEnumerable<Quad> Quads => quads.Values;

        public uint TotalCount
        {
            get
            {
                if (cachedTotalCount == 0)
                {
                    cachedTotalCount = (uint)triangles.Count;
                    cachedTotalCount += (uint)quads.Count;
                    cachedTotalCount += (uint)polygons.Count;
                }

                return cachedTotalCount;
            }
        }

        public void Remove(Polygon polygon)
        {
            switch (polygon.VertexCount)
            {
                case 3:
                    triangles.Remove(polygon.Id);
                    break;
                case 4:
                    quads.Remove(polygon.Id);
                    break;
                default:
                    polygons.Remove(polygon.Id);
                    break;
            }
        }

        public void Add(Polygon polygon)
        {
            polygon.Id = ++lastId;

            switch (polygon.VertexCount)
            {
                case 3:
                    triangles.Add(polygon.Id, polygon as Triangle);
                    break;
                case 4:
                    quads.Add(polygon.Id, polygon as Quad);
                    break;
                default:
                    polygons.Add(polygon.Id, polygon);
                    break;
            }

            cachedTotalCount = 0;
        }

        public void AddRange(IEnumerable<Polygon> polygonsToAdd)
        {
            foreach (var polygon in polygonsToAdd)
            {
                Add(polygon);
            }
        }

        public void Clear()
        {
            triangles.Clear();
            quads.Clear();
            polygons.Clear();
            cachedTotalCount = 0;
            lastId = 0;
        }

        public IEnumerable<Polygon> GetAll()
        {
            foreach (var triangle in triangles.Values)
            {
                yield return triangle;
            }

            foreach (var quad in quads.Values)
            {
                yield return quad;
            }

            foreach (var polygon in polygons.Values)
            {
                yield return polygon;
            }
        }

        public void Read(BinaryReader reader)
        {
            Id = reader.ReadUInt32();
            LayerId = reader.ReadUInt32();
            MaterialId = reader.ReadUInt32();
            UseVertexNormals = reader.ReadBoolean();
            lastId = reader.ReadUInt64();

            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var triangle = new Triangle();
                triangle.Read(reader);
                triangles.Add(triangle.Id, triangle);
            }
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var quad = new Quad();
                quad.Read(reader);
                quads.Add(quad.Id, quad);
            }
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var vertexCount = reader.ReadInt32();
                var poly = new Polygon(vertexCount);
                poly.Read(reader);
                polygons.Add(poly.Id, poly);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(LayerId);
            writer.Write(MaterialId);
            writer.Write(UseVertexNormals);
            writer.Write(lastId);

            var triList = Triangles.ToList();
            writer.Write(triList.Count);
            for (int i = 0; i < triList.Count; i++)
            {
                triList[i].Write(writer);
            }

            var quadList = Quads.ToList();
            writer.Write(quadList.Count);
            for (int i = 0; i < quadList.Count; i++)
            {
                quadList[i].Write(writer);
            }

            var polyList = Polygons.ToList();
            writer.Write(polyList.Count);
            for (int i = 0; i < polyList.Count; i++)
            {
                writer.Write(polyList[i].VertexCount);
                polyList[i].Write(writer);
            }
        }
    }
}