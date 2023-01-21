using System;
using System.Collections.Generic;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Models.Geometry
{
    public class GemCadFileTierIndexData
    {
        public int Tier { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Index { get; set; } = double.NaN;
        public string CuttingInstructions { get; set; } = string.Empty;
        public Vertex3D FacetPoint { get; set; } = new Vertex3D();
        public Vertex3D FacetNormal { get; set; } = new Vertex3D();
        public List<Vertex3D> Points { get; set; } = new List<Vertex3D>();
    }
}