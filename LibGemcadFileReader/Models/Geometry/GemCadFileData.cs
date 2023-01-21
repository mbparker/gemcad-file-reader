using System;
using System.Collections.Generic;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Models.Geometry
{
    public class GemCadFileData
    {
        public GemCadFileMetadata Metadata { get; set; } = new GemCadFileMetadata();
        public List<GemCadFileTierData> Tiers { get; set; } = new List<GemCadFileTierData>();
        public List<Polygon> FacetPolygons { get; set; } = new List<Polygon>();
        public List<Triangle> RenderingTriangles { get; set; } = new List<Triangle>();   
    }
}