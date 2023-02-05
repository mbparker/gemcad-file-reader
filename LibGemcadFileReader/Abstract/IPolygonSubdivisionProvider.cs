using System.Collections.Generic;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Abstract
{
    public interface IPolygonSubdivisionProvider
    {
        IReadOnlyList<Triangle> Subdivide(IReadOnlyList<Triangle> triangles, int iterations);
        IReadOnlyList<Triangle> Subdivide(IReadOnlyList<Quad> quads, int iterations);
    }
}