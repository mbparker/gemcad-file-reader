using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{

    /*
     Compear125.gem
     
     RECORD STRUCTURE - 1 per tier index
     -----------------------------------
     [00000000] 24 bytes - X,Y,Z coordinate?
     [00000018] 4 bytes - 0x1 Unknown marker
     [0000001C] 1 byte - Note length
     [0000001D] X bytes - Note string (length indicated by the preceding byte field)
     [0000002F] 4 bytes - 0x1 Unknown marker
     [00000033] 24 bytes - X,Y,Z coordinate?
     [0000004B] 4 bytes - 0x1 Unknown marker
     [0000004F] 24 bytes - X,Y,Z coordinate?
     [00000067] 4 bytes - 0x1 Unknown marker
     [0000006B] 24 bytes - X,Y,Z coordinate?
     [00000083] 4 bytes - 0x1 Unknown marker
     [00000087] 24 bytes - X,Y,Z coordinate?
     [0000009F] 4 bytes - 0x0 End of record marker
     
     [000000A3] 24 bytes - X,Y,Z coordinate?
     [000000BB] 4 bytes - 0x1 Unknown marker
     [000000BF] 1 byte - Note length
     [000000C0] X bytes - Note string (length indicated by the preceding byte field)
     [000000D2] 4 bytes - 0x1 Unknown marker
     [000000D6] 24 bytes - X,Y,Z coordinate?
     [000000EE] 4 bytes - 0x1 Unknown marker
     [000000F2] 24 bytes - X,Y,Z coordinate?
     [0000010A] 4 bytes - 0x1 Unknown marker
     [0000010E] 24 bytes - X,Y,Z coordinate?
     [00000126] 4 bytes - 0x1 Unknown marker
     [0000012A] 24 bytes - X,Y,Z coordinate?
     [00000142] 4 bytes - 0x0 End of record marker           
     
     ...
     
     TRAILER RECORD STRUCTURE
     ------------------------
     [00002E4F] 4 bytes - 0x0 End of data records marker
     [00002E53] 4 Bytes - Unknown
     [00002E57] 4 bytes - 0x1 Unknown marker
     [00002E5B] 4 bytes - 0x1 Unknown marker
     [00002E5F] 4 Bytes - Gear (96)
     [00002E63] 8 Bytes - Index (1.54)
     [00002E6B] 4 Bytes - Unknown (seems to always be 32767 - FF7F0000)
     [00002E6F] 8 bytes - Gear Location Angle (0.0)
     
     REPEAT UNTIL 0x0 or EOF - Last string is the Footer note
     --------------------------------------------------------
     [00002E77] 1 byte - Header note length
     [00002E78] X bytes - Header note string (length indicated by the preceding byte field)           
     */

    public class GemCadGemImport : IGemCadGemImport
    {
        private readonly IFileOperations fileOperations;
        private readonly IVectorOperations vectorOperations;
        private readonly IGeometryOperations geometryOperations;
        private readonly ILoggerService logger;

        public GemCadGemImport(IFileOperations fileOperations, IVectorOperations vectorOperations,
            IGeometryOperations geometryOperations, ILoggerService logger)
        {
            this.fileOperations = fileOperations;
            this.vectorOperations = vectorOperations;
            this.geometryOperations = geometryOperations;
            this.logger = logger;
        }

        public PolygonContainer Import(string filename)
        {
            var data = ParseBinaryData(filename);
            return BuildModel(data);
        }

        private GemCadBinaryFileData ParseBinaryData(string filename)
        {
            logger.Debug($"[GEMIMPORT] Begin parsing Gemcad GEM file: {filename}");
            using (var stream = fileOperations.CreateFileStream(filename, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    return ParseBinaryData(reader);
                }
            }
        }

        private PolygonContainer BuildModel(GemCadBinaryFileData parsedData)
        {
            var result = new PolygonContainer();

            for (int i = 0; i < parsedData.TierIndexRecords.Count; i++)
            {
                for (int j = 1; j < parsedData.TierIndexRecords[i].Points.Count - 1; j++)
                {
                    var triangle = new Triangle();
                    triangle.P1 = new PolygonVertex(parsedData.TierIndexRecords[i].Points[0]);
                    triangle.P2 = new PolygonVertex(parsedData.TierIndexRecords[i].Points[j]);
                    triangle.P3 = new PolygonVertex(parsedData.TierIndexRecords[i].Points[j + 1]);

                    // Don't assume the winding is correct, because it's probably not for half the polys.
                    // Check both directions, and take the normal with the end furthest from 0,0,0
                    var normal1 =
                        vectorOperations.CalculateNormal(triangle.P1.Vertex, triangle.P2.Vertex, triangle.P3.Vertex);
                    var normalEnd1 = vectorOperations.Add(normal1, triangle.P1.Vertex);
                    var dist1 = geometryOperations.Length3d(normalEnd1, new Vertex3D());

                    var normal2 =
                        vectorOperations.CalculateNormal(triangle.P3.Vertex, triangle.P2.Vertex, triangle.P1.Vertex);
                    var normalEnd2 = vectorOperations.Add(normal2, triangle.P1.Vertex);
                    var dist2 = geometryOperations.Length3d(normalEnd2, new Vertex3D());

                    //logger.Debug($"[GEMIMPORT] N1D={dist1} N2D={dist2}");

                    if (Math.Abs(dist2) > Math.Abs(dist1))
                    {
                        triangle.Reverse();
                        triangle.Normal = normal2;
                    }
                    else
                    {
                        triangle.Normal = normal1;
                    }

                    triangle.P1.Normal = triangle.Normal;
                    triangle.P2.Normal = triangle.Normal;
                    triangle.P3.Normal = triangle.Normal;
                    result.Add(triangle);
                }
            }

            return result;
        }

        private GemCadBinaryFileData ParseBinaryData(BinaryReader reader)
        {
            var parsedData = new GemCadBinaryFileData();

            while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
            {
                // This is pretty jank, but it works. Should find a better way of knowing were at the trailer section.
                var tempPos = reader.BaseStream.Position;
                var unknown1 = reader.ReadInt32(); // 0x0 marker
                var unknown2 = reader.ReadBytes(4); // Unknown - no clue
                var unknown3 = reader.ReadInt32(); // 0x? > 0 marker
                var unknown4 = reader.ReadInt32(); // 0x1 marker
                if (unknown1 == 0 && unknown3 > 0 && unknown4 == 1)
                {
                    logger.Debug(
                        $"[GEMIMPORT] Parsing trailer record at offset {reader.BaseStream.Position.ToString("X8")}");
                    logger.Debug($"[GEMIMPORT] Unknown2 = {BitConverter.ToString(unknown2)}");
                    parsedData.TrailerSection.Unknown1 = unknown1;
                    parsedData.TrailerSection.Unknown2 = unknown2;
                    parsedData.TrailerSection.Unknown3 = unknown3;
                    parsedData.TrailerSection.Unknown4 = unknown4;
                    parsedData.TrailerSection.Gear = reader.ReadInt32();
                    parsedData.TrailerSection.RIndex = ValidateDouble(reader.ReadDouble());
                    parsedData.TrailerSection.Unknown5 =
                        reader.ReadInt32(); // Unknown - but seems to be same in all files
                    parsedData.TrailerSection.GearLocation = ValidateDouble(reader.ReadDouble());

                    logger.Debug("[GEMIMPORT] Parsing trailer section text lines");
                    var textLines = new List<string>();
                    while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
                    {
                        var line = ReadAnsiString(reader, false)?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            textLines.Add(line);
                        }
                    }

                    if (textLines.Any())
                    {
                        if (textLines.Count > 1)
                        {
                            parsedData.TrailerSection.Headers = textLines.Take(textLines.Count - 1).ToArray();
                            parsedData.TrailerSection.Footer = textLines.Last();
                        }
                        else
                        {
                            parsedData.TrailerSection.Headers = textLines.ToArray();
                        }
                    }
                }
                else
                {
                    reader.BaseStream.Position = tempPos;

                    logger.Debug(
                        $"[GEMIMPORT] Parsing tier index record at offset {reader.BaseStream.Position.ToString("X8")}");
                    var rec = new TierIndexRecord();
                    rec.Normal = Read3DPoint(reader, out int eodMarker);
                    rec.Tier = eodMarker;
                    var offset = reader.BaseStream.Position;
                    var text = ReadAnsiString(reader, true).Split('\t');
                    if (text.Length > 0)
                    {
                        rec.Name = text[0].Trim();
                        if (text.Length > 1)
                        {
                            rec.CuttingInstructions = text[1].Trim();
                            if (text.Length > 2)
                            {
                                logger.Warn(
                                    $"Ignoring {text.Length - 2} string element(s) at offset {offset.ToString("X8")}: {string.Join("\t", text.Skip(2))}");
                            }
                        }
                    }

                    while (eodMarker > 0)
                    {
                        var pt = Read3DPoint(reader, out eodMarker);
                        rec.Points.Add(pt);
                    }

                    parsedData.TierIndexRecords.Add(rec);
                }
            }
            
            return parsedData;
        }

        private Vertex3D Read3DPoint(BinaryReader reader, out int eodMarker)
        {
            var result = new Vertex3D();
            result.X = ValidateDouble(reader.ReadDouble());
            result.Y = ValidateDouble(reader.ReadDouble());
            result.Z = ValidateDouble(reader.ReadDouble());
            eodMarker = ReadEodMarker(reader);
            return result;
        }

        private string ReadAnsiString(BinaryReader reader, bool checkMarker)
        {
            string result = string.Empty;
            var strLen = reader.ReadByte();
            if (strLen > 0)
            {
                var strBytes = reader.ReadBytes(strLen);
                result = Encoding.ASCII.GetString(strBytes);
            }

            if (checkMarker)
            {
                ReadEodMarker(reader);
            }

            return result;
        }

        private double ValidateDouble(double value)
        {
            // Ugh - Surely there's a better way to handle this.
            return value.ToString().Contains('E') ? 0.0 : value;
        }

        private int ReadEodMarker(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        private class GemCadBinaryFileData
        {
            public List<TierIndexRecord> TierIndexRecords { get; set; } = new List<TierIndexRecord>();
            public HeaderAndFooterRecord TrailerSection { get; set; } = new HeaderAndFooterRecord();
        }

        private class TierIndexRecord
        {
            public int Tier { get; set; }
            public Vertex3D Normal { get; set; } = new Vertex3D();
            public List<Vertex3D> Points { get; set; } = new List<Vertex3D>();
            public string Name { get; set; } = string.Empty;
            public string CuttingInstructions { get; set; } = string.Empty;
        }

        private class HeaderAndFooterRecord
        {
            public int Unknown1 { get; set; }
            public byte[] Unknown2 { get; set; }
            public int Unknown3 { get; set; }
            public int Unknown4 { get; set; }
            public int Unknown5 { get; set; }
            public int Gear { get; set; }
            public double GearLocation { get; set; }
            public double RIndex { get; set; }
            public string[] Headers { get; set; }
            public string Footer { get; set; }
        }
    }
}