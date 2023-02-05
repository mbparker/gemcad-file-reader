using LibGemcadFileReader.Models;

namespace LibGemcadFileReader.Abstract
{
    public interface IGemCadFileFormatIdentifier
    {
        GemCadFileFormat Identify(string filename);
    }
}