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
        private readonly IFileOperations fileOperations;
        private readonly IVectorOperations vectorOperations;
        private readonly IGeometryOperations geometryOperations;
        private readonly IPolygonSubdivisionProvider subdivisionProvider;
        private readonly ILoggerService logger;

        public GemCadAscImport(IFileOperations fileOperations, IVectorOperations vectorOperations,
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
                } else if (parts[0] == "y")
                {
                    if (parts.Length == 3)
                    {
                        if (int.TryParse(parts[1], out int symmetryFolds))
                        {
                            string symmetryMirror = parts[2].Trim();
                            logger.Debug($"[ASCIMPORT] Read symmetry {symmetryFolds} {symmetryMirror}");
                            fileData.Metadata.SymmetryFolds = symmetryFolds;
                            fileData.Metadata.SymmetryMirror = string.Equals(symmetryMirror, "y", StringComparison.InvariantCultureIgnoreCase);
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
            GenerateCutPlanes(fileData.Metadata.Gear, fileData.Metadata.GearLocationAngle, fileData.Tiers);
            var roughCube = new List<Polygon>(GenerateRoughCube());
            PerformCutsOnRoughCube(roughCube, fileData.Tiers);
            ProcessPolygons(fileData);
        }

        private void GenerateCutPlanes(double gear, double gearAngle, IReadOnlyList<GemCadFileTierData> tiers)
        {
            var origin = new Vertex3D();
            var stepAngle = 360.0 / gear;
            var rollAngleOffset = gearAngle * stepAngle;
            for (int i = 0; i < tiers.Count; i++)
            {
                var tier = tiers[i];
                for (int j = 0; j < tier.Indices.Count; j++)
                {
                    var pt = geometryOperations.ProjectPoint(origin, tier.Distance, 90);
                    var pitchAngle = MathUtils.FilterAngle(Math.Abs(tier.Angle) - 90);
                    if (Math.Sign(tier.Angle) < 0)
                    {
                        pitchAngle *= -1;
                    }
                    var rollAngle = tier.Indices[j].Index * stepAngle;
                    pt = geometryOperations.RotatePoint(pt, 0, 0, pitchAngle, origin);
                    pt = geometryOperations.RotatePoint(pt, 0,  MathUtils.FilterAngle(rollAngle - rollAngleOffset), 0, origin);
                    tier.Indices[j].FacetNormal = geometryOperations.ProjectPointAlongVector(pt, origin, -3.0);
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
            var quad = new Polygon(4);
            result.Add(quad);
            quad.Vertices[0].Vertex.X = -sizeDiv2;
            quad.Vertices[0].Vertex.Y = -sizeDiv2;
            quad.Vertices[0].Vertex.Z = -sizeDiv2;

            quad.Vertices[1].Vertex.X = -sizeDiv2;
            quad.Vertices[1].Vertex.Y = sizeDiv2;
            quad.Vertices[1].Vertex.Z = -sizeDiv2;

            quad.Vertices[2].Vertex.X = sizeDiv2;
            quad.Vertices[2].Vertex.Y = sizeDiv2;
            quad.Vertices[2].Vertex.Z = -sizeDiv2;

            quad.Vertices[3].Vertex.X = sizeDiv2;
            quad.Vertices[3].Vertex.Y = -sizeDiv2;
            quad.Vertices[3].Vertex.Z = -sizeDiv2;
        }

        private void AddBottom(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Polygon(4);
            result.Add(quad);
            quad.Vertices[0].Vertex.X = -sizeDiv2;
            quad.Vertices[0].Vertex.Y = -sizeDiv2;
            quad.Vertices[0].Vertex.Z = sizeDiv2;

            quad.Vertices[1].Vertex.X = -sizeDiv2;
            quad.Vertices[1].Vertex.Y = -sizeDiv2;
            quad.Vertices[1].Vertex.Z = -sizeDiv2;

            quad.Vertices[2].Vertex.X = sizeDiv2;
            quad.Vertices[2].Vertex.Y = -sizeDiv2;
            quad.Vertices[2].Vertex.Z = -sizeDiv2;

            quad.Vertices[3].Vertex.X = sizeDiv2;
            quad.Vertices[3].Vertex.Y = -sizeDiv2;
            quad.Vertices[3].Vertex.Z = sizeDiv2;
        }

        private void AddFront(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Polygon(4);
            result.Add(quad);
            quad.Vertices[3].Vertex.X = sizeDiv2;
            quad.Vertices[3].Vertex.Y = -sizeDiv2;
            quad.Vertices[3].Vertex.Z = sizeDiv2;

            quad.Vertices[2].Vertex.X = -sizeDiv2;
            quad.Vertices[2].Vertex.Y = -sizeDiv2;
            quad.Vertices[2].Vertex.Z = sizeDiv2;

            quad.Vertices[1].Vertex.X = -sizeDiv2;
            quad.Vertices[1].Vertex.Y = sizeDiv2;
            quad.Vertices[1].Vertex.Z = sizeDiv2;

            quad.Vertices[0].Vertex.X = sizeDiv2;
            quad.Vertices[0].Vertex.Y = sizeDiv2;
            quad.Vertices[0].Vertex.Z = sizeDiv2;
        }

        private void AddLeft(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Polygon(4);
            result.Add(quad);
            quad.Vertices[3].Vertex.X = -sizeDiv2;
            quad.Vertices[3].Vertex.Y = -sizeDiv2;
            quad.Vertices[3].Vertex.Z = sizeDiv2;

            quad.Vertices[2].Vertex.X = -sizeDiv2;
            quad.Vertices[2].Vertex.Y = -sizeDiv2;
            quad.Vertices[2].Vertex.Z = -sizeDiv2;

            quad.Vertices[1].Vertex.X = -sizeDiv2;
            quad.Vertices[1].Vertex.Y = sizeDiv2;
            quad.Vertices[1].Vertex.Z = -sizeDiv2;

            quad.Vertices[0].Vertex.X = -sizeDiv2;
            quad.Vertices[0].Vertex.Y = sizeDiv2;
            quad.Vertices[0].Vertex.Z = sizeDiv2;
        }

        private void AddRight(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Polygon(4);
            result.Add(quad);
            quad.Vertices[0].Vertex.X = sizeDiv2;
            quad.Vertices[0].Vertex.Y = -sizeDiv2;
            quad.Vertices[0].Vertex.Z = -sizeDiv2;

            quad.Vertices[1].Vertex.X = sizeDiv2;
            quad.Vertices[1].Vertex.Y = sizeDiv2;
            quad.Vertices[1].Vertex.Z = -sizeDiv2;

            quad.Vertices[2].Vertex.X = sizeDiv2;
            quad.Vertices[2].Vertex.Y = sizeDiv2;
            quad.Vertices[2].Vertex.Z = sizeDiv2;

            quad.Vertices[3].Vertex.X = sizeDiv2;
            quad.Vertices[3].Vertex.Y = -sizeDiv2;
            quad.Vertices[3].Vertex.Z = sizeDiv2;
        }

        private void AddTop(List<Polygon> result, double sizeDiv2)
        {
            var quad = new Polygon(4);
            result.Add(quad);
            quad.Vertices[3].Vertex.X = sizeDiv2;
            quad.Vertices[3].Vertex.Y = sizeDiv2;
            quad.Vertices[3].Vertex.Z = sizeDiv2;

            quad.Vertices[2].Vertex.X = -sizeDiv2;
            quad.Vertices[2].Vertex.Y = sizeDiv2;
            quad.Vertices[2].Vertex.Z = sizeDiv2;

            quad.Vertices[1].Vertex.X = -sizeDiv2;
            quad.Vertices[1].Vertex.Y = sizeDiv2;
            quad.Vertices[1].Vertex.Z = -sizeDiv2;

            quad.Vertices[0].Vertex.X = sizeDiv2;
            quad.Vertices[0].Vertex.Y = sizeDiv2;
            quad.Vertices[0].Vertex.Z = -sizeDiv2;
        }

        private void PerformCutsOnRoughCube(IReadOnlyList<Polygon> roughCube, IReadOnlyList<GemCadFileTierData> tiers)
        {
            var polygons = new List<Polygon>(roughCube);
            var map = new List<Tuple<int, int, Polygon>>();

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
                        if (currentPolygon.Vertices.Count == 0)
                            continue;

                        var crossPoints = CutPolygonByPlane(currentPolygon, tierIndex);

                        for (int k = 0; k < crossPoints.Count; k++)
                        {
                            var point = crossPoints[k];
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

                    //logger.Debug($"CutPoints = {cutPoints.Count}");
                    for (int j = 0; j < cutPoints.Count; j++)
                    {
                        var point = cutPoints[j];
                        bool alreadyExists = false;
                        for (int k = 0; k < cutPolygon.Vertices.Count; k++)
                        {
                            if (IsSamePoint(point, cutPolygon.Vertices[k].Vertex))
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

                    if (cutPolygon.Vertices.Count > 2)
                    {
                        ReArrangePoints(cutPolygon);
                        polygons.Add(cutPolygon);
                        map.Add(new Tuple<int, int, Polygon>(h, i, cutPolygon));
                    }
                }
            }

            foreach (var mapItem in map)
            {
                tiers[mapItem.Item1].Indices[mapItem.Item2].Points =
                    mapItem.Item3.Vertices.Select(x => x.Vertex).ToList();
            }
        }

        private List<Vertex3D> CutPolygonByPlane(Polygon polygon, GemCadFileTierIndexData tierIndex)
        {
            var crossPoints = new List<Vertex3D>();
            var planeNormal = tierIndex.FacetNormal;
            var planePoint = geometryOperations.ProjectPointAlongVector(planeNormal, new Vertex3D(), 3.0);

            //logger.Debug($"Polygon Point Count={pg.Vertices.Count}");

            int i = 0;
            while (true)
            {
                var fp1 = polygon.Vertices[i].Vertex;
                Vertex3D fp2;
                if (i == polygon.Vertices.Count - 1)
                {
                    fp2 = polygon.Vertices[0].Vertex;
                }
                else
                {
                    fp2 = polygon.Vertices[i + 1].Vertex;
                }

                var d = planeNormal.X * (fp2.X - fp1.X) + planeNormal.Y * (fp2.Y - fp1.Y) + planeNormal.Z * (fp2.Z - fp1.Z);

                if (Math.Abs(d) > Constants.Tolerance)
                {
                    var delta = (planeNormal.X * (planePoint.X - fp1.X) + planeNormal.Y * (planePoint.Y - fp1.Y) + planeNormal.Z * (planePoint.Z - fp1.Z)) / d;
                    if (Math.Abs(delta) < Constants.Tolerance)
                    {
                        delta = 0;
                    }

                    var cx = fp1.X + (fp2.X - fp1.X) * delta;
                    var cy = fp1.Y + (fp2.Y - fp1.Y) * delta;
                    var cz = fp1.Z + (fp2.Z - fp1.Z) * delta;

                    if (delta >= 0 && delta <= 1)
                    {
                        var crossPoint = new Vertex3D(cx, cy, cz);
                        bool alreadyExists = false;
                        for (int j = 0; j < crossPoints.Count; j++)
                        {
                            if (IsSamePoint(crossPoint, crossPoints[j]))
                            {
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (!alreadyExists)
                        {
                            //logger.Debug($"Cross Point: {cx},{cy},{cz}  g0: {g0.X},{g0.Y},{g0.Z}  g1: {g1.X},{g1.Y},{g1.Z}  delta: {delta}");
                            crossPoints.Add(crossPoint);
                        }
                    }
                }

                i++;
                if (i == polygon.Vertices.Count)
                {
                    break;
                }
            }

            i = polygon.Vertices.Count - 1;

            while (true)
            {
                var fp1 = polygon.Vertices[i].Vertex;

                var d = planeNormal.X * (fp1.X - planePoint.X) + planeNormal.Y * (fp1.Y - planePoint.Y) + planeNormal.Z * (fp1.Z - planePoint.Z);

                if (d > 0)
                {
                    polygon.RemoveAt(i);
                }

                i--;
                if (i == -1)
                {
                    break;
                }
            }

            for (i = 0; i < crossPoints.Count; i++)
            {
                planePoint = crossPoints[i];
                bool alreadyExists = false;
                for (int j = 0; j < polygon.Vertices.Count; j++)
                {
                    if (IsSamePoint(planePoint, polygon.Vertices[j].Vertex))
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    polygon.Add(new PolygonVertex(new Vertex3D(planePoint.X, planePoint.Y, planePoint.Z)));
                }
            }

            if (polygon.Vertices.Count > 3)
            {
                ReArrangePoints(polygon);
            }

            return crossPoints;
        }

        private void ProcessPolygons(GemCadFileData data)
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

        private void ReArrangePoints(Polygon polygon)
        {
            if (polygon.Vertices.Count < 4)
            {
                return;
            }
            
            var vertices = polygon.Vertices.ToList();

            int maxIndex = 0;
            
            Vertex3D point1 = vertices[0].Vertex;
            Vertex3D point2, point3;
            Vertex3D g0, g1;
            
            var reorderedPointList = new List<Vertex3D>();
            for (int i = 0; i < vertices.Count; i++)
            {
                reorderedPointList.Add(new Vertex3D());
            }

            reorderedPointList[0] = point1;

            double maxAngle;
            var dAngles = new List<double>();
            dAngles.Add(0);

            for (int i = 1; i < vertices.Count; i++)
            {
                point2 = vertices[i].Vertex;
                maxAngle = 0;
                for (int j = 1; j < vertices.Count; j++)
                {
                    if (i != j)
                    {
                        point3 = vertices[j].Vertex;

                        g0 = new Vertex3D(point2.X - point1.X, point2.Y - point1.Y, point2.Z - point1.Z);
                        g1 = new Vertex3D(point3.X - point1.X, point3.Y - point1.Y, point3.Z - point1.Z);

                        var angle = AngleBetween(g0, g1);

                        if (maxAngle < angle)
                        {
                            maxAngle = angle;
                        }
                    }
                }

                dAngles.Add(maxAngle);
            }

            maxAngle = 0;

            for (int i = 1; i < dAngles.Count; i++)
            {
                if (maxAngle < dAngles[i])
                {
                    maxAngle = dAngles[i];
                    maxIndex = i;
                }
            }

            point2 = vertices[maxIndex].Vertex;
            reorderedPointList[1] = point2;
            dAngles.Clear();
            dAngles.Add(-1);

            for (int i = 1; i < vertices.Count; i++)
            {
                point3 = vertices[i].Vertex;
                g0 = new Vertex3D(point2.X - point1.X, point2.Y - point1.Y, point2.Z - point1.Z);
                g1 = new Vertex3D(point3.X - point1.X, point3.Y - point1.Y, point3.Z - point1.Z);

                var angle = AngleBetween(g0, g1);
                dAngles.Add(angle);
            }

            int nLows;

            for (int i = 1; i < dAngles.Count; i++)
            {
                if (i != maxIndex)
                {
                    nLows = 0;
                    for (int j = 0; j < dAngles.Count; j++)
                    {
                        if (dAngles[j] < dAngles[i])
                            nLows++;
                    }

                    reorderedPointList[nLows] = vertices[i].Vertex;
                }
            }

            polygon.Replace(reorderedPointList.Select(x => new PolygonVertex(x)).ToArray());
        }

        private double AngleBetween(Vertex3D p1, Vertex3D p2)
        {
            return vectorOperations.AngleBetween(p1.Clone<Vertex3D>(), p2.Clone<Vertex3D>());
        }

        private bool IsSamePoint(Vertex3D pt1, Vertex3D pt2)
        {
            return Math.Abs(geometryOperations.Length3d(pt1, pt2)) < Constants.Tolerance;
        }
    }
}