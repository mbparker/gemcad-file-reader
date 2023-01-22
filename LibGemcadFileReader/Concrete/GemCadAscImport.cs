using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Models.Geometry;
using LibGemcadFileReader.Models.Geometry.Primitive;

namespace LibGemcadFileReader.Concrete
{
    public class GemCadAscImport : IGemCadAscImport
    {
        private const double Tolerance = 0.0000000001;

        private readonly IFileOperations fileOperations;
        private readonly IVectorOperations vectorOperations;
        private readonly IGeometryOperations geometryOperations;
        private readonly ILoggerService logger;

        public GemCadAscImport(IFileOperations fileOperations, IVectorOperations vectorOperations,
            IGeometryOperations geometryOperations, ILoggerService logger)
        {
            this.fileOperations = fileOperations;
            this.vectorOperations = vectorOperations;
            this.geometryOperations = geometryOperations;
            this.logger = logger;
        }

        public GemCadFileData Import(string filename)
        {
            var result = new GemCadFileData();
            using (var stream = fileOperations.CreateFileStream(filename, FileMode.Open))
            {
                using (var reader = new StreamReader(stream))
                {
                    int currentTier = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine()?.Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            ProcessLine(line, result, ref currentTier);
                        }
                    }

                    if (result.Tiers.Any())
                    {
                        BuildGeometry(result);
                    }
                }
            }

