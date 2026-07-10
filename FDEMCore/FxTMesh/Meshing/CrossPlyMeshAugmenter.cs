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

        public static FEMesh AddZCrossPly(FEMesh mesh, CellBoundary boundary, double thickness, ElementConfig config, DebugOptions? dOptions = null)
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

            int nNodesPerTriangleSide = config.FiberTriangleNodes / 3;

            double spacing = (bottom[1].Point.X - bottom[0].Point.X) * nNodesPerTriangleSide;
            if (spacing <= NodeTolerance)
                throw new InvalidOperationException("Could not determine valid boundary point spacing for CrossPly layers.");

            int nRows = Math.Max(1, (int)Math.Round(thickness / spacing));
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

            return new FEMesh(nodes, elements, periodicData.Pairs, periodicData.X1Nodes, periodicData.Y1Nodes,
                periodicData.PinnedNode);
        }

        private static List<(int Index, Point2D Point)> FindEdgeNodes(List<Point2D> nodes, double zValue)
        {
            return nodes
                .Select((p, i) => (Index: i, Point: p))
                .Where(n => Math.Abs(n.Point.Y - zValue) < NodeTolerance)
                .OrderBy(n => n.Point.X)
                .ToList();
        }

        private static void AddLayer(List<(int Index, Point2D Point)> edge, double dz, double outerZ, List<Point2D> nodes, List<Element> elements, ref int nextElementId, ElementConfig config)
        {
            //WARNING: This assumes that all of the triangular nodes are evenly spaced along the edge. If this is not the case, the resulting mesh may be invalid.
            int step = config.MatrixTriangleNodes / 3;
            var primaryEdge = edge.Where((e, i) => i % step == 0).ToList();

            if (primaryEdge[^1].Index != edge[^1].Index)
                primaryEdge.Add(edge[^1]);

            int nCols = primaryEdge.Count;
            int nRows = Math.Max(1, (int)Math.Round(Math.Abs((outerZ - primaryEdge[0].Point.Y) / dz)));

            var rowNodeIds = new List<int[]>();
            rowNodeIds.Add(primaryEdge.Select(e => e.Index).ToArray());

            for (int r = 1; r <= nRows; r++)
            {
                double z = primaryEdge[0].Point.Y + r * dz;
                var row = new int[nCols];

                for (int c = 0; c < nCols; c++)
                {
                    var p = new Point2D(primaryEdge[c].Point.X, z);
                    row[c] = AddNode(nodes, p);
                }

                rowNodeIds.Add(row);
            }

            for (int r = 0; r < nRows; r++)
            {
                for (int c = 0; c < nCols - 1; c++)
                {
                    Point2D p00 = nodes[rowNodeIds[r][c]];
                    Point2D p10 = nodes[rowNodeIds[r][c + 1]];
                    Point2D p11 = nodes[rowNodeIds[r + 1][c + 1]];
                    Point2D p01 = nodes[rowNodeIds[r + 1][c]];

                    var quadNodes = BuildQuadNodes(p00, p10, p11, p01, config);

                    for (int i = 0; i < quadNodes.Length; i++)
                        AddNode(nodes, quadNodes[i]);

                    elements.Add(new Element(nextElementId++, ElementPhase.Composite, config.MatrixQuadFxTType, quadNodes));
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

        private static Point2D[] BuildQuadNodes(Point2D p0, Point2D p1, Point2D p2, Point2D p3, ElementConfig config)
        {
            return config.MatrixQuadNodes switch
            {
                6 => BuildQuad6(p0, p1, p2, p3),
                8 => BuildQuad8(p0, p1, p2, p3),
                9 => BuildQuad9(p0, p1, p2, p3),
                12 => BuildQuad12(p0, p1, p2, p3),
                16 => BuildQuad16(p0, p1, p2, p3),
                _ => throw new NotSupportedException($"Unsupported CrossPly quad order: {config.MatrixQuadNodes}")
            };
        }

        private static Point2D[] BuildQuad6(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            return new[] { p0, ElementBuilderBase.Midpoint(p0, p1), p1, p3, ElementBuilderBase.Midpoint(p3, p2), p2 };
        }

        private static Point2D[] BuildQuad8(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            return new[] { p0, ElementBuilderBase.Midpoint(p0, p1), p1, ElementBuilderBase.Midpoint(p1, p2), p2, 
                ElementBuilderBase.Midpoint(p2, p3), p3, ElementBuilderBase.Midpoint(p3, p0) };
        }

        private static Point2D[] BuildQuad9(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            return BuildQuad8(p0, p1, p2, p3).Concat(new[] { ElementBuilderBase.Centroid(p0, p1, p2, p3) }).ToArray();
        }

        private static Point2D[] BuildQuad12(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            return new[]
            {
                p0, ElementBuilderBase.PointAlong(p0, p1, 1.0 / 3.0), ElementBuilderBase.PointAlong(p0, p1, 2.0 / 3.0), p1,
                ElementBuilderBase.PointAlong(p1, p2, 1.0 / 3.0), ElementBuilderBase.PointAlong(p1, p2, 2.0 / 3.0), p2,
                ElementBuilderBase.PointAlong(p2, p3, 1.0 / 3.0), ElementBuilderBase.PointAlong(p2, p3, 2.0 / 3.0), p3,
                ElementBuilderBase.PointAlong(p3, p0, 1.0 / 3.0), ElementBuilderBase.PointAlong(p3, p0, 2.0 / 3.0)
            };
        }

        private static Point2D[] BuildQuad16(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
        {
            return BuildQuad12(p0, p1, p2, p3)
                .Concat(new[]
                {
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 1.0 / 3.0, 1.0 / 3.0),
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 2.0 / 3.0, 1.0 / 3.0),
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 2.0 / 3.0, 2.0 / 3.0),
                    ElementBuilderBase.BilinearPoint(p0, p1, p2, p3, 1.0 / 3.0, 2.0 / 3.0)
                })
                .ToArray();
        }

    }
}
