using System.Collections.Generic;
using System.Linq;
using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{
    public class PolygonSubdivisionProvider : IPolygonSubdivisionProvider
    {
        private readonly IGeometryOperations geometryOperations;
        private readonly IVectorOperations vectorOperations;

        public PolygonSubdivisionProvider(IGeometryOperations geometryOperations, IVectorOperations vectorOperations)
        {
            this.geometryOperations = geometryOperations;
            this.vectorOperations = vectorOperations;
        }

        public IReadOnlyList<Triangle> Subdivide(IReadOnlyList<Triangle> triangles, int iterations)
        {
            var result = triangles.ToList();
            for (int i = 1; i <= iterations; i++)
            {
                var triangleArray = result.ToArray();
                foreach (var triangle in triangleArray)
                {
                    var length1 = geometryOperations.Length3d(triangle.P1.Vertex, triangle.P2.Vertex);
                    var a = geometryOperations.ProjectPointAlongVector(triangle.P1.Vertex, triangle.P2.Vertex,
                        length1 / 2.0);
                    var length2 = geometryOperations.Length3d(triangle.P2.Vertex, triangle.P3.Vertex);
                    var b = geometryOperations.ProjectPointAlongVector(triangle.P2.Vertex, triangle.P3.Vertex,
                        length2 / 2.0);
                    var length3 = geometryOperations.Length3d(triangle.P3.Vertex, triangle.P1.Vertex);
                    var c = geometryOperations.ProjectPointAlongVector(triangle.P3.Vertex, triangle.P1.Vertex,
                        length3 / 2.0);

                    var newTriangles = new List<Triangle>(4);

                    var newTriangle = new Triangle();
                    newTriangle.P1.Vertex = triangle.P1.Vertex;
                    newTriangle.P2.Vertex = a;
                    newTriangle.P3.Vertex = c;
                    newTriangles.Add(newTriangle);

                    newTriangle = new Triangle();
                    newTriangle.P1.Vertex = a;
                    newTriangle.P2.Vertex = triangle.P2.Vertex;
                    newTriangle.P3.Vertex = b;
                    newTriangles.Add(newTriangle);

                    newTriangle = new Triangle();
                    newTriangle.P1.Vertex = c;
                    newTriangle.P2.Vertex = b;
                    newTriangle.P3.Vertex = triangle.P3.Vertex;
                    newTriangles.Add(newTriangle);

                    newTriangle = new Triangle();
                    newTriangle.P1.Vertex = a;
                    newTriangle.P2.Vertex = b;
                    newTriangle.P3.Vertex = c;
                    newTriangles.Add(newTriangle);
                    
                    result.Remove(triangle);
                    ComputeFaceNormals(newTriangles);
                    ComputeVertexNormals(newTriangles);
                    result.AddRange(newTriangles);
                }
            }

            return result.ToArray();
        }

        public IReadOnlyList<Triangle> Subdivide(IReadOnlyList<Quad> quads, int iterations)
        {
            return Subdivide(ConvertQuadsToTriangles(quads), iterations);
        }
        
        private IReadOnlyList<Triangle> ConvertQuadsToTriangles(IReadOnlyList<Quad> quads)
        {
            var result = new List<Triangle>();
            foreach (var quad in quads)
            {
                var triangles = new List<Triangle>(2);

                var triangle = new Triangle();
                triangle.P1.Vertex = quad.P1.Vertex;
                triangle.P2.Vertex = quad.P2.Vertex;
                triangle.P3.Vertex = quad.P3.Vertex;
                triangles.Add(triangle);

                triangle = new Triangle();
                triangle.P1.Vertex = quad.P1.Vertex;
                triangle.P2.Vertex = quad.P3.Vertex;
                triangle.P3.Vertex = quad.P4.Vertex;
                triangles.Add(triangle);
                
                result.AddRange(triangles);
            }

            return result.ToArray();
        }        

        private void ComputeFaceNormals(IEnumerable<Triangle> triangles)
        {
            foreach (var triangle in triangles)
            {
                triangle.Normal = vectorOperations.CalculateNormal(
                    triangle.P1.Vertex,
                    triangle.P2.Vertex,
                    triangle.P3.Vertex);
            }
        }

        private void ComputeVertexNormals(IEnumerable<Triangle> triangles)
        {
            var list = FindVertexFaceReferences(triangles);
            foreach (var item in list)
            {
                var normal = new Vertex3D();
                foreach (var polygon in item.Polygons)
                {
                    normal = vectorOperations.Add(normal, polygon.Normal);
                }

                normal = vectorOperations.Divide(normal, item.Polygons.Count);
                vectorOperations.Normalize(normal);
                foreach (var vertex in item.Vertices)
                {
                    vertex.Normal = normal;
                }
            }

            list.Clear();
        }

        private List<VertexSearchDataItem> FindVertexFaceReferences(IEnumerable<Triangle> triangles)
        {
            var dict = new Dictionary<string, VertexSearchDataItem>();

            foreach (var triangle in triangles)
            {
                foreach (var vertex in triangle.Vertices)
                {
                    var key = vertex.Vertex.ToSortString();
                    if (!dict.TryGetValue(key, out VertexSearchDataItem item))
                    {
                        item = new VertexSearchDataItem();
                        dict.Add(key, item);
                    }

                    item.Vertices.Add(vertex);
                    item.Polygons.Add(triangle);
                }
            }

            return dict.Values.ToList();
        }

        private class VertexSearchDataItem
        {
            public VertexSearchDataItem()
            {
                Vertices = new List<PolygonVertex>();                
                Polygons = new List<Polygon>();
            }

            public List<PolygonVertex> Vertices { get; }            

            public List<Polygon> Polygons { get; }
        }
    }
}