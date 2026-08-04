using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FDEMCore.FxTMesh.Meshing
{
    public static class CrossPlyMeshAugmenter
    {
        private const double NodeTolerance = 1e-8;


        public static FEMesh AddZCrossPly(FEMesh mesh, CellBoundary boundary, double thickness, FxTElementFamily config, DebugOptions? dOptions = null)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));
            if (thickness <= 0.0) return mesh;

            if (boundary.Walls[4].BoundaryType == BoundaryType.Periodic)
                throw new InvalidOperationException("CrossPly can only be added in the z direction when the z direction is non-periodic.");

            double ly = boundary.ODimensions[1];
            double lz = boundary.ODimensions[2];

            var nodes = mesh.GlobalNodes.ToList();
            var elements = mesh.Elements.ToList();
            var periodicPairs = mesh.PeriodicNodePairs.ToList();

            var bottom = FindEdgeNodes(nodes, zValue: 0.0);
            var top = FindEdgeNodes(nodes, zValue: lz);

            if (bottom.Count < 2 || top.Count < 2)
                throw new InvalidOperationException("Could not find enough top/bottom boundary nodes to build CrossPly layers.");

            IElementBuilder myElementBuilder = ElementBuilderProvider.Create(config);
            int nNodesPerTriangleSide = myElementBuilder.GetNNodesPerSideOfElement();

            // Calculate average spacing from both top and bottom edges
            double avgBottomSpacing = CalculateAverageNodeSpacing(bottom, nNodesPerTriangleSide);
            double aveTopSpacing = CalculateAverageNodeSpacing(top, nNodesPerTriangleSide);
            double avgSpacing = (avgBottomSpacing + aveTopSpacing) / 2.0; // Average of both edges

            if (avgSpacing <= NodeTolerance)
                throw new InvalidOperationException("Could not determine valid boundary point spacing for CrossPly layers.");

            int nRows = Math.Max(1, (int)Math.Round(thickness / avgSpacing));
            double dz = thickness / nRows;

            int nextElementId = elements.Count;

            AddLayer(bottom, -dz, -thickness, nodes, elements, ref nextElementId, config);
            AddLayer(top, dz, lz + thickness, nodes, elements, ref nextElementId, config);

            // Build periodic node pairs and node regions
            var periodicData = IdentifyImportantNodes.BuildPeriodicBoundaryData(boundary, NodeTolerance, nodes);

            // Write final mesh: triangles + fiber + quads/triangles
            if (dOptions != null && dOptions.Debug)
            {
                var fullMesh = new FEMesh(nodes.ToList(), elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>(), null);
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("AllMeshPlusCrossPly"), fullMesh);
            }

            //Now add the thickness of the layer to the ODimensions
            boundary.ODimensions[2] += 2 * thickness;

            return new FEMesh(nodes, elements, periodicData.Pairs, periodicData.X1Nodes, periodicData.Y1Nodes,
                periodicData.PinnedNode);
        }

        /// <summary>
        /// Calculates the average spacing between consecutive corner nodes (primary nodes) along an edge.
        /// </summary>
        private static double CalculateAverageNodeSpacing(List<(int Index, Point2D Point)> edge, int nNodesPerTriangleSide)
        {
            // Extract primary/corner nodes (every nNodesPerTriangleSide-th node)
            var primaryEdge = edge.Where((e, i) => i % nNodesPerTriangleSide == 0).ToList();

            if (primaryEdge.Count < 2)
                return 0.0;

            // Ensure we include the last node if it's not already included
            if (primaryEdge[^1].Index != edge[^1].Index)
                primaryEdge.Add(edge[^1]);

            // Calculate spacings between consecutive primary nodes
            double totalSpacing = 0.0;
            for (int i = 0; i < primaryEdge.Count - 1; i++)
            {
                double dx = primaryEdge[i + 1].Point.X - primaryEdge[i].Point.X;
                totalSpacing += Math.Abs(dx);
            }

            return totalSpacing / (primaryEdge.Count - 1);
        }

        private static List<(int Index, Point2D Point)> FindEdgeNodes(List<Point2D> nodes, double zValue)
        {
            return nodes
                .Select((p, i) => (Index: i, Point: p))
                .Where(n => Math.Abs(n.Point.Y - zValue) < NodeTolerance)
                .OrderBy(n => n.Point.X)
                .ToList();
        }

        private static void AddLayer(List<(int Index, Point2D Point)> edge, double dz, double outerZ, List<Point2D> nodes, List<Element> elements, ref int nextElementId, FxTElementFamily config)
        {
            IElementBuilder myElementBuilder = ElementBuilderProvider.Create(config);
            int step = myElementBuilder.GetNNodesPerSideOfElement();

            // Extract corner/primary nodes only
            var primaryEdge = edge.Where((e, i) => i % (step-1) == 0).ToList();

            if (primaryEdge[^1].Index != edge[^1].Index)
                primaryEdge.Add(edge[^1]);

            int nRows = Math.Max(1, (int)Math.Round(Math.Abs((outerZ - edge[0].Point.Y) / dz)));

            // Create all rows with ALL boundary nodes (ensures alignment)
            var rowNodeIds = new List<int[]>();
            rowNodeIds.Add(edge.Select(e => e.Index).ToArray()); // First row is the full boundary

            for (int r = 1; r <= nRows; r++)
            {
                double z = edge[0].Point.Y + r * dz;
                var row = new int[edge.Count];

                // Create nodes at all boundary X positions
                for (int c = 0; c < edge.Count; c++)
                {
                    var p = new Point2D(edge[c].Point.X, z);
                    row[c] = AddNode(nodes, p);
                }

                rowNodeIds.Add(row);
            }

            // Create quad elements ONLY between consecutive corner nodes
            for (int r = 0; r < nRows; r++)
            {
                for (int i = 0; i < primaryEdge.Count - 1; i++)
                {
                    // Find indices in the full edge list for this corner pair
                    int c0 = edge.FindIndex(e => e.Index == primaryEdge[i].Index);
                    int c1 = edge.FindIndex(e => e.Index == primaryEdge[i + 1].Index);

                    Point2D p00 = nodes[rowNodeIds[r][c0]];
                    Point2D p10 = nodes[rowNodeIds[r][c1]];
                    Point2D p11 = nodes[rowNodeIds[r + 1][c1]];
                    Point2D p01 = nodes[rowNodeIds[r + 1][c0]];

                    var quadNodes = BuildQuadNodes(p00, p10, p11, p01, myElementBuilder.GetQuadName());

                    for (int j = 0; j < quadNodes.Length; j++)
                        AddNode(nodes, quadNodes[j]);

                    elements.Add(new Element(nextElementId++, ElementPhase.Composite, myElementBuilder.GetQuadName(), quadNodes));
                }
            }
        }
        private static int AddNode(List<Point2D> nodes, Point2D p)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (Math.Abs(nodes[i].X - p.X) < NodeTolerance && Math.Abs(nodes[i].Y - p.Y) < NodeTolerance)
                    return i;
            }

            nodes.Add(p);
            return nodes.Count - 1;
        }

        private static Point2D[] BuildQuadNodes(Point2D p0, Point2D p1, Point2D p2, Point2D p3, string quadElementName)
        {
            //remove the IPs
            string elementType = quadElementName.Split('.')[0];

            return elementType switch
            {
                "2DQ8" or "2P5DQ9" => BuildQuad8(p0, p1, p2, p3),

                "2DQ9" or "2P5DQ10" => BuildQuad9(p0, p1, p2, p3),

                "2DQ12" or "2P5DQ13" => BuildQuad12(p0, p1, p2, p3),

                "2DQ16" or "2P5DQ17" => BuildQuad16(p0, p1, p2, p3),

                _ => throw new NotSupportedException($"Unsupported CrossPly quad order: {quadElementName}")
            };
        }


        private static Point2D[] BuildQuad8(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            Point2D[] nodes = new[] { p0, ElementBuilderBase.Midpoint(p0, p1), p1, ElementBuilderBase.Midpoint(p1, p2), p2,
                ElementBuilderBase.Midpoint(p2, p3), p3, ElementBuilderBase.Midpoint(p3, p0) };
            return ElementBuilderBase.EnsureCcwQuad8(nodes);
        }

        private static Point2D[] BuildQuad9(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            Point2D[] nodes = BuildQuad8(p0, p1, p2, p3).Concat(new[] { ElementBuilderBase.Centroid(p0, p1, p2, p3) }).ToArray();
            return ElementBuilderBase.EnsureCcwQuad9(nodes);
        }

        private static Point2D[] BuildQuad12(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            Point2D[] nodes = new[]
            {
                p0, ElementBuilderBase.PointAlong(p0, p1, 1.0 / 3.0), ElementBuilderBase.PointAlong(p0, p1, 2.0 / 3.0), p1,
                ElementBuilderBase.PointAlong(p1, p2, 1.0 / 3.0), ElementBuilderBase.PointAlong(p1, p2, 2.0 / 3.0), p2,
                ElementBuilderBase.PointAlong(p2, p3, 1.0 / 3.0), ElementBuilderBase.PointAlong(p2, p3, 2.0 / 3.0), p3,
                ElementBuilderBase.PointAlong(p3, p0, 1.0 / 3.0), ElementBuilderBase.PointAlong(p3, p0, 2.0 / 3.0)
            };
            return ElementBuilderBase.EnsureCcwQuad12(nodes);
        }

        private static Point2D[] BuildQuad16(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            Point2D[] nodes = BuildQuad12(p0, p1, p2, p3)
                .Concat(new[]
                {
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 1.0 / 3.0, 1.0 / 3.0),
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 2.0 / 3.0, 1.0 / 3.0),
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 2.0 / 3.0, 2.0 / 3.0),
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 1.0 / 3.0, 2.0 / 3.0)
                })
                .ToArray();
            return ElementBuilderBase.EnsureCcwQuad16(nodes);
        }

    }
}