using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry;
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
        private readonly IPolygonSubdivisionProvider subdivisionProvider;
        private readonly ILoggerService logger;

        public GemCadGemImport(IFileOperations fileOperations, IVectorOperations vectorOperations,
            IGeometryOperations geometryOperations, IPolygonSubdivisionProvider subdivisionProvider, 
            ILoggerService logger)
        {
            this.fileOperations = fileOperations;
            this.vectorOperations = vectorOperations;
            this.geometryOperations = geometryOperations;
            this.subdivisionProvider = subdivisionProvider;
            this.logger = logger;
        }

        public GemCadFileData Import(string filename)
        {
            var data = ParseBinaryData(filename);
            CalculateTierDefinitions(data);
            BuildModel(data);
            return data;
        }

        private GemCadFileData ParseBinaryData(string filename)
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

        private void CalculateTierDefinitions(GemCadFileData parsedData)
        {
            var origin = new Vertex3D();
            var stepAngle = 360.0 / parsedData.Metadata.Gear;
            var rollAngleOffset = parsedData.Metadata.GearLocationAngle * stepAngle;
            logger.Debug($"[GEMIMPORT] Gear: {parsedData.Metadata.Gear} Step Angle: {stepAngle} Gear Location Angle: {parsedData.Metadata.GearLocationAngle} Roll Offset Angle: {rollAngleOffset}");
            for (int i = 0; i < parsedData.Tiers.Count; i++)
            {
                var tier = parsedData.Tiers[i];
                for (int j = 0; j < tier.Indices.Count; j++)
                {
                    var index = tier.Indices[j];

                    if (j == 0)
                    {
                        double angle;
                        if (Math.Abs(index.FacetPoint.X) < double.Epsilon &&
                            Math.Abs(index.FacetPoint.Y) < double.Epsilon)
                        {
                            if (Math.Sign(index.FacetPoint.Z) > 0)
                            {
                                angle = 0.0;
                            }
                            else
                            {
                                angle = -90.0;
                            }
                        }
                        else
                        {
                            angle = MathUtils.FilterAngle(geometryOperations.TrueAngleBetweenVectors(index.FacetPoint,
                                origin,
                                new Vertex3D(index.FacetPoint.X, index.FacetPoint.Y, 0)) - 90);
                            if (index.FacetPoint.Z < 0)
                            {
                                angle *= -1;
                            }
                        }

                        angle = Math.Round(angle, 2);
                        var distance = geometryOperations.Length3d(origin, index.FacetPoint);
                        
                        tier.Angle = angle;
                        tier.Distance = distance;
                        logger.Debug($"[GEMIMPORT] Tier {i}: ANGLE={angle} DIST={distance}");
                    }

                    double indexAngle;
                    if (Math.Abs(index.FacetPoint.X) < double.Epsilon && Math.Abs(index.FacetPoint.Y) < double.Epsilon)
                    {
                        indexAngle = parsedData.Metadata.Gear;
                    }
                    else
                    {
                        indexAngle =
                            (geometryOperations.GetAngle2d(origin,
                                new Vertex3D(index.FacetPoint.X, index.FacetPoint.Y, 0)) - 90 + rollAngleOffset) /
                            stepAngle * -1;
                        if (Math.Sign(indexAngle) < 0)
                        {
                            indexAngle += parsedData.Metadata.Gear;
                        }
                    }

                    var indexVal = Convert.ToInt32(Math.Round(indexAngle));
                    index.Index = indexVal;

                    logger.Debug($"[GEMIMPORT] Tier {i} Index {j}: {indexVal}");
                }
            }
        }

        private void BuildModel(GemCadFileData parsedData)
        {
            for (int i = 0; i < parsedData.Tiers.Count; i++)
            {
                for (int j = 0; j < parsedData.Tiers[i].Indices.Count; j++)
                {
                    var index = parsedData.Tiers[i].Indices[j];
                    for (int k = 1; k < index.Points.Count - 1; k++)
                    {
                        var triangle = new Triangle();
                        triangle.P1 = new PolygonVertex(index.Points[0]);
                        triangle.P2 = new PolygonVertex(index.Points[k]);
                        triangle.P3 = new PolygonVertex(index.Points[k + 1]);

                        // Don't assume the winding is correct, because it's probably not for half the polys.
                        // Check both directions, and take the normal with the end furthest from 0,0,0
                        var normal1 =
                            vectorOperations.CalculateNormal(triangle.P1.Vertex, triangle.P2.Vertex,
                                triangle.P3.Vertex);
                        var normalEnd1 = vectorOperations.Add(normal1, triangle.P1.Vertex);
                        var dist1 = geometryOperations.Length3d(normalEnd1, new Vertex3D());

                        var normal2 =
                            vectorOperations.CalculateNormal(triangle.P3.Vertex, triangle.P2.Vertex,
                                triangle.P1.Vertex);
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
                        parsedData.RenderingTriangles.Add(triangle);
                    }
                }
            }

            var startingCount = parsedData.RenderingTriangles.Count;
            parsedData.RenderingTriangles = subdivisionProvider.Subdivide(parsedData.RenderingTriangles, 1).ToList();
            logger.Debug(
                $"[GEMIMPORT] Subdivided {startingCount} triangles to {parsedData.RenderingTriangles.Count} triangles for rendering.");
        }

        private GemCadFileData ParseBinaryData(BinaryReader reader)
        {
            var currentTier = new GemCadFileTierData();
            currentTier.Number = 1;
            var indexPoints = new List<Vertex3D>();
            var indices = new List<GemCadFileTierIndexData>();
            var parsedData = new GemCadFileData();

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
                    parsedData.Metadata.Unknown1 = unknown1;
                    parsedData.Metadata.Unknown2 = unknown2;
                    parsedData.Metadata.Unknown3 = unknown3;
                    parsedData.Metadata.Unknown4 = unknown4;
                    parsedData.Metadata.Gear = reader.ReadInt32();
                    parsedData.Metadata.RefractiveIndex = reader.ReadDouble();
                    parsedData.Metadata.Unknown5 =
                        reader.ReadInt32(); // Unknown - but seems to be same in all files
                    parsedData.Metadata.GearLocationAngle = reader.ReadDouble();

                    logger.Debug("[GEMIMPORT] Parsing trailer section text lines");
                    var textLines = parsedData.Metadata.Headers;
                    while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
                    {
                        var line = ReadAnsiString(reader, false);
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logger.Debug($"[GEMIMPORT] Parsed trailer text line: {line}");
                            textLines.Add(line);
                        }
                        else
                        {
                            logger.Debug("[GEMIMPORT] Switching to footnotes section");
                            textLines = parsedData.Metadata.Footnotes;
                        }
                    }
                }
                else
                {
                    reader.BaseStream.Position = tempPos;

                    logger.Debug(
                        $"[GEMIMPORT] Parsing tier index record at offset {reader.BaseStream.Position.ToString("X8")}");
                    var rec = new GemCadFileTierIndexData();
                    rec.FacetPoint = Read3DPoint(reader, out int eodMarker);
                    rec.Tier = eodMarker;
                    var text = ReadAnsiString(reader, true).Split('\t');
                    if (text.Length > 0)
                    {
                        rec.Name = text[0].Trim();
                        if (text.Length > 1)
                        {
                            if (string.IsNullOrWhiteSpace(currentTier.CuttingInstructions))
                            {
                                currentTier.CuttingInstructions = string.Join("\t", text.Skip(1));
                            }
                        }
                    }

                    while (eodMarker > 0)
                    {
                        var pt = Read3DPoint(reader, out eodMarker);
                        indexPoints.Add(pt);
                    }
                    
                    if (currentTier.Number != rec.Tier)
                    {
                        currentTier.Indices = indices.ToList();
                        parsedData.Tiers.Add(currentTier);
                        indices.Clear();
                        currentTier = new GemCadFileTierData();
                        currentTier.Number = rec.Tier;
                    }

                    rec.Points = indexPoints.ToList();
                    indices.Add(rec);
                    indexPoints.Clear();
                }
            }

            if (indices.Any())
            {
                currentTier.Indices = indices.ToList();
                parsedData.Tiers.Add(currentTier);                
            }
            
            return parsedData;
        }

        private Vertex3D Read3DPoint(BinaryReader reader, out int eodMarker)
        {
            var result = new Vertex3D();
            result.X = reader.ReadDouble();
            result.Y = reader.ReadDouble();
            result.Z = reader.ReadDouble();
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
                result = Encoding.ASCII.GetString(strBytes).Trim();
            }

            if (checkMarker)
            {
                ReadEodMarker(reader);
            }
            else if (string.IsNullOrWhiteSpace(result))
            {
                if (reader.ReadByte() > 0)
                {
                    reader.BaseStream.Position--;
                }
            }            

            return result;
        }

        private int ReadEodMarker(BinaryReader reader)
        {
            return reader.ReadInt32();
        }
    }
}