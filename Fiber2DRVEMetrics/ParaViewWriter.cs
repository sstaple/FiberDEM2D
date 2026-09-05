using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DelaunatorSharp;

namespace Fiber2DRVEMetrics
{
    internal class ParaViewWriter
    {
        public static void WriteDisks(string path,
                                      List<double> Y,
                                      List<double> Z,
                                      List<double> R,
                                      List<Triangle> triangles,
                                      int nSides = 32)
        {
            // Collect all unique point indices used by the filtered triangles
            var includeSet = new HashSet<int>(
                triangles.SelectMany(t => t.VertexIndices)
            );

            var points = new List<IPoint>();
            var connectivity = new List<int>();
            var offsets = new List<int>();
            var types = new List<byte>();
            int offset = 0;

            foreach (int i in includeSet)
            {
                if (i < 0 || i >= Y.Count) continue;
                double cx = Y[i], cy = Z[i], radius = R[i];
                var cell = new List<int>();
                for (int j = 0; j < nSides; j++)
                {
                    double theta = 2 * Math.PI * j / nSides;
                    double x = cx + radius * Math.Cos(theta);
                    double y = cy + radius * Math.Sin(theta);
                    points.Add(new Point(x, y));
                    cell.Add(points.Count - 1);
                }
                connectivity.AddRange(cell);
                offset += cell.Count;
                offsets.Add(offset);
                types.Add(7); // VTK_POLYGON
            }

            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.WriteLine("<?xml version=\"1.0\"?>");
            writer.WriteLine("<VTKFile type=\"UnstructuredGrid\" version=\"0.1\" byte_order=\"LittleEndian\">");
            writer.WriteLine("  <UnstructuredGrid>");
            writer.WriteLine($"    <Piece NumberOfPoints=\"{points.Count}\" NumberOfCells=\"{offsets.Count}\"> ");

            // Points
            writer.WriteLine("      <Points>");
            writer.WriteLine("        <DataArray type=\"Float32\" NumberOfComponents=\"3\" format=\"ascii\">");
            foreach (var p in points)
                writer.WriteLine($"          {p.X.ToString(CultureInfo.InvariantCulture)} {p.Y.ToString(CultureInfo.InvariantCulture)} 0.0");
            writer.WriteLine("        </DataArray>");
            writer.WriteLine("      </Points>");

            // Cells: connectivity, offsets, types
            writer.WriteLine("      <Cells>");
            writer.WriteLine("        <DataArray type=\"Int32\" Name=\"connectivity\" format=\"ascii\">");
            foreach (var idx in connectivity)
                writer.WriteLine($"          {idx}");
            writer.WriteLine("        </DataArray>");

            writer.WriteLine("        <DataArray type=\"Int32\" Name=\"offsets\" format=\"ascii\">");
            foreach (var off in offsets)
                writer.WriteLine($"          {off}");
            writer.WriteLine("        </DataArray>");

            writer.WriteLine("        <DataArray type=\"UInt8\" Name=\"types\" format=\"ascii\">");
            foreach (var t in types)
                writer.WriteLine($"          {t}");
            writer.WriteLine("        </DataArray>");
            writer.WriteLine("      </Cells>");

            writer.WriteLine("    </Piece>");
            writer.WriteLine("  </UnstructuredGrid>");
            writer.WriteLine("</VTKFile>");
        }
        

        public static void WriteTriangles(string path, List<Triangle> triangles, bool includeVolumeFraction = true)
        {
            var points = new List<IPoint>();
            var connectivity = new List<(int, int, int)>();
            var volumeFractions = new List<double>();

            var pointMap = new Dictionary<string, int>();
            int pointIndex = 0;

            foreach (var tri in triangles)
            {
                var indices = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    var p = tri.VertexCoordinates[i];
                    string key = $"{p.X},{p.Y}";
                    if (!pointMap.ContainsKey(key))
                    {
                        pointMap[key] = pointIndex++;
                        points.Add(p);
                    }
                    indices[i] = pointMap[key];
                }
                connectivity.Add((indices[0], indices[1], indices[2]));
                if (includeVolumeFraction)
                    volumeFractions.Add(tri.VolumeFraction);
            }

            using var writer = new StreamWriter(path, false, Encoding.UTF8);

            writer.WriteLine("<?xml version=\"1.0\"?>");
            writer.WriteLine("<VTKFile type=\"UnstructuredGrid\" version=\"0.1\" byte_order=\"LittleEndian\">");
            writer.WriteLine("  <UnstructuredGrid>");
            writer.WriteLine($"    <Piece NumberOfPoints=\"{points.Count}\" NumberOfCells=\"{connectivity.Count}\">");

            // Points
            writer.WriteLine("      <Points>");
            writer.WriteLine("        <DataArray type=\"Float32\" NumberOfComponents=\"3\" format=\"ascii\">");
            foreach (var p in points)
                writer.WriteLine($"          {p.X.ToString(CultureInfo.InvariantCulture)} {p.Y.ToString(CultureInfo.InvariantCulture)} 0.0");
            writer.WriteLine("        </DataArray>");
            writer.WriteLine("      </Points>");

            // Connectivity
            writer.WriteLine("      <Cells>");
            writer.WriteLine("        <DataArray type=\"Int32\" Name=\"connectivity\" format=\"ascii\">");
            foreach (var (i0, i1, i2) in connectivity)
                writer.WriteLine($"          {i0} {i1} {i2}");
            writer.WriteLine("        </DataArray>");

            writer.WriteLine("        <DataArray type=\"Int32\" Name=\"offsets\" format=\"ascii\">");
            for (int i = 1; i <= connectivity.Count; i++)
                writer.WriteLine("          " + (i * 3));
            writer.WriteLine("        </DataArray>");

            writer.WriteLine("        <DataArray type=\"UInt8\" Name=\"types\" format=\"ascii\">");
            for (int i = 0; i < connectivity.Count; i++)
                writer.WriteLine("          5"); // VTK_TRIANGLE = 5
            writer.WriteLine("        </DataArray>");
            writer.WriteLine("      </Cells>");

            // Cell data
            if (includeVolumeFraction)
            {
                writer.WriteLine("      <CellData Scalars=\"volumeFraction\">");
                writer.WriteLine("        <DataArray type=\"Float32\" Name=\"volumeFraction\" format=\"ascii\">");
                foreach (var v in volumeFractions)
                    writer.WriteLine("          " + v.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("        </DataArray>");
                writer.WriteLine("      </CellData>");
            }

            writer.WriteLine("    </Piece>");
            writer.WriteLine("  </UnstructuredGrid>");
            writer.WriteLine("</VTKFile>");
        }

        public static void WriteTrianglesWithClusters(string path,
                                                       List<Triangle> triangles,
                                                       bool includeVolumeFraction = true,
                                                       bool includeFCNumber = true,
                                                       bool includeIsFC = true,
                                                       bool includeMRCNumber = true,
                                                       bool includeIsMRC = true)
        {
            var points = new List<IPoint>();
            var connectivity = new List<(int, int, int)>();

            var volFracs = new List<double>();
            var fcNums = new List<int>();
            var isFCs = new List<byte>();
            var mrcNums = new List<int>();
            var isMRCs = new List<byte>();

            var pointMap = new Dictionary<string, int>();
            int pIdx = 0;
            foreach (var tri in triangles)
            {
                var idxs = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    var p = tri.VertexCoordinates[i];
                    string key = $"{p.X},{p.Y}";
                    if (!pointMap.TryGetValue(key, out int mapIdx))
                    {
                        mapIdx = pIdx++;
                        pointMap[key] = mapIdx;
                        points.Add(p);
                    }
                    idxs[i] = mapIdx;
                }
                connectivity.Add((idxs[0], idxs[1], idxs[2]));

                if (includeVolumeFraction) volFracs.Add(tri.VolumeFraction);
                if (includeFCNumber) fcNums.Add(tri.FCNumber);
                if (includeIsFC) isFCs.Add(tri.IsFC ? (byte)1 : (byte)0);
                if (includeMRCNumber) mrcNums.Add(tri.MRCNumber);
                if (includeIsMRC) isMRCs.Add(tri.IsMRC ? (byte)1 : (byte)0);
            }

            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.WriteLine("<?xml version=\"1.0\"?>");
            writer.WriteLine("<VTKFile type=\"UnstructuredGrid\" version=\"0.1\" byte_order=\"LittleEndian\">");
            writer.WriteLine("  <UnstructuredGrid>");
            writer.WriteLine($"    <Piece NumberOfPoints=\"{points.Count}\" NumberOfCells=\"{connectivity.Count}\">\n");

            // Points
            writer.WriteLine("      <Points>");
            writer.WriteLine("        <DataArray type=\"Float32\" NumberOfComponents=\"3\" format=\"ascii\">");
            foreach (var p in points)
                writer.WriteLine($"          {p.X.ToString(CultureInfo.InvariantCulture)} {p.Y.ToString(CultureInfo.InvariantCulture)} 0.0");
            writer.WriteLine("        </DataArray>");
            writer.WriteLine("      </Points>");

            // Cells
            writer.WriteLine("      <Cells>");
            writer.WriteLine("        <DataArray type=\"Int32\" Name=\"connectivity\" format=\"ascii\">");
            foreach (var (a, b, c) in connectivity)
                writer.WriteLine($"          {a} {b} {c}");
            writer.WriteLine("        </DataArray>");

            writer.WriteLine("        <DataArray type=\"Int32\" Name=\"offsets\" format=\"ascii\">");
            for (int i = 1; i <= connectivity.Count; i++)
                writer.WriteLine($"          {i * 3}");
            writer.WriteLine("        </DataArray>");

            writer.WriteLine("        <DataArray type=\"UInt8\" Name=\"types\" format=\"ascii\">");
            for (int i = 0; i < connectivity.Count; i++)
                writer.WriteLine("          5");
            writer.WriteLine("        </DataArray>");
            writer.WriteLine("      </Cells>");

            // CellData
            if (includeVolumeFraction || includeFCNumber || includeIsFC || includeMRCNumber || includeIsMRC)
            {
                writer.Write("      <CellData");
                if (includeVolumeFraction) writer.Write(" Scalars=\"volumeFraction\"");
                writer.WriteLine(">");

                if (includeVolumeFraction)
                {
                    writer.WriteLine("        <DataArray type=\"Float32\" Name=\"volumeFraction\" format=\"ascii\">");
                    foreach (var v in volFracs) writer.WriteLine($"          {v.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine("        </DataArray>");
                }
                if (includeFCNumber)
                {
                    writer.WriteLine("        <DataArray type=\"Int32\" Name=\"FCNumber\" format=\"ascii\">");
                    foreach (var n in fcNums) writer.WriteLine($"          {n}");
                    writer.WriteLine("        </DataArray>");
                }
                if (includeIsFC)
                {
                    writer.WriteLine("        <DataArray type=\"UInt8\" Name=\"IsFC\" format=\"ascii\">");
                    foreach (var b in isFCs) writer.WriteLine($"          {b}");
                    writer.WriteLine("        </DataArray>");
                }
                if (includeMRCNumber)
                {
                    writer.WriteLine("        <DataArray type=\"Int32\" Name=\"MRCNumber\" format=\"ascii\">");
                    foreach (var m in mrcNums) writer.WriteLine($"          {m}");
                    writer.WriteLine("        </DataArray>");
                }
                if (includeIsMRC)
                {
                    writer.WriteLine("        <DataArray type=\"UInt8\" Name=\"IsMRC\" format=\"ascii\">");
                    foreach (var b in isMRCs) writer.WriteLine($"          {b}");
                    writer.WriteLine("        </DataArray>");
                }
                writer.WriteLine("      </CellData>");
            }

            writer.WriteLine("    </Piece>");
            writer.WriteLine("  </UnstructuredGrid>");
            writer.WriteLine("</VTKFile>");
        }
    
    }
}

