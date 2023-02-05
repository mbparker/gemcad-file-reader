using System;
using System.IO;
using System.Text;
using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models;

namespace LibGemcadFileReader.Concrete
{
    public class GemCadFileFormatIdentifier : IGemCadFileFormatIdentifier
    {
        private readonly IFileOperations fileOperations;

        public GemCadFileFormatIdentifier(IFileOperations fileOperations)
        {
            this.fileOperations = fileOperations;
        }

        public GemCadFileFormat Identify(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("A required argument is empty.", nameof(filename));
            }

            if (fileOperations.FileExists(filename))
            {
                using (var stream = fileOperations.CreateFileStream(filename, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.ASCII))
                    {
                        if (reader.BaseStream.Length > 7)
                        {
                            var bytes = reader.ReadBytes(7);
                            var str = Encoding.ASCII.GetString(bytes);
                            if (str == "GemCad ")
                            {
                                return GemCadFileFormat.Ascii;
                            }

                            return GemCadFileFormat.Binary;
                        }

                        throw new InvalidDataException("Cannot identify file as a GemCad data file.");
                    }
                }
            }

            throw new FileNotFoundException("The specified GemCad file does not exist.", filename);
        }
    }
}