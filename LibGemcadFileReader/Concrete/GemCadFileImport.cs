using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models;
using LibGemcadFileReader.Models.Geometry;

namespace LibGemcadFileReader.Concrete
{
    public class GemCadFileImport : IGemCadFileImport
    {
        private readonly IGemCadFileFormatIdentifier formatIdentifier;
        private readonly IGemCadAscImport asciiImporter;
        private readonly IGemCadGemImport binaryImporter;

        public GemCadFileImport(
            IGemCadFileFormatIdentifier formatIdentifier,
            IGemCadAscImport asciiImporter,
            IGemCadGemImport binaryImporter)
        {
            this.formatIdentifier = formatIdentifier;
            this.asciiImporter = asciiImporter;
            this.binaryImporter = binaryImporter;
        }

        public GemCadFileData Import(string filename)
        {
            var format = formatIdentifier.Identify(filename);
            switch (format)
            {
                case GemCadFileFormat.Ascii:
                    return asciiImporter.Import(filename);
                default:
                    return binaryImporter.Import(filename);
            }
        }
    }
}