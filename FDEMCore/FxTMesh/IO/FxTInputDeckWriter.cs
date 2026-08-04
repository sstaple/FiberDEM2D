using FDEMCore;
using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing;
using FDEMCore.FxTMesh.Meshing.Elements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FxTMeshGenerator.IO
{
    public static class FxTInputDeckWriter
    {
        public static void WriteMeshDeck( string outputDirectory, FEMesh mesh, CellBoundary boundary)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));

            Directory.CreateDirectory(outputDirectory);

            WriteNodes(Path.Combine(outputDirectory, "nodes.fxt"), mesh);
            WriteElements(Path.Combine(outputDirectory, "elements.fxt"), mesh);

            bool hasPeriodicData = mesh.PeriodicNodePairs.Count > 0;

            if (hasPeriodicData)
            {
                WritePbcPairs(Path.Combine(outputDirectory, "pbcPairs.fxt"), mesh);
                WriteNodeRegions(Path.Combine(outputDirectory, "nodeRegions.fxt"), mesh);
                WritePackStatistics(Path.Combine(outputDirectory, "packStatistics.json"), boundary);
            }
        }

        private static void WriteNodes(string path, FEMesh mesh)
        {
            using var writer = new StreamWriter(path);

            writer.WriteLine("id,x,y");

            for (int i = 0; i < mesh.GlobalNodes.Count; i++)
            {
                Point2D p = mesh.GlobalNodes[i];
                int nodeId = i + 1;

                writer.WriteLine(string.Join(",",
                    nodeId.ToString(CultureInfo.InvariantCulture),
                    p.X.ToString("G17", CultureInfo.InvariantCulture),
                    p.Y.ToString("G17", CultureInfo.InvariantCulture)));
            }
        }

        private static void WriteElements(string path, FEMesh mesh)
        {
            using var writer = new StreamWriter(path);

            for (int i = 0; i < mesh.Elements.Count; i++)
            {
                Element element = mesh.Elements[i];

                int elementId = i + 1;
                string materialName = GetMaterialName(element);
                string elementType = element.ElementName;

                var nodeIds = element.Nodes
                    .Select(node => FindNodeId(mesh.GlobalNodes, node).ToString(CultureInfo.InvariantCulture));

                writer.WriteLine(string.Join(",",
                    new[] { elementId.ToString(CultureInfo.InvariantCulture), materialName, elementType }
                    .Concat(nodeIds)));
            }
        }

        private static void WritePbcPairs(string path, FEMesh mesh)
        {
            using var writer = new StreamWriter(path);

            foreach (var pair in mesh.PeriodicNodePairs)
            {
                writer.WriteLine($"{pair.Node1 + 1},{pair.Node2 + 1}");
            }
        }

        private static void WriteNodeRegions(string path, FEMesh mesh)
        {
            using var writer = new StreamWriter(path);

            if (mesh.PinnedNode.HasValue)
            {
                writer.WriteLine("FxT.pinned");
                writer.WriteLine(mesh.PinnedNode.Value + 1);
                //writer.WriteLine();
            }

            if (mesh.X1Nodes.Count > 0)
            {
                writer.WriteLine("FxT.x1");
                foreach (int nodeIndex in mesh.X1Nodes)
                    writer.WriteLine(nodeIndex + 1);
                //writer.WriteLine();
            }

            if (mesh.Y1Nodes.Count > 0)
            {
                writer.WriteLine("FxT.y1");
                foreach (int nodeIndex in mesh.Y1Nodes)
                    writer.WriteLine(nodeIndex + 1);
            }
        }
       
        private static void WritePackStatistics(string path, CellBoundary boundary)
        {
            var statistics = new
            {
                Lx = boundary.ODimensions[0],
                Ly = boundary.ODimensions[1],
                Lz = boundary.ODimensions[2]
            };

            //Now check for periodicity in each direction.  If it isn't periodic, then we need to add the wall thickness to the corresponding dimension.


            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(path, JsonSerializer.Serialize(statistics, options));
        }

        private static string GetMaterialName(Element element)
        {
            return element.Phase switch
            {
                ElementPhase.Fiber => "fiber",
                ElementPhase.Matrix => "matrix",
                ElementPhase.Composite => "composite",
                _ => throw new NotSupportedException($"Unsupported element phase: {element.Phase}")
            };
        }

        private static int FindNodeId(IReadOnlyList<Point2D> globalNodes, Point2D node)
        {
            const double tolerance = 1e-10;

            for (int i = 0; i < globalNodes.Count; i++)
            {
                double dx = globalNodes[i].X - node.X;
                double dy = globalNodes[i].Y - node.Y;

                if (Math.Sqrt(dx * dx + dy * dy) < tolerance)
                    return i + 1;
            }

            throw new InvalidOperationException(
                $"Could not find node ({node.X}, {node.Y}) in global node list.");
        }
    }
}
