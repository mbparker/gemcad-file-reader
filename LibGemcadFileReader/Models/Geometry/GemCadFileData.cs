using System.Collections.Generic;

namespace LibGemcadFileReader.Models.Geometry
{
    public class GemCadFileData
    {
        public GemCadFileMetadata Metadata { get; set; } = new GemCadFileMetadata();
        public List<GemCadFileTierData> Tiers { get; set; } = new List<GemCadFileTierData>();
    }
}