            return result;
        }

        private void ProcessLine(string line, GemCadFileData fileData, ref int currentTier)
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.None).Select(x => x.Trim())
                .ToArray();
            if (parts.Any())
            {
                if (parts[0] == "g")
                {
                    if (parts.Length == 3)
                    {
                        if (int.TryParse(parts[1], out int gear) &&
                            double.TryParse(parts[2], out double gearLocation))
                        {
                            logger.Debug($"[ASCIMPORT] Read gear and location {gear} {gearLocation}");
                            fileData.Metadata.Gear = gear;
                            fileData.Metadata.GearLocationAngle = gearLocation;
                        }
                    }
                }
                else if (parts[0] == "I")
                {
                    if (parts.Length == 2)
                    {
                        if (double.TryParse(parts[1], out double index))
                        {
                            logger.Debug($"[ASCIMPORT] Read index {index}");
                            fileData.Metadata.RefractiveIndex = index;
                        }
                    }
                }
                else if (parts[0] == "H")
                {
                    if (parts.Length > 1)
                    {
                        var header = string.Join(" ", parts.Skip(1));
                        logger.Debug($"[ASCIMPORT] Read header {header}");
                        fileData.Metadata.Headers.Add(header);
                    }
                }
                else if (parts[0] == "F")
                {
                    if (parts.Length > 1)
                    {
                        var footer = string.Join(" ", parts.Skip(1));
                        logger.Debug($"[ASCIMPORT] Read footnote {footer}");
                        fileData.Metadata.Footnotes.Add(footer);
                    }
                }
                else if (parts[0] == "a")
                {
                    if (parts.Length > 2)
                    {
                        if (double.TryParse(parts[1], out double angle) &&
                            double.TryParse(parts[2], out double distance))
                        {
                            if (parts.Length > 3)
                            {
                                var facetIndices = new List<Tuple<string, double>>();
                                var index = 3;
                                var currentTierIndex = double.NaN;
                                var currentCuttingInstructions = string.Empty;
                                while(index < parts.Length)
                                {
                                    if (parts[index] == "n")
                                    {
                                        if (!double.IsNaN(currentTierIndex))
                                        {
                                            facetIndices.Add(new Tuple<string, double>(parts[index + 1], currentTierIndex));
                                            currentTierIndex = double.NaN;
                                        }
                                        
                                        index += 2;
                                    }
                                    else if (double.TryParse(parts[index], out double facetIndex))
                                    {
                                        if (!double.IsNaN(currentTierIndex))
                                        {
                                            facetIndices.Add(new Tuple<string, double>(string.Empty, currentTierIndex));
                                        }
                                        
                                        currentTierIndex = facetIndex;
                                        index++;
                                    }
                                    else
                                    {
                                        if (!double.IsNaN(currentTierIndex))
                                        {
                                            facetIndices.Add(new Tuple<string, double>(string.Empty, currentTierIndex));
                                            currentTierIndex = double.NaN;
                                        }
                                        
                                        currentCuttingInstructions = string.Join(" ", parts.Skip(index));
                                        break;
                                    }
                                }
                                
                                if (!double.IsNaN(currentTierIndex))
                                {
                                    facetIndices.Add(new Tuple<string, double>(string.Empty, currentTierIndex));
                                }                                
                                
                                logger.Debug($"[ASCIMPORT] Read angle, distance, tier indices, and cutting instructions: {angle}, {distance}, {facetIndices.Count} indices, {currentCuttingInstructions}");
                                var tier = new GemCadFileTierData();
                                tier.Number = ++currentTier;
                                tier.Angle = angle;
                                tier.Distance = distance;
                                for (int i = 0; i < facetIndices.Count; i++)
                                {
                                    var tierIndex = new GemCadFileTierIndexData();
                                    tierIndex.Tier = currentTier;
                                    tierIndex.Name = facetIndices[i].Item1;
                                    tierIndex.Index = facetIndices[i].Item2;
                                    tier.Indices.Add(tierIndex);
                                }
                                
                                fileData.Tiers.Add(tier);
                            }
                        }
                    }
                }
            }
        }

        private void BuildGeometry(GemCadFileData fileData)
        {
            GenerateCutPlanes(fileData.Metadata.Gear, fileData.Tiers);
            fileData.FacetPolygons.AddRange(GenerateRoughCube());
            PerformCutsOnRoughCube(fileData.FacetPolygons, fileData.Tiers);
            ProcessPolygons(fileData);
        }

        private void GenerateCutPlanes(double gear, IReadOnlyList<GemCadFileTierData> tiers)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                var tier = tiers[i];
                double alpha = tier.Angle * Math.PI / 180.0;
                double distance = tier.Distance;
                int sg = Math.Sign(alpha);
                if (sg == 0) sg = 1;
                
                for (int j = 0; j < tier.Indices.Count; j++)
                {
                    double beta = tier.Indices[j].Index / gear * Math.PI * 2;
                    double x = distance * Math.Sin(alpha) * Math.Cos(beta);
                    double y = distance * Math.Sin(alpha) * Math.Sin(beta);
                    double z = sg * distance * Math.Cos(alpha);
                    tier.Indices[j].FacetPoint = new Vertex3D(x, y, z);
                }
            }
        }


        private List<Polygon> GenerateRoughCube()
        {
            const double L = 10;

            var result = new List<Polygon>();

            AddLeft(result, L);
            AddFront(result, L);
            AddRight(result, L);
            AddBack(result, L);
            AddTop(result, L);
            AddBottom(result, L);

            return result;
        }

        private void AddBack(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Quad();
            result.Add(quad);
            quad.P1.Vertex.X = -sizeDiv2;
            quad.P1.Vertex.Y = -sizeDiv2;
            quad.P1.Vertex.Z = -sizeDiv2;

            quad.P2.Vertex.X = -sizeDiv2;
            quad.P2.Vertex.Y = sizeDiv2;
            quad.P2.Vertex.Z = -sizeDiv2;

            quad.P3.Vertex.X = sizeDiv2;
            quad.P3.Vertex.Y = sizeDiv2;
            quad.P3.Vertex.Z = -sizeDiv2;

            quad.P4.Vertex.X = sizeDiv2;
            quad.P4.Vertex.Y = -sizeDiv2;
            quad.P4.Vertex.Z = -sizeDiv2;
        }

        private void AddBottom(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Quad();
            result.Add(quad);
            quad.P1.Vertex.X = -sizeDiv2;
            quad.P1.Vertex.Y = -sizeDiv2;
            quad.P1.Vertex.Z = sizeDiv2;

            quad.P2.Vertex.X = -sizeDiv2;
            quad.P2.Vertex.Y = -sizeDiv2;
            quad.P2.Vertex.Z = -sizeDiv2;

            quad.P3.Vertex.X = sizeDiv2;
            quad.P3.Vertex.Y = -sizeDiv2;
            quad.P3.Vertex.Z = -sizeDiv2;

            quad.P4.Vertex.X = sizeDiv2;
            quad.P4.Vertex.Y = -sizeDiv2;
            quad.P4.Vertex.Z = sizeDiv2;
        }

        private void AddFront(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Quad();
            result.Add(quad);
            quad.P4.Vertex.X = sizeDiv2;
            quad.P4.Vertex.Y = -sizeDiv2;
            quad.P4.Vertex.Z = sizeDiv2;

            quad.P3.Vertex.X = -sizeDiv2;
            quad.P3.Vertex.Y = -sizeDiv2;
            quad.P3.Vertex.Z = sizeDiv2;

            quad.P2.Vertex.X = -sizeDiv2;
            quad.P2.Vertex.Y = sizeDiv2;
            quad.P2.Vertex.Z = sizeDiv2;

            quad.P1.Vertex.X = sizeDiv2;
            quad.P1.Vertex.Y = sizeDiv2;
            quad.P1.Vertex.Z = sizeDiv2;
        }

        private void AddLeft(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Quad();
            result.Add(quad);
            quad.P4.Vertex.X = -sizeDiv2;
            quad.P4.Vertex.Y = -sizeDiv2;
            quad.P4.Vertex.Z = sizeDiv2;

            quad.P3.Vertex.X = -sizeDiv2;
            quad.P3.Vertex.Y = -sizeDiv2;
            quad.P3.Vertex.Z = -sizeDiv2;

            quad.P2.Vertex.X = -sizeDiv2;
            quad.P2.Vertex.Y = sizeDiv2;
            quad.P2.Vertex.Z = -sizeDiv2;

            quad.P1.Vertex.X = -sizeDiv2;
            quad.P1.Vertex.Y = sizeDiv2;
            quad.P1.Vertex.Z = sizeDiv2;
        }

        private void AddRight(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Quad();
            result.Add(quad);
            quad.P1.Vertex.X = sizeDiv2;
            quad.P1.Vertex.Y = -sizeDiv2;
            quad.P1.Vertex.Z = -sizeDiv2;

            quad.P2.Vertex.X = sizeDiv2;
            quad.P2.Vertex.Y = sizeDiv2;
            quad.P2.Vertex.Z = -sizeDiv2;

            quad.P3.Vertex.X = sizeDiv2;
            quad.P3.Vertex.Y = sizeDiv2;
            quad.P3.Vertex.Z = sizeDiv2;

            quad.P4.Vertex.X = sizeDiv2;
            quad.P4.Vertex.Y = -sizeDiv2;
            quad.P4.Vertex.Z = sizeDiv2;
        }

        private void AddTop(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Quad();
            result.Add(quad);
            quad.P4.Vertex.X = sizeDiv2;
            quad.P4.Vertex.Y = sizeDiv2;
            quad.P4.Vertex.Z = sizeDiv2;

            quad.P3.Vertex.X = -sizeDiv2;
            quad.P3.Vertex.Y = sizeDiv2;
            quad.P3.Vertex.Z = sizeDiv2;

            quad.P2.Vertex.X = -sizeDiv2;
            quad.P2.Vertex.Y = sizeDiv2;
            quad.P2.Vertex.Z = -sizeDiv2;

            quad.P1.Vertex.X = sizeDiv2;
            quad.P1.Vertex.Y = sizeDiv2;
            quad.P1.Vertex.Z = -sizeDiv2;
        }

        private void PerformCutsOnRoughCube(List<Polygon> polygons, IReadOnlyList<GemCadFileTierData> tiers)
        {
            Vertex3D point;

            for (int h = 0; h < tiers.Count; h++)
            {
                var tier = tiers[h];
                for (int i = 0; i < tier.Indices.Count; i++)
                {
                    var tierIndex = tier.Indices[i];

                    var cutPoints = new List<Vertex3D>();
                    var cutPolygon = new Polygon(0);

                    for (int j = 0; j < polygons.Count; j++)
                    {
                        var currentPolygon = polygons[j];
                        if (currentPolygon.VertexCount == 0)
                            continue;

                        var crossPoints = CutPolygonByPlane(currentPolygon, tierIndex);

                        for (int k = 0; k < crossPoints.Count; k++)
                        {
                            point = crossPoints[k];
                            bool alreadyExists = false;
                            for (int l = 0; l < cutPoints.Count; l++)
                            {
                                if (IsSamePoint(point, cutPoints[l]))
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }

                            if (!alreadyExists)
                            {
                                cutPoints.Add(new Vertex3D(point.X, point.Y, point.Z));
                            }
                        }
                    }

                    logger.Debug($"CutPoints = {cutPoints.Count}");
                    for (int j = 0; j < cutPoints.Count; j++)
                    {
                        point = cutPoints[j];
                        bool alreadyExists = false;
                        for (int k = 0; k < cutPolygon.VertexCount; k++)
                        {
                            if (IsSamePoint(point, cutPolygon[k].Vertex))
                            {
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (!alreadyExists)
                        {
                            var vertex = new Vertex3D(point.X, point.Y, point.Z);
                            cutPolygon.Add(new PolygonVertex(vertex));
                        }
                    }

                    if (cutPolygon.VertexCount > 2)
                    {
                        ReArrangePoints(cutPolygon);
                        polygons.Add(cutPolygon);
                    }
                }
            }
        }

        private List<Vertex3D> CutPolygonByPlane(Polygon pg, GemCadFileTierIndexData tierIndex)
        {
            var crossPt = new List<Vertex3D>();
            Vertex3D g0, g1, p;
            double cx, cy, cz;
            p = tierIndex.FacetPoint.Clone<Vertex3D>();
            double delta, d;
            int i = 0;

            logger.Debug($"Polygon Point Count={pg.VertexCount}");

            while (true)
            {
                g0 = pg[i].Vertex;
                if (i == pg.VertexCount - 1)
                {
                    g1 = pg[0].Vertex;
                }
                else
                {
                    g1 = pg[i + 1].Vertex;
                }

                d = p.X * (g1.X - g0.X) + p.Y * (g1.Y - g0.Y) + p.Z * (g1.Z - g0.Z);

                if (Math.Abs(d) > Tolerance)
                {
                    delta = (p.X * (p.X - g0.X) + p.Y * (p.Y - g0.Y) + p.Z * (p.Z - g0.Z)) / d;
                    if (Math.Abs(delta) < Tolerance)
                        delta = 0;

                    cx = g0.X + (g1.X - g0.X) * delta;
                    cy = g0.Y + (g1.Y - g0.Y) * delta;
                    cz = g0.Z + (g1.Z - g0.Z) * delta;

                    if (delta >= 0 && delta <= 1)
                    {
                        var pt = new Vertex3D(cx, cy, cz);
                        bool alreadyExists = false;
                        for (int j = 0; j < crossPt.Count; j++)
                        {
                            if (IsSamePoint(pt, crossPt[j]))
                            {
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (!alreadyExists)
                        {
                            logger.Debug(
                                $"Cross Point: {cx},{cy},{cz}  g0: {g0.X},{g0.Y},{g0.Z}  g1: {g1.X},{g1.Y},{g1.Z}  delta: {delta}");
                            crossPt.Add(pt);
                        }
                    }
                }

                i++;
                if (i == pg.VertexCount)
                {
                    break;
                }
            }

            i = pg.VertexCount - 1;

            while (true)
            {
                g1 = pg[i].Vertex;

                d = p.X * (g1.X - p.X) + p.Y * (g1.Y - p.Y) + p.Z * (g1.Z - p.Z);

                if (d > 0)
                {
                    pg.RemoveAt(i);
                }

                i--;
                if (i == -1)
                    break;
            }

            for (i = 0; i < crossPt.Count; i++)
            {
                p = crossPt[i];
                bool alreadyExists = false;
                for (int j = 0; j < pg.VertexCount; j++)
                {
                    if (IsSamePoint(p, pg[j].Vertex))
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    pg.Add(new PolygonVertex(new Vertex3D(p.X, p.Y, p.Z)));
                }
            }

            if (pg.VertexCount > 3)
                ReArrangePoints(pg);
            return crossPt;
        }

        private void ProcessPolygons(GemCadFileData fileData)
        {
            for (int i = 0; i < fileData.FacetPolygons.Count; i++)
            {
                logger.Debug(
                    $"[ASCIMPORT] Convert polygon to triangle - Point Count: {fileData.FacetPolygons[i].VertexCount}");
                for (int j = 1; j < fileData.FacetPolygons[i].VertexCount - 1; j++)
                {
                    var triangle = new Triangle();
                    triangle.P1 = fileData.FacetPolygons[i][0];
                    triangle.P2 = fileData.FacetPolygons[i][j];
                    triangle.P3 = fileData.FacetPolygons[i][j + 1];

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

                    logger.Debug($"[ASCIMPORT] N1D={dist1} N2D={dist2}");

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
                    fileData.RenderingTriangles.Add(triangle);
                }
            }
        }

        private void ReArrangePoints(Polygon polygon)
        {
            var vertices = polygon.ToList();

            if (vertices.Count() < 4)
                return;

            int i, j, maxIndex = 0;
            Vertex3D p0 = vertices[0].Vertex;
            Vertex3D p1, p2;

            Vertex3D g0, g1;
            List<Vertex3D> lstPoints = new List<Vertex3D>();
            for (i = 0; i < vertices.Count(); i++)
                lstPoints.Add(new Vertex3D());
            lstPoints[0] = p0;

            double angle, maxAngle;
            List<double> dAngles = new List<double>();
            dAngles.Add(0);

            for (i = 1; i < vertices.Count(); i++)
            {
                p1 = vertices[i].Vertex;
                maxAngle = 0;
                for (j = 1; j < vertices.Count(); j++)
                {
                    if (i != j)
                    {
                        p2 = vertices[j].Vertex;

                        g0 = new Vertex3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
                        g1 = new Vertex3D(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

                        angle = AngleBetween(g0, g1);

                        if (maxAngle < angle)
                            maxAngle = angle;
                    }
                }

                dAngles.Add(maxAngle);
            }

            maxAngle = 0;

            for (i = 1; i < dAngles.Count(); i++)
            {
                if (maxAngle < dAngles[i])
                {
                    maxAngle = dAngles[i];
                    maxIndex = i;
                }
            }

            p1 = vertices[maxIndex].Vertex;
            lstPoints[1] = p1;
            dAngles.Clear();
            dAngles.Add(-1);

            for (i = 1; i < vertices.Count(); i++)
            {
                p2 = vertices[i].Vertex;
                g0 = new Vertex3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
                g1 = new Vertex3D(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

                angle = AngleBetween(g0, g1);
                dAngles.Add(angle);
            }

            int nLows;

            for (i = 1; i < dAngles.Count(); i++)
            {
                if (i != maxIndex)
                {
                    nLows = 0;
                    for (j = 0; j < dAngles.Count(); j++)
                    {
                        if (dAngles[j] < dAngles[i])
                            nLows++;
                    }

                    lstPoints[nLows] = vertices[i].Vertex;
                }
            }

            polygon.Replace(lstPoints.Select(x => new PolygonVertex(x)).ToArray());
        }

        private double AngleBetween(Vertex3D p1, Vertex3D p2)
        {
            return vectorOperations.AngleBetween(p1.Clone<Vertex3D>(), p2.Clone<Vertex3D>());
        }

        private bool IsSamePoint(Vertex3D pt1, Vertex3D pt2)
        {
            bool bIsSame = false;
            double dis = Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2.0) +
                                   Math.Pow(pt1.Y - pt2.Y, 2.0) +
                                   Math.Pow(pt1.Z - pt2.Z, 2.0));
            if (dis < Tolerance)
                bIsSame = true;

            return bIsSame;
        }
    }
}