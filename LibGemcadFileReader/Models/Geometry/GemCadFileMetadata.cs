using System;
using System.Collections.Generic;

namespace LibGemcadFileReader.Models.Geometry
{
    public class GemCadFileMetadata
    {
        public int Gear { get; set; }
        public double GearLocationAngle { get; set; }
        public double Index { get; set; }
        public List<string> Headers { get; set; } = new List<string>();
        public string Footer { get; set; }
            
        // These fields are only applicable when parsing the binary format files (*.GEM)
        public int Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; } = Array.Empty<byte>();
        public int Unknown3 { get; set; }
        public int Unknown4 { get; set; }
        public int Unknown5 { get; set; }  
    }
}