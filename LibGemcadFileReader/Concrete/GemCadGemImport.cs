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

        private void CalculateTierDefinitions(GemCadFileData data)
        {
            var origin = new Vertex3D();
            var stepAngle = 360.0 / data.Metadata.Gear;
            var rollAngleOffset = data.Metadata.GearLocationAngle * stepAngle;
            logger.Debug($"[GEMIMPORT] Gear: {data.Metadata.Gear} Step Angle: {stepAngle} Gear Location Angle: {data.Metadata.GearLocationAngle} Roll Offset Angle: {rollAngleOffset}");
            for (int i = 0; i < data.Tiers.Count; i++)
            {
                var tier = data.Tiers[i];
                for (int j = 0; j < tier.Indices.Count; j++)
                {
                    var index = tier.Indices[j];

                    if (j == 0)
                    {
                        double angle;
                        if (Math.Abs(index.FacetNormal.X) < double.Epsilon &&
                            Math.Abs(index.FacetNormal.Y) < double.Epsilon)
                        {
                            if (Math.Sign(index.FacetNormal.Z) > 0)
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
                            angle = MathUtils.FilterAngle(geometryOperations.AngleBetweenConnectedVectors(index.FacetNormal,
                                origin,
                                new Vertex3D(index.FacetNormal.X, index.FacetNormal.Y, 0)) - 90);
                            if (index.FacetNormal.Z < 0)
                            {
                                angle *= -1;
                            }
                        }

                        angle = Math.Round(angle, 2);

                        var triangle = geometryOperations.CreateTriangleFromPoints(index.Points[0], index.Points[1], index.Points[2], true);
                        var intersect = vectorOperations.FindRayPlaneIntersection(origin, index.FacetNormal, triangle);
                        var distance = geometryOperations.Length3d(origin, intersect);
                        
                        tier.Angle = angle;
                        tier.Distance = distance;
                        logger.Debug($"[GEMIMPORT] Tier {i}: ANGLE={angle} DIST={distance} PC={index.Points.Count}");
                    }

                    double indexAngle;
                    if (Math.Abs(index.FacetNormal.X) < double.Epsilon && Math.Abs(index.FacetNormal.Y) < double.Epsilon)
                    {
                        indexAngle = data.Metadata.Gear;
                    }
                    else
                    {
                        indexAngle =
                            (geometryOperations.GetAngle2d(origin,
                                new Vertex3D(index.FacetNormal.X, index.FacetNormal.Y, 0)) - 90 + rollAngleOffset) /
                            stepAngle * -1;
                        if (Math.Sign(indexAngle) < 0)
                        {
                            indexAngle += data.Metadata.Gear;
                        }
                    }

                    indexAngle = Math.Abs(MathUtils.ClockN(indexAngle, data.Metadata.Gear));
                    var indexVal = Convert.ToInt32(Math.Round(indexAngle));
                    index.Index = indexVal;

                    logger.Debug($"[GEMIMPORT] Tier {i} Index {j}: {indexVal}");
                }
            }
        }

        private void BuildModel(GemCadFileData data)
        {
            int startingTriangleCount = 0;
            int endingTriangleCount = 0;
            for (int i = 0; i < data.Tiers.Count; i++)
            {
                for (int j = 0; j < data.Tiers[i].Indices.Count; j++)
                {
                    var index = data.Tiers[i].Indices[j];
                    var triangles = ConvertCoplanarPointsToTriangles(index.Points);
                    if (triangles.Any())
                    {
                        startingTriangleCount += triangles.Count;
                        index.RenderingTriangles = subdivisionProvider.Subdivide(triangles, 1).ToList();
                        endingTriangleCount += index.RenderingTriangles.Count;
                    }
                }
            }
            
            logger.Debug(
                $"[GEMIMPORT] Subdivided {startingTriangleCount} triangles to {endingTriangleCount} triangles for rendering.");
        }

        private IReadOnlyList<Triangle> ConvertCoplanarPointsToTriangles(IReadOnlyList<Vertex3D> points)
        {
            var result = new List<Triangle>();
            for (int i = 1; i < points.Count - 1; i++)
            {
                var triangle = geometryOperations.CreateTriangleFromPoints(points[0], points[i], points[i + 1], true);
                result.Add(triangle);
            }

            return result;
        }

        private GemCadFileData ParseBinaryData(BinaryReader reader)
        {
            var currentTier = new GemCadFileTierData();
            currentTier.IsPreform = false;
            currentTier.Number = 1;
            var indexPoints = new List<Vertex3D>();
            var indices = new List<GemCadFileTierIndexData>();
            var parsedData = new GemCadFileData();
            var inPreform = false;

            while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
            {
                // This is pretty jank, but it works. Should find a better way of knowing were at the trailer section.
                var tempPos = reader.BaseStream.Position;
                var unknown1 = reader.ReadInt32(); // 0x0 marker
                var unknown2 = reader.ReadBytes(4); // Unknown - but never all zeroes
                var symmetryFolds = reader.ReadInt32();
                var symmetryMirror = reader.ReadInt32();
                if (unknown1 == 0 && unknown2.Sum(x => x) > 0 && symmetryFolds > 0 && (symmetryMirror == 0 || symmetryMirror == 1))
                {
                    logger.Debug(
                        $"[GEMIMPORT] Parsing trailer record at offset {reader.BaseStream.Position.ToString("X8")}");
                    logger.Debug($"[GEMIMPORT] Unknown2 = {BitConverter.ToString(unknown2)}");
                    parsedData.Metadata.SymmetryFolds = symmetryFolds;
                    parsedData.Metadata.SymmetryMirror = symmetryMirror != 0;
                    parsedData.Metadata.Gear = reader.ReadInt32();
                    parsedData.Metadata.RefractiveIndex = reader.ReadDouble();
                    var unknown3 = reader.ReadBytes(4); // Unknown - but seems to be same in all files
                    logger.Debug($"[GEMIMPORT] Unknown3 = {BitConverter.ToString(unknown3)}");
                    parsedData.Metadata.GearLocationAngle = reader.ReadDouble();

                    logger.Debug("[GEMIMPORT] Parsing trailer section text lines");
                    var textLines = parsedData.Metadata.Headers;
                    while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
                    {
                        var line = ReadAnsiString(reader, false);
                        if (!inPreform)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                if (string.Equals(line, "preform", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    logger.Debug("[GEMIMPORT] Detected PREFORM data");
                                    inPreform = true;
                                    break;
                                }

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
                }
                else
                {
                    reader.BaseStream.Position = tempPos;

                    logger.Debug(
                        $"[GEMIMPORT] Parsing tier index record at offset {reader.BaseStream.Position.ToString("X8")}");
                    var rec = new GemCadFileTierIndexData();
                    rec.FacetNormal = Read3DPoint(reader, out int eodMarker);
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
                        currentTier.IsPreform = inPreform;
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
                result = Encoding.ASCII.GetString(strBytes);
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