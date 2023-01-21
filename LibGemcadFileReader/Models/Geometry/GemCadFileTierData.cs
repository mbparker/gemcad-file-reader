using System;
using System.Collections.Generic;

namespace LibGemcadFileReader.Models.Geometry
{
    public class GemCadFileTierData
    {
        public int Number { get; set; }
        public double Angle { get; set; }
        public double Distance { get; set; }
        public string CuttingInstructions { get; set; }
        public List<GemCadFileTierIndexData> Indices { get; set; } = new List<GemCadFileTierIndexData>();
    }
}