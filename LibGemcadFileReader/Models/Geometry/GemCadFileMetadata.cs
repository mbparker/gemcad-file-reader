using System.Collections.Generic;

namespace LibGemcadFileReader.Models.Geometry
{
    public class GemCadFileMetadata
    {
        public int Gear { get; set; }
        public double GearLocationAngle { get; set; }
        public double RefractiveIndex { get; set; }
        public int SymmetryFolds { get; set; }
        public bool SymmetryMirror { get; set; }
        public List<string> Headers { get; set; } = new List<string>();
        public List<string> Footnotes { get; set; } = new List<string>();
    }
}