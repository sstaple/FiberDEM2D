using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing.Elements;
using FDEMCore.FxTMesh.Meshing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace FDEMCore.FxTMesh.Meshing
{
    /// <summary>
    /// Builds finite element mesh from a Delaunay triangulation.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly List<Point2D> _globalNodes = new();
        private readonly Dictionary<string, int> _nodeToIndex = new();
        private readonly List<Element> _elements = new();
        private int _elementIdCounter = 0;
        private const double NodeTolerance = 1e-5;
        private double DomainTolerance;
        private IElementBuilder _elementBuilder;

        public FEMesh BuildMesh(TriangulationMesh2D triangulation,IReadOnlyList<Fiber> fibers,
            CellBoundary boundary,ElementConfig config, DebugOptions? dOptions = null)
        {
            // Reset state
            _globalNodes.Clear();
            _nodeToIndex.Clear();
            _elements.Clear();
            _elementIdCounter = 0;
            //scale tolerance to the magnitude of the problem domain (e.g. cell size) to prevent issues with very small or large coordinates
            DomainTolerance = boundary.ODimensions.Max() * NodeTolerance;

            _elementBuilder = ElementBuilderProvider.Create(config);

            // Process each triangle to build interior matrix elements
            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                var tri = triangulation.Triangles[i];
                var nodeA = triangulation.Nodes[tri[0]];
                var nodeB = triangulation.Nodes[tri[1]];
                var nodeC = triangulation.Nodes[tri[2]];

                BuildMatrixInteriorTriangle(nodeA, nodeB, nodeC, fibers, config, dOptions);
            }

            // Write intermediate mesh: just interior triangles
            if (dOptions != null && dOptions.Debug)
            {
                var interiorTriMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>(), null);
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("triMesh"), interiorTriMesh);
            }

            // Build fiber and matrix elements between adjacent triangles
            BuildInteriorFiberMatrixElements(triangulation, fibers, config, dOptions);

            // Write mesh: triangles + fiber + quads/triangles before boundary elements
            if (dOptions != null && dOptions.Debug)
            {
                var fullMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>(), null);
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("AllMeshNoBoundary"), fullMesh);
            }

            // Build boundary fiber/matrix elements for periodic edges
            BuildBoundaryFiberMatrixElements(triangulation, fibers, boundary, config, dOptions);

            // Write final mesh: triangles + fiber + quads/triangles
            if (dOptions != null && dOptions.Debug)
            {
                var fullMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>(), null);
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("AllMesh"), fullMesh);
            }

            // Build periodic node pairs and node regions
            var periodicData = IdentifyImportantNodes.BuildPeriodicBoundaryData(boundary, DomainTolerance, _globalNodes);

            return new FEMesh( _globalNodes,_elements, periodicData.Pairs,periodicData.X1Nodes,periodicData.Y1Nodes,
                periodicData.PinnedNode);
        }

        private int AddOrGetGlobalNode(Point2D node)
        {
            string key = $"{node.X:F10}_{node.Y:F10}";

            if (_nodeToIndex.TryGetValue(key, out int index))
            {
                return index;
            }

            index = _globalNodes.Count;
            _globalNodes.Add(node);
            _nodeToIndex[key] = index;
            return index;
        }

        /// <summary>
        /// Writes an error file documenting critical fiber overlap issues.
        /// File is named {baseFileName}_error.txt.
        /// </summary>
        private void WriteCriticalOverlapErrorFile( Node[] triangleNodes,IReadOnlyList<Fiber> fibers,
            FiberOverlapInfo[] overlapInfo,DebugOptions? dOptions)
        {
            if (dOptions == null || string.IsNullOrEmpty(dOptions.Directory) || string.IsNullOrEmpty(dOptions.FileName))
                return;

            string errorFileName = Path.Combine(dOptions.Directory, dOptions.FileName + "_error.txt");

            using (StreamWriter writer = new StreamWriter(errorFileName, append: true))
            {
                writer.WriteLine("=== Critical Fiber Overlap Detected ===");
                writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();

                // Write triangle information
                writer.WriteLine("Triangle Nodes:");
                for (int i = 0; i < 3; i++)
                {
                    var node = triangleNodes[i];
                    if (node.Type == NodeType.FiberCenter || node.Type == NodeType.ProjectedFiber)
                    {
                        writer.WriteLine($"  Node {i}: Fiber ID {node.FiberId}, " +
                            $"Position ({node.P.X:F6}, {node.P.Y:F6})");
                    }
                    else
                    {
                        writer.WriteLine($"  Node {i}: Boundary point, " +
                            $"Position ({node.P.X:F6}, {node.P.Y:F6})");
                    }
                }
                writer.WriteLine();

                // Write overlap details for each fiber with critical overlap
                writer.WriteLine("Critical Overlaps:");
                for (int i = 0; i < 3; i++)
                {
                    if (overlapInfo[i].HasCriticalOverlap)
                    {
                        var node = triangleNodes[i];
                        var fiber = fibers[node.FiberId];
                        var otherIndices = GetOtherIndices(i);
                        var otherNode1 = triangleNodes[otherIndices[0]];
                        var otherNode2 = triangleNodes[otherIndices[1]];

                        double minDist = CalculatePointToLineDistance(
                            node.P,
                            otherNode1.P,
                            otherNode2.P);

                        writer.WriteLine($"  Fiber {node.FiberId}:");
                        writer.WriteLine($"    Radius: {fiber.Radius:F6}");
                        writer.WriteLine($"    Distance to opposite edge: {minDist:F6}");
                        writer.WriteLine($"    Critical threshold: {fiber.Radius + fiber.Radius / 20.0:F6}");
                        writer.WriteLine($"    Fiber surface crosses or is too close to triangle edge.");
                    }
                }

                writer.WriteLine();
                writer.WriteLine("This indicates the triangulation needs improvement at this location.");
                writer.WriteLine("========================================");
                writer.WriteLine();
            }
        }

        /// <summary>
        /// Returns the indices of the other two elements in a 3-element array.
        /// </summary>
        private int[] GetOtherIndices(int currentIndex)
        {
            return currentIndex switch
            {
                0 => new[] { 1, 2 },
                1 => new[] { 0, 2 },
                2 => new[] { 0, 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(currentIndex))
            };
        }

        /// <summary>
        /// Classifies an edge based on the node types of its two endpoints.
        /// </summary>
        private EdgeType ClassifyEdgeType(Node node1, Node node2)
        {
            bool node1IsFiber = node1.Type == NodeType.FiberCenter || node1.Type == NodeType.ProjectedFiber;
            bool node2IsFiber = node2.Type == NodeType.FiberCenter || node2.Type == NodeType.ProjectedFiber;

            if (node1IsFiber && node2IsFiber)
                return EdgeType.TwoFibers;
            else if (node1IsFiber || node2IsFiber)
                return EdgeType.OneFiberOneBoundary;
            else
                return EdgeType.TwoBoundaries;
        }

        /// <summary>
        /// Converts a projection vector to a direction tuple (ox, oy).
        /// </summary>
        private (int ox, int oy) GetProjectionDirectionFromVector(double[] projectionVector)
        {
            if (projectionVector == null || projectionVector.Length < 2)
                return (0, 0);

            double x = projectionVector[0];
            double y = projectionVector[1];
            double tolerance = 1e-6;

            // Determine direction based on the projection vector
            if (Math.Abs(y) < tolerance) // Horizontal projection
            {
                if (x > tolerance) return (1, 0);   // Right
                if (x < -tolerance) return (-1, 0); // Left
            }
            else if (Math.Abs(x) < tolerance) // Vertical projection
            {
                if (y > tolerance) return (0, 1);   // Top
                if (y < -tolerance) return (0, -1); // Bottom
            }

            return (0, 0);
        }

        /// <summary>
        /// Checks if a shared edge is in counter-clockwise order within a triangle.
        /// Based on MATLAB Triad.CheckIfSharedEdgeIsCCWOrder (lines 119-130).
        /// Handles both fiber-fiber edges and fiber-boundary edges.
        /// </summary>
        private bool CheckIfSharedEdgeIsCCWOrder(Node[] triangleNodes, Node[] sharedEdgeNodes)
        {
            // Find indices of the shared edge nodes in the triangle
            int idx1 = -1;
            int idx2 = -1;

            for (int i = 0; i < triangleNodes.Length; i++)
            {
                if (NodesMatch(triangleNodes[i], sharedEdgeNodes[0]))
                    idx1 = i;
                if (NodesMatch(triangleNodes[i], sharedEdgeNodes[1]))
                    idx2 = i;
            }

            if (idx1 == -1 || idx2 == -1)
                throw new InvalidOperationException("Shared edge nodes not found in triangle");

            // Check if edge is in CCW order
            if (idx1 == 0 && idx2 == 2)
                return false;
            else if (idx1 < idx2 || (idx1 == triangleNodes.Length - 1 && idx2 == 0))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Adjusts the middle nodes of fiber elements if fibers are too close together.
        /// Based on MATLAB FE_Mesh.ChangeMiddleNodeIfFibersAreTooClose (lines 967-999).
        /// </summary>
        private (Point2D[] fiber1Nodes, Point2D[] fiber2Nodes) ChangeMiddleNodeIfFibersAreTooClose(
            Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, double radius1, double radius2)
        {
            double distanceBetweenFibers = MathHelper.CalcDistanceBetweenTwoPoints(fiber1Nodes[0], fiber2Nodes[0]);
            double sumOfRadii = radius1 + radius2;
            double ratio = sumOfRadii / distanceBetweenFibers;

            if (ratio >= 0.90)
            {
                // Create vectors connecting fiber centers
                var v12 = MathHelper.MakeVector2D(fiber1Nodes[0], fiber2Nodes[0]);
                var v21 = MathHelper.MakeVector2D(fiber2Nodes[0], fiber1Nodes[0]);

                // Calculate angles
                double t12 = Math.Atan2(v12.Y, v12.X);
                double t21 = Math.Atan2(v21.Y, v21.X);

                // Update middle node locations
                var fiber1MiddleNode = new Point2D(
                    fiber1Nodes[0].X + radius1 * Math.Cos(t12),
                    fiber1Nodes[0].Y + radius1 * Math.Sin(t12));

                var fiber2MiddleNode = new Point2D(
                    fiber2Nodes[0].X + radius2 * Math.Cos(t21),
                    fiber2Nodes[0].Y + radius2 * Math.Sin(t21));

                // Copy arrays and update middle node (node 3 for 6-node element)
                var newFiber1Nodes = (Point2D[])fiber1Nodes.Clone();
                var newFiber2Nodes = (Point2D[])fiber2Nodes.Clone();

                newFiber1Nodes[3] = fiber1MiddleNode;
                newFiber2Nodes[3] = fiber2MiddleNode;

                return (newFiber1Nodes, newFiber2Nodes);
            }

            return (fiber1Nodes, fiber2Nodes);
        }

        /// <summary>
        /// Checks if a triangle shares an edge with two given nodes.
        /// Matches both FiberId AND Offset for fiber nodes, or position for boundary nodes.
        /// This prevents connecting original fibers to projected ones.
        /// </summary>
        private bool SharesEdge(Node[] triangleNodes, Node edgeNode1, Node edgeNode2)
        {
            int matchCount = 0;

            foreach (var node in triangleNodes)
            {
                // Check if node matches edgeNode1
                if (NodesMatch(node, edgeNode1))
                {
                    matchCount++;
                }
                // Check if node matches edgeNode2
                else if (NodesMatch(node, edgeNode2))
                {
                    matchCount++;
                }
            }

            return matchCount == 2;
        }

        /// <summary>
        /// Checks if two nodes represent the same point.
        /// For fiber nodes: matches FiberId and Offset.
        /// For boundary nodes: matches position.
        /// </summary>
        private bool NodesMatch(Node node1, Node node2)
        {
            // For fiber nodes: match by FiberId and Offset
            if (node1.FiberId != -1 && node2.FiberId != -1)
            {
                return node1.FiberId == node2.FiberId && node1.Offset == node2.Offset;
            }
            // For boundary nodes: match by position
            else if ((node1.FiberId == -1) && (node2.FiberId == -1))
            {
                return Math.Abs(node1.P.X - node2.P.X) < NodeTolerance &&
                       Math.Abs(node1.P.Y - node2.P.Y) < NodeTolerance &&
                       node1.Offset == node2.Offset;
            }
            // Mixed fiber/boundary: no match
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a unique key for an edge including offset information.
        /// Handles both fiber nodes (with FiberId) and boundary nodes (without FiberId).
        /// This prevents matching original fibers with their projections.
        /// </summary>
        private string GetEdgeKey(Node node1, Node node2)
        {
            // Create a unique key that handles both fiber and boundary nodes
            string key1 = node1.FiberId!=-1
                ? $"F{node1.FiberId}_{node1.Offset.ox}_{node1.Offset.oy}"
                : $"B{node1.P.X:F10}_{node1.P.Y:F10}_{node1.Offset.ox}_{node1.Offset.oy}";

            string key2 = node2.FiberId != -1
                ? $"F{node2.FiberId}_{node2.Offset.ox}_{node2.Offset.oy}"
                : $"B{node2.P.X:F10}_{node2.P.Y:F10}_{node2.Offset.ox}_{node2.Offset.oy}";

            // Order-independent key
            if (string.CompareOrdinal(key1, key2) < 0)
                return $"{key1}|{key2}";
            else
                return $"{key2}|{key1}";
        }

        private Point2D[] ReconstructTriangleElementNodes( Node[] nodes, IReadOnlyList<Fiber> fibers)
        {
            var elementNodes = new Point2D[3];
            var overlapInfo = DetectFiberOverlaps(nodes, fibers);

            for (int i = 0; i < 3; i++)
            {
                if (nodes[i].Type == NodeType.FiberCenter || nodes[i].Type == NodeType.ProjectedFiber)
                {
                    var fiber = fibers[nodes[i].FiberId];
                    var otherIndices = GetOtherIndices(i);

                    elementNodes[i] = CalculateFiberSurfacePoint(
                        nodes[i].P,
                        fiber.Radius,
                        nodes[otherIndices[0]].P,
                        nodes[otherIndices[1]].P,
                        overlapInfo[i].NeedsAdjustment,
                        i,
                        overlapInfo);
                }
                else
                {
                    elementNodes[i] = nodes[i].P;
                }
            }

            return elementNodes;
        }

        #region Add Methods

        private void AddElement(ElementPhase phase, ElementBuildResult result)
        {
            var element = new Element(_elementIdCounter++, phase, result.ElementName, result.Nodes);
            _elements.Add(element);

            foreach (var node in result.Nodes)
                AddOrGetGlobalNode(node);
        }
        #endregion

        #region Find / Determine Methods

        private bool IsProjectedFiberNode(Node node)
        {
            return node.Type == NodeType.ProjectedFiber
                && node.FiberId != -1
                && node.Offset != (0, 0);
        }

        private bool IsBoundaryOrCornerPoint(Node node)
        {
            return node.Type == NodeType.ProjectedBoundary
                || node.Type == NodeType.BoundaryCorner;
        }

        /// <summary>
        /// Detects if any fibers in the triangle are too close to the opposite edge.
        /// Uses MATLAB's two-tier threshold approach:
        /// - Critical overlap (factor 20): fiber surface crossing edge - should throw
        /// - Adjustment needed (factor 2): narrow inner triangle - should adjust midpoint
        /// Based on MATLAB Triad.DetermineIfFibersOverlapTriad and willInteriorElementHaveToBeAdjusted.
        /// </summary>
        private FiberOverlapInfo[] DetectFiberOverlaps(Node[] triangleNodes, IReadOnlyList<Fiber> fibers)
        {
            return TriangleQuality.DetectFiberOverlaps(triangleNodes, fibers);
        }

        /// <summary>
        /// Finds all shared edges between adjacent triangles and reconstructs their element nodes.
        /// Returns edge data classified by edge type (TwoFibers, OneFiberOneBoundary, TwoBoundaries).
        /// </summary>
        private List<EdgeData> FindSharedEdgesForFiberElements(TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers, DebugOptions? dOptions = null)
        {
            var edgeDataList = new List<EdgeData>();

            // Store which triangle elements we've already built (mapping from triangle index to built interior triangle nodes)
            var triangleElements = new Dictionary<int, Point2D[]>();

            // Reconstruct interior triangle element nodes for all triangles
            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                var tri = triangulation.Triangles[i];
                var nodeA = triangulation.Nodes[tri[0]];
                var nodeB = triangulation.Nodes[tri[1]];
                var nodeC = triangulation.Nodes[tri[2]];
                var nodes = new[] { nodeA, nodeB, nodeC };

                // Reconstruct the interior triangle element nodes the same way we built them
                var elementNodes = new Point2D[3];
                var overlapInfo = DetectFiberOverlaps(nodes, fibers);

                // Check for critical overlaps and write error file if found
                bool hasCriticalOverlap = false;
                for (int j = 0; j < 3; j++)
                {
                    if (overlapInfo[j].HasCriticalOverlap)
                    {
                        hasCriticalOverlap = true;
                        break;
                    }
                }

                if (hasCriticalOverlap)
                {
                    WriteCriticalOverlapErrorFile(nodes, fibers, overlapInfo, dOptions);
                    // Skip processing this triangle since it has critical overlap
                    continue;
                }

                for (int j = 0; j < 3; j++)
                {
                    var currentNode = nodes[j];
                    var otherIndices = GetOtherIndices(j);
                    var otherNode1 = nodes[otherIndices[0]];
                    var otherNode2 = nodes[otherIndices[1]];

                    // Calculate surface point for fiber centers, use point as-is for boundary nodes
                    if (currentNode.Type == NodeType.FiberCenter || currentNode.Type == NodeType.ProjectedFiber)
                    {
                        var fiber = fibers[currentNode.FiberId];
                        Point2D fiberCenter = currentNode.P;

                        elementNodes[j] = CalculateFiberSurfacePoint(
                            fiberCenter,
                            fiber.Radius,
                            otherNode1.P,
                            otherNode2.P,
                            overlapInfo[j].NeedsAdjustment,
                            j,
                            overlapInfo);
                    }
                    else
                    {
                        // Boundary node - use as is
                        elementNodes[j] = currentNode.P;
                    }
                }

                triangleElements[i] = elementNodes;
            }

            // Find adjacent triangle pairs and classify edges
            var processedEdges = new HashSet<string>();

            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                if (!triangleElements.ContainsKey(i))
                    continue;

                var tri1 = triangulation.Triangles[i];
                var nodes1 = new[] {
                    triangulation.Nodes[tri1[0]],
                    triangulation.Nodes[tri1[1]],
                    triangulation.Nodes[tri1[2]]
                };

                // Find all edges of this triangle
                var edges = new[] {
                    (nodes1[0], nodes1[1]),
                    (nodes1[1], nodes1[2]),
                    (nodes1[2], nodes1[0])
                };

                // Check each edge for adjacent triangles
                for (int edgeIdx = 0; edgeIdx < edges.Length; edgeIdx++)
                {
                    var edge = edges[edgeIdx];
                    var edgeKey = GetEdgeKey(edge.Item1, edge.Item2);

                    if (processedEdges.Contains(edgeKey))
                        continue; // Already processed this edge

                    // Find adjacent triangle sharing this edge
                    for (int j = i + 1; j < triangulation.Triangles.Count; j++)
                    {
                        if (!triangleElements.ContainsKey(j))
                            continue;

                        var tri2 = triangulation.Triangles[j];
                        var nodes2 = new[] {
                            triangulation.Nodes[tri2[0]],
                            triangulation.Nodes[tri2[1]],
                            triangulation.Nodes[tri2[2]]
                        };

                        // Check if this triangle shares the edge
                        bool sharesEdge = SharesEdge(nodes2, edge.Item1, edge.Item2);

                        if (sharesEdge)
                        {
                            // Mark edge as processed
                            processedEdges.Add(edgeKey);

                            // Classify the edge type based on the shared edge nodes
                            var edgeType = ClassifyEdgeType(edge.Item1, edge.Item2);

                            // Store edge data with classification
                            var edgeData = new EdgeData(
                                nodes1,
                                nodes2,
                                triangleElements[i],
                                triangleElements[j],
                                new[] { edge.Item1, edge.Item2 },
                                edgeType);

                            edgeDataList.Add(edgeData);

                            break; // Only one adjacent triangle per edge
                        }
                    }
                }
            }

            return edgeDataList;
        }

        /// <summary>
        /// Finds all periodic fiber pairs on boundary edges.
        /// Uses the fibers' HasProjectedFibers property to identify boundary fibers,
        /// then finds triangles containing pairs of such fibers.
        /// Based on MATLAB's CreateTableOfOriginalAndProjectedFibersOnBoundary / FindEdgeFiberPairs logic.
        /// </summary>
        private List<PeriodicFiberPair> FindPeriodicFiberPairs(TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers, CellBoundary boundary)
        {
            var pairs = new List<PeriodicFiberPair>();
            var processedPairs = new HashSet<(int, int, int, int)>(); // To avoid duplicates

            // For each triangle, check if it contains an edge with two projected fibers
            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];
                var nodes = new[] {
                    triangulation.Nodes[tri[0]],
                    triangulation.Nodes[tri[1]],
                    triangulation.Nodes[tri[2]]
                };

                // Check each edge of the triangle
                for (int edgeIdx = 0; edgeIdx < 3; edgeIdx++)
                {
                    var node1 = nodes[edgeIdx];
                    var node2 = nodes[(edgeIdx + 1) % 3];

                    // Look for edges with two PROJECTED fiberss and  (same projection direction)
                    bool bothProjected = node1.Type == NodeType.ProjectedFiber && node2.Type == NodeType.ProjectedFiber;
                    bool isBoundaryEdge = false;
                    if (bothProjected)
                    {
                        //check that it is an outside edge only if both are projected
                        int trianglesWithFibers = CountTrianglesSharingProjectedEdge(triangulation, node1, node2);
                        isBoundaryEdge = trianglesWithFibers == 1 ? true : false;
                    }

                    if (bothProjected && isBoundaryEdge)
                    {
                        if (!TryFindPeriodicPartnerForProjectedEdge(triangulation, node1, node2, boundary,
                            out int originalTriIdx, out var projectionDirection))
                        {
                            continue;
                        }

                        int projFiber1Id = node1.FiberId;
                        int projFiber2Id = node2.FiberId;

                        var pairKey = (
                            Math.Min(projFiber1Id, projFiber2Id),
                            Math.Max(projFiber1Id, projFiber2Id),
                            Math.Min(originalTriIdx, triIdx),
                            Math.Max(originalTriIdx, triIdx)
                        );

                        if (processedPairs.Contains(pairKey))
                            continue;

                        processedPairs.Add(pairKey);

                        pairs.Add(new PeriodicFiberPair
                        {
                            Fiber1Id = projFiber1Id,
                            Fiber2Id = projFiber2Id,
                            OriginalTriangleIndex = originalTriIdx,
                            ProjectedTriangleIndex = triIdx,
                            ProjectionDirection = projectionDirection
                        });
                    }
                }
            }
            return pairs;
        }

        //For the bundary edges, finds the triangle with the same fiber pair
        private bool TryFindPeriodicPartnerForProjectedEdge(TriangulationMesh2D triangulation, Node projA, Node projB, CellBoundary boundary, out int partnerTriangleIndex, out (int ox, int oy) projectionDirection)
        {
            partnerTriangleIndex = -1;
            projectionDirection = (0, 0);

            Point2D projectedMid = new Point2D(0.5 * (projA.P.X + projB.P.X), 0.5 * (projA.P.Y + projB.P.Y));

            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];

                var nodes = new[]
                {
            triangulation.Nodes[tri[0]],
            triangulation.Nodes[tri[1]],
            triangulation.Nodes[tri[2]]
        };

                var edges = new[]
                {
            (nodes[0], nodes[1]),
            (nodes[1], nodes[2]),
            (nodes[2], nodes[0])
        };

                foreach (var edge in edges)
                {
                    Node a = edge.Item1;
                    Node b = edge.Item2;

                    bool sameFiberPair =
                        (a.FiberId == projA.FiberId && b.FiberId == projB.FiberId) ||
                        (a.FiberId == projB.FiberId && b.FiberId == projA.FiberId);

                    if (!sameFiberPair)
                        continue;

                    bool isSameNodes = (NodesMatch(a, projA) && NodesMatch(b, projB)) || (NodesMatch(a, projB) && NodesMatch(b, projA));
                    
                    if (isSameNodes)
                        continue;

                    bool isEdge = CountTrianglesSharingProjectedEdge(triangulation, a, b) == 1;

                    if (!isEdge)
                        continue;

                    Point2D partnerMid = new Point2D(0.5 * (a.P.X + b.P.X), 0.5 * (a.P.Y + b.P.Y));

                    bool gotProjectionDirection = TryGetProjectionDirectionFromEdgeMidpoints(projectedMid, partnerMid, boundary, out projectionDirection);

                    if (!gotProjectionDirection)
                        continue;

                    partnerTriangleIndex = triIdx;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetProjectionDirectionFromEdgeMidpoints(Point2D projectedMid, Point2D partnerMid, CellBoundary boundary, out (int ox, int oy) direction)
        {
            direction = (0, 0);

            double lx = boundary.ODimensions[1];
            double ly = boundary.ODimensions[2];

            double rawOx = (projectedMid.X - partnerMid.X) / lx;
            double rawOy = (projectedMid.Y - partnerMid.Y) / ly;

            int ox = (int)Math.Round(rawOx);
            int oy = (int)Math.Round(rawOy);

            double tol = 1e-6;

            if (Math.Abs(rawOx - ox) > tol || Math.Abs(rawOy - oy) > tol)
                return false;

            direction = (ox, oy);
            return true;
        }

        /// <summary>
        /// Finds a triangle containing two specific fiber centers (original, not projected).
        /// </summary>
        private int FindTriangleWithTwoOriginalFibers(TriangulationMesh2D triangulation,
            int fiber1Id, int fiber2Id)
        {
            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];
                var nodes = new[] {
                    triangulation.Nodes[tri[0]],
                    triangulation.Nodes[tri[1]],
                    triangulation.Nodes[tri[2]]
                };

                int matchCount = 0;
                foreach (var node in nodes)
                {
                    if (node.Type == NodeType.FiberCenter &&
                        node.Offset == (0, 0) &&
                        (node.FiberId == fiber1Id || node.FiberId == fiber2Id))
                    {
                        matchCount++;
                    }
                }

                if (matchCount == 2)
                    return triIdx;
            }

            return -1;
        }

        /// <summary>
        /// Fallback method to find periodic fiber pairs based on position when HasProjectedFibers is not available.
        /// </summary>
        private List<PeriodicFiberPair> FindPeriodicFiberPairsByPosition(TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers, CellBoundary boundary)
        {
            var pairs = new List<PeriodicFiberPair>();
            var processedPairs = new HashSet<(int, int, int, int)>();

            double maxX = boundary.ODimensions[1];
            double maxY = boundary.ODimensions[2];

            // For each triangle, check if it contains an edge with two fibers near the same boundary
            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];
                var nodes = new[] {
                    triangulation.Nodes[tri[0]],
                    triangulation.Nodes[tri[1]],
                    triangulation.Nodes[tri[2]]
                };

                // Check each edge of the triangle
                for (int edgeIdx = 0; edgeIdx < 3; edgeIdx++)
                {
                    var node1 = nodes[edgeIdx];
                    var node2 = nodes[(edgeIdx + 1) % 3];

                    // Both nodes must be original fiber centers
                    if (node1.Type != NodeType.FiberCenter || node2.Type != NodeType.FiberCenter)
                        continue;
                    if (node1.Offset != (0, 0) || node2.Offset != (0, 0))
                        continue;
                    if (node1.FiberId==-1 || node2.FiberId == -1)
                        continue;

                    int fiber1Id = node1.FiberId;
                    int fiber2Id = node2.FiberId;
                    var fiber1 = fibers[fiber1Id];
                    var fiber2 = fibers[fiber2Id];

                    // Use fiber radius as tolerance for boundary detection
                    double tolerance = Math.Max(fiber1.Radius, fiber2.Radius) * 1.5;

                    // Check if both are near the same boundary
                    var projectionDirection = (0, 0);

                    if (Math.Abs(node1.P.X) < tolerance && Math.Abs(node2.P.X) < tolerance)
                        projectionDirection = (1, 0); // Left edge -> projects right
                    else if (Math.Abs(node1.P.X - maxX) < tolerance && Math.Abs(node2.P.X - maxX) < tolerance)
                        projectionDirection = (-1, 0); // Right edge -> projects left
                    else if (Math.Abs(node1.P.Y) < tolerance && Math.Abs(node2.P.Y) < tolerance)
                        projectionDirection = (0, 1); // Bottom edge -> projects top
                    else if (Math.Abs(node1.P.Y - maxY) < tolerance && Math.Abs(node2.P.Y - maxY) < tolerance)
                        projectionDirection = (0, -1); // Top edge -> projects bottom

                    if (projectionDirection == (0, 0))
                        continue;

                    Console.WriteLine($"Debug: Found edge fibers {fiber1Id} and {fiber2Id} on boundary, projection direction {projectionDirection}");

                    // Find the triangle containing both projected fibers
                    int projectedTriIdx = FindTriangleWithTwoProjectedFibers(
                        triangulation,
                        fiber1Id,
                        fiber2Id,
                        projectionDirection);

                    if (projectedTriIdx == -1)
                    {
                        Console.WriteLine($"Debug: Could not find projected triangle for fibers {fiber1Id} and {fiber2Id}");
                        continue;
                    }

                    // Avoid duplicate pairs
                    var pairKey = (
                        Math.Min(fiber1Id, fiber2Id),
                        Math.Max(fiber1Id, fiber2Id),
                        Math.Min(triIdx, projectedTriIdx),
                        Math.Max(triIdx, projectedTriIdx)
                    );

                    if (processedPairs.Contains(pairKey))
                        continue;

                    processedPairs.Add(pairKey);

                    Console.WriteLine($"Debug: Adding pair: fibers {fiber1Id},{fiber2Id}, triangles {triIdx},{projectedTriIdx}");

                    pairs.Add(new PeriodicFiberPair
                    {
                        Fiber1Id = fiber1Id,
                        Fiber2Id = fiber2Id,
                        OriginalTriangleIndex = triIdx,
                        ProjectedTriangleIndex = projectedTriIdx,
                        ProjectionDirection = projectionDirection
                    });
                }
            }

            return pairs;
        }

        /// <summary>
        /// Finds the triangle that contains the projected versions of two fibers.
        /// </summary>
        private int FindTriangleWithTwoProjectedFibers(TriangulationMesh2D triangulation,
            int fiber1Id, int fiber2Id, (int ox, int oy) projectionDirection)
        {
            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];
                var nodes = new[] {
                    triangulation.Nodes[tri[0]],
                    triangulation.Nodes[tri[1]],
                    triangulation.Nodes[tri[2]]
                };

                // Count how many nodes match our projected fibers
                int matchCount = 0;
                foreach (var node in nodes)
                {
                    if (node.Type == NodeType.ProjectedFiber &&
                        node.Offset == projectionDirection &&
                        node.FiberId != -1 &&
                        (node.FiberId == fiber1Id || node.FiberId == fiber2Id))
                    {
                        matchCount++;
                    }
                }

                // If we found both projected fibers in this triangle, we have a match
                if (matchCount == 2)
                    return triIdx;
            }

            return -1;  // No matching triangle found
        }

        /// <summary>
        /// Finds the interior triangle element node that corresponds to a specific fiber.
        /// Since elementNodes[j] is calculated from triangleNodes[j].FiberId, we can directly map them.
        /// This is more reliable than distance-based search, especially when fibers are close together.
        /// Now matches both FiberId AND Offset to handle periodic boundaries correctly.
        /// </summary>
        private Point2D FindInteriorTriangleNodeByFiberId(Node[] triangleNodes, Point2D[] elementNodes, Node targetNode)
        {
            for (int i = 0; i < triangleNodes.Length; i++)
            {
                if (triangleNodes[i].FiberId == targetNode.FiberId &&
                    triangleNodes[i].Offset == targetNode.Offset)
                {
                    return elementNodes[i];
                }
            }
            throw new InvalidOperationException($"Could not find element node for fiber {targetNode.FiberId} with offset ({targetNode.Offset.ox},{targetNode.Offset.oy}) in triangle");
        }

        /// <summary>
        /// Finds the interior triangle element node that corresponds to any node (fiber or boundary).
        /// Matches FiberId and Offset for fiber nodes, or position for boundary nodes.
        /// </summary>
        private Point2D FindInteriorTriangleNodeByNode(Node[] triangleNodes, Point2D[] elementNodes, Node targetNode)
        {
            for (int i = 0; i < triangleNodes.Length; i++)
            {
                // For fiber nodes: match by FiberId and Offset
                if (targetNode.Type == NodeType.FiberCenter || targetNode.Type == NodeType.ProjectedFiber)
                {
                    if (triangleNodes[i].FiberId == targetNode.FiberId &&
                        triangleNodes[i].Offset == targetNode.Offset)
                    {
                        return elementNodes[i];
                    }
                }
                // For boundary nodes: match by position (FiberId is null)
                else if (triangleNodes[i].Type == targetNode.Type &&
                         Math.Abs(triangleNodes[i].P.X - targetNode.P.X) < NodeTolerance &&
                         Math.Abs(triangleNodes[i].P.Y - targetNode.P.Y) < NodeTolerance)
                {
                    return elementNodes[i];
                }
            }
            throw new InvalidOperationException($"Could not find element node for target node in triangle");
        }

        
        /// <summary>
        /// Finds all pairs of projected fiber nodes and boundary nodes that lie on the same edge, indicating a periodic connection.
        /// </summary>
        private List<PeriodicFiberBoundaryPair> FindPeriodicFiberBoundaryPairs(TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers, CellBoundary boundary)
        {
            var pairs = new List<PeriodicFiberBoundaryPair>();
            var processed = new HashSet<string>();

            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];

                var nodes = new[]{triangulation.Nodes[tri[0]], triangulation.Nodes[tri[1]], triangulation.Nodes[tri[2]] };

                for (int edgeIdx = 0; edgeIdx < 3; edgeIdx++)
                {
                    var a = nodes[edgeIdx];
                    var b = nodes[(edgeIdx + 1) % 3];

                    Node projectedFiberNode;
                    Node boundaryNode;

                    if (IsProjectedFiberNode(a) && IsBoundaryOrCornerPoint(b))
                    {
                        projectedFiberNode = a;
                        boundaryNode = b;
                    }
                    else if (IsProjectedFiberNode(b) && IsBoundaryOrCornerPoint(a))
                    {
                        projectedFiberNode = b;
                        boundaryNode = a;
                    }
                    else
                    {
                        continue;
                    }

                    var direction = projectedFiberNode.Offset;
                    if (direction == (0, 0))
                        continue;

                    var shift = GetPeriodicShift(boundary, direction);

                    //TODO: I maybe need to have the corner boundary points linked with the other corners.  
                    var originalBoundaryPoint = new Point2D( boundaryNode.P.X - shift.X,
                        boundaryNode.P.Y - shift.Y);

                    int originalTriIdx = FindTriangleWithOriginalFiberAndBoundaryPoint(triangulation,
                        projectedFiberNode.FiberId,originalBoundaryPoint);

                    //This skips if the triangle with the original fiber and boundary pt wasn't found.  
                    if (originalTriIdx < 0)
                        continue;

                    string key = $"{projectedFiberNode.FiberId}_{originalTriIdx}_{triIdx}_{boundaryNode.P.X:F10}_{boundaryNode.P.Y:F10}";

                    if (!processed.Add(key))
                        continue;

                    pairs.Add(new PeriodicFiberBoundaryPair
                    {
                        FiberId = projectedFiberNode.FiberId,
                        OriginalTriangleIndex = originalTriIdx,
                        ProjectedTriangleIndex = triIdx,
                        ProjectionDirection = direction,
                        BoundaryPoint = boundaryNode.P
                    });
                }
            }

            return pairs;
        }

        private int FindTriangleWithOriginalFiberAndBoundaryPoint( TriangulationMesh2D triangulation,int fiberId,
            Point2D boundaryPoint)
        {
            for (int triIdx = 0; triIdx < triangulation.Triangles.Count; triIdx++)
            {
                var tri = triangulation.Triangles[triIdx];

                var nodes = new[] {triangulation.Nodes[tri[0]],triangulation.Nodes[tri[1]], triangulation.Nodes[tri[2]]};

                bool hasOriginalFiber = nodes.Any(n =>
                    n.Type == NodeType.FiberCenter &&
                    n.Offset == (0, 0) &&
                    n.FiberId == fiberId);

                bool hasBoundaryPoint = nodes.Any(n =>
                    n.Type != NodeType.FiberCenter &&
                    n.Type != NodeType.ProjectedFiber &&
                    Math.Abs(n.P.X - boundaryPoint.X) < NodeTolerance &&
                    Math.Abs(n.P.Y - boundaryPoint.Y) < NodeTolerance);

                if (hasOriginalFiber && hasBoundaryPoint)
                    return triIdx;
            }

            return -1;
        }

        private int CountTrianglesSharingProjectedEdge( TriangulationMesh2D triangulation, Node a, Node b)
        {
            int count = 0;

            foreach (var tri in triangulation.Triangles)
            {
                var nodes = new[]{ triangulation.Nodes[tri[0]], triangulation.Nodes[tri[1]], triangulation.Nodes[tri[2]]};

                if (SharesEdge(nodes, a, b))
                    count++;
            }

            return count;
        }
        #endregion

        #region Builder Methods


        private void BuildBoundaryFiberMatrixElementsForFiberBoundaryPair(PeriodicFiberBoundaryPair pair,
            TriangulationMesh2D triangulation, IReadOnlyList<Fiber> fibers,CellBoundary boundary, ElementConfig config)
        {
            var origTri = triangulation.Triangles[pair.OriginalTriangleIndex];
            var projTri = triangulation.Triangles[pair.ProjectedTriangleIndex];

            var origNodes = new[] {triangulation.Nodes[origTri[0]],
                triangulation.Nodes[origTri[1]],triangulation.Nodes[origTri[2]]};

            var projNodes = new[] {triangulation.Nodes[projTri[0]],
                triangulation.Nodes[projTri[1]],triangulation.Nodes[projTri[2]] };

            var origElementNodes = ReconstructTriangleElementNodes(origNodes, fibers);
            var projElementNodes = ReconstructTriangleElementNodes(projNodes, fibers);

            var originalFiberNode = origNodes.First(n =>
                n.Type == NodeType.FiberCenter &&
                n.FiberId == pair.FiberId);

            var projectedFiberNode = projNodes.First(n =>
                n.Type == NodeType.ProjectedFiber &&
                n.FiberId == pair.FiberId &&
                n.Offset == pair.ProjectionDirection);

            var boundaryNode = projNodes.First(n =>
                n.Type != NodeType.FiberCenter &&
                n.Type != NodeType.ProjectedFiber &&
                Math.Abs(n.P.X - pair.BoundaryPoint.X) < NodeTolerance &&
                Math.Abs(n.P.Y - pair.BoundaryPoint.Y) < NodeTolerance);

            var originalFiberSurfacePoint =
                FindInteriorTriangleNodeByNode(origNodes, origElementNodes, originalFiberNode);

            var projectedFiberSurfacePoint =
                FindInteriorTriangleNodeByNode(projNodes, projElementNodes, projectedFiberNode);

            var boundaryPoint =
                FindInteriorTriangleNodeByNode(projNodes, projElementNodes, boundaryNode);

            var shift = GetPeriodicShift(boundary, pair.ProjectionDirection);

            var originalFiberSurfacePointProjected = new Point2D(
                originalFiberSurfacePoint.X + shift.X,
                originalFiberSurfacePoint.Y + shift.Y);

            var projectedFiberCenter = projectedFiberNode.P;

            var fiber = fibers[pair.FiberId];

            bool isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(
                projNodes,
                new[] { projectedFiberNode, boundaryNode });

            var fiberResult = _elementBuilder.BuildFiberTriangle(
                projectedFiberCenter, originalFiberSurfacePointProjected,
                projectedFiberSurfacePoint, fiber.Radius, isEdgeCCW);

            AddElement(ElementPhase.Fiber, fiberResult);

            var matrixResult = _elementBuilder.BuildFiberBoundaryMatrixTriangle(
                fiberResult.Nodes, boundaryPoint, isEdgeCCW);

            AddElement(ElementPhase.Matrix, matrixResult);
        }

        /// <summary>
        /// Builds fiber and matrix elements between adjacent triangles.
        /// Based on MATLAB FE_Mesh.BuildInteriorFiberMatrixElements (lines 253-300).
        /// </summary>
        private void BuildInteriorFiberMatrixElements(TriangulationMesh2D triangulation, IReadOnlyList<Fiber> fibers,
            ElementConfig config, DebugOptions? dOptions = null)
        {
            // Find all shared edges and classify them
            var edgeDataList = FindSharedEdgesForFiberElements(triangulation, fibers, dOptions);

            // Build fiber elements first (based on edge type)
            foreach (var edgeData in edgeDataList)
            {
                if (edgeData.Type == EdgeType.TwoFibers)
                {
                    // Build 2 fiber elements for fiber-fiber edge
                    BuildFiberElementsForSharedEdge(
                        edgeData.Triangle1Nodes,
                        edgeData.Triangle2Nodes,
                        edgeData.Triangle1ElementNodes,
                        edgeData.Triangle2ElementNodes,
                        edgeData.SharedEdgeNodes,
                        fibers,
                        config);
                }
                else if (edgeData.Type == EdgeType.OneFiberOneBoundary)
                {
                    // Build 1 fiber element for fiber-boundary edge
                    BuildSingleFiberElement(
                        edgeData.Triangle1Nodes,
                        edgeData.Triangle2Nodes,
                        edgeData.Triangle1ElementNodes,
                        edgeData.Triangle2ElementNodes,
                        edgeData.SharedEdgeNodes,
                        fibers,
                        config);
                }
                // TwoBoundaries: no fiber elements
            }

            // Write intermediate mesh: triangles + fiber elements (before quads)
            if (dOptions != null && dOptions.Debug)
            {
                var triPlusFibMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>(), null);
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("triPlusFib"), triPlusFibMesh);
            }

            // Now build matrix elements (quad or triangular based on edge type)
            foreach (var edgeData in edgeDataList)
            {
                if (edgeData.Type == EdgeType.TwoFibers)
                {
                    // Build 8-node quad element between two fibers
                    BuildQuadElementForSharedEdge(
                        edgeData.Triangle1Nodes,
                        edgeData.Triangle2Nodes,
                        edgeData.Triangle1ElementNodes,
                        edgeData.Triangle2ElementNodes,
                        edgeData.SharedEdgeNodes,
                        fibers,
                        config);
                }
                else if (edgeData.Type == EdgeType.OneFiberOneBoundary)
                {
                    // Build 6-node triangular matrix element between fiber and boundary
                    BuildTriangularMatrixElement(
                        edgeData.Triangle1Nodes,
                        edgeData.Triangle2Nodes,
                        edgeData.Triangle1ElementNodes,
                        edgeData.Triangle2ElementNodes,
                        edgeData.SharedEdgeNodes,
                        fibers,
                        config);
                }
                // TwoBoundaries: no matrix elements between boundaries
            }
        }

        /// <summary>
        /// Builds fiber and matrix elements for periodic boundary edges.
        /// Connects original fibers on one edge to their projected counterparts on the opposite edge.
        /// Based on MATLAB FE_Mesh.BuildBoundaryFiberMatrixElements (lines 304-344).
        /// </summary>
        private void BuildBoundaryFiberMatrixElements(TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers, CellBoundary boundary, ElementConfig config,
            DebugOptions? dOptions = null)
        {
            // Find all periodic fiber pairs (original fiber + projected fiber pairs that share a boundary edge)
            var periodicFiberPairs = FindPeriodicFiberPairs(triangulation, fibers, boundary);

            if (dOptions != null && dOptions.Debug)
            {
                Console.WriteLine($"Found {periodicFiberPairs.Count} periodic fiber pairs");
            }

            // Build fiber and matrix elements for each periodic pair
            foreach (var pair in periodicFiberPairs)
            {
                if (dOptions != null && dOptions.Debug)
                {
                    Console.WriteLine($"Building boundary elements for fibers {pair.Fiber1Id} and {pair.Fiber2Id}");
                }

                BuildBoundaryFiberMatrixElementsForPair(pair, triangulation, fibers, boundary, config);
            }

            // Now work on fiber-boundary matrix elements for any remaining boundary fibers that were not part of periodic pairs
            var fiberBoundaryPairs = FindPeriodicFiberBoundaryPairs(triangulation, fibers, boundary);

            foreach (var pair in fiberBoundaryPairs)
            {
                BuildBoundaryFiberMatrixElementsForFiberBoundaryPair( pair, triangulation, fibers, boundary, config);
            }
        }


        /// <summary>
        /// Builds fiber and matrix elements for a single periodic fiber pair.
        /// Creates 2 fiber elements and 1 quad matrix element connecting the original and projected triangles.
        /// </summary>
        private void BuildBoundaryFiberMatrixElementsForPair(PeriodicFiberPair pair, TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers, CellBoundary boundary, ElementConfig config)
        {
            // Get the partner triangle's element nodes
            var origTri = triangulation.Triangles[pair.OriginalTriangleIndex];
            var origNodes = new[] {
                triangulation.Nodes[origTri[0]],
                triangulation.Nodes[origTri[1]],
                triangulation.Nodes[origTri[2]]
            };

            // Get the projected triangle's element nodes
            var projTri = triangulation.Triangles[pair.ProjectedTriangleIndex];
            var projNodes = new[] {
                triangulation.Nodes[projTri[0]],
                triangulation.Nodes[projTri[1]],
                triangulation.Nodes[projTri[2]]
            };

            // Reconstruct interior triangle element nodes for both triangles
            var origOverlapInfo = DetectFiberOverlaps(origNodes, fibers);
            var projOverlapInfo = DetectFiberOverlaps(projNodes, fibers);

            // Build original triangle element nodes
            var origElementNodes = new Point2D[3];
            for (int i = 0; i < 3; i++)
            {
                if (origNodes[i].Type == NodeType.FiberCenter || origNodes[i].Type == NodeType.ProjectedFiber)
                {
                    var fiber = fibers[origNodes[i].FiberId];
                    var otherIndices = GetOtherIndices(i);

                    origElementNodes[i] = CalculateFiberSurfacePoint(
                        origNodes[i].P,
                        fiber.Radius,
                        origNodes[otherIndices[0]].P,
                        origNodes[otherIndices[1]].P,
                        origOverlapInfo[i].NeedsAdjustment,
                        i,
                        origOverlapInfo);
                }
                else
                {
                    origElementNodes[i] = origNodes[i].P;
                }
            }

            // Build projected triangle element nodes
            var projElementNodes = new Point2D[3];
            for (int i = 0; i < 3; i++)
            {
                if (projNodes[i].Type == NodeType.FiberCenter || projNodes[i].Type == NodeType.ProjectedFiber)
                {
                    var fiber = fibers[projNodes[i].FiberId];
                    var otherIndices = GetOtherIndices(i);

                    projElementNodes[i] = CalculateFiberSurfacePoint(
                        projNodes[i].P,
                        fiber.Radius,
                        projNodes[otherIndices[0]].P,
                        projNodes[otherIndices[1]].P,
                        projOverlapInfo[i].NeedsAdjustment,
                        i,
                        projOverlapInfo);
                }
                else
                {
                    projElementNodes[i] = projNodes[i].P;
                }
            }

            // Find the surface points for the two fibers in each triangle
            Point2D fiber1NodeOrig = new Point2D(0, 0);
            Point2D fiber1NodeProj = new Point2D(0, 0);
            Point2D fiber2NodeOrig = new Point2D(0, 0);
            Point2D fiber2NodeProj = new Point2D(0, 0);

            // Locate the reconstructed fiber surface points for the two matching fibers in the partner and projected triangles.
            for (int i = 0; i < 3; i++)
            {
                if (origNodes[i].FiberId == pair.Fiber1Id)
                    fiber1NodeOrig = origElementNodes[i];

                if (origNodes[i].FiberId == pair.Fiber2Id)
                    fiber2NodeOrig = origElementNodes[i];

                if (projNodes[i].FiberId == pair.Fiber1Id)
                    fiber1NodeProj = projElementNodes[i];

                if (projNodes[i].FiberId == pair.Fiber2Id)
                    fiber2NodeProj = projElementNodes[i];
            }
            //Now shift the nodes over using the periodic shift to ensure they are in the correct position for element building
            var periodicShift = GetPeriodicShift(boundary, pair.ProjectionDirection);
            var fiber1NodeOrigProjected = ShiftPoint(fiber1NodeOrig, periodicShift);
            var fiber2NodeOrigProjected = ShiftPoint(fiber2NodeOrig, periodicShift);

            // Build fiber elements
            var fiber1 = fibers[pair.Fiber1Id];
            var fiber2 = fibers[pair.Fiber2Id];
            var pos1 = new Point2D(fiber1.CurrentPosition[1], fiber1.CurrentPosition[2]);
            var pos2 = new Point2D(fiber2.CurrentPosition[1], fiber2.CurrentPosition[2]);

            //Try this new code to override
            var projectedFiber1Node = projNodes.First(n => n.FiberId == pair.Fiber1Id);
            var projectedFiber2Node = projNodes.First(n => n.FiberId == pair.Fiber2Id);
            var fiber1Nodes = BuildBoundaryFiberElement(projectedFiber1Node.P,fiber1NodeOrigProjected,fiber1NodeProj,
                fiber1.Radius,pair.ProjectionDirection);
            var fiber2Nodes = BuildBoundaryFiberElement(projectedFiber2Node.P, fiber2NodeOrigProjected, fiber2NodeProj,
                fiber2.Radius, pair.ProjectionDirection);

            /*var fiber1Nodes = BuildBoundaryFiberElement(ShiftPoint(pos1, periodicShift), fiber1NodeOrigProjected,
                fiber1NodeProj, fiber1.Radius, pair.ProjectionDirection);
            var fiber2Nodes = BuildBoundaryFiberElement(ShiftPoint(pos2, periodicShift), fiber2NodeOrigProjected,
                fiber2NodeProj, fiber2.Radius, pair.ProjectionDirection);
            */
            //Now do the quad points
            var fiber2NodesForQuad = (Point2D[])fiber2Nodes.Clone();

            double currentPairing =
                DistanceSquared(fiber1Nodes[2], fiber2NodesForQuad[4]) +
                DistanceSquared(fiber1Nodes[4], fiber2NodesForQuad[2]);

            double swappedPairing =
                DistanceSquared(fiber1Nodes[2], fiber2NodesForQuad[2]) +
                DistanceSquared(fiber1Nodes[4], fiber2NodesForQuad[4]);

            if (swappedPairing < currentPairing)
            {
                (fiber2NodesForQuad[2], fiber2NodesForQuad[4]) =
                    (fiber2NodesForQuad[4], fiber2NodesForQuad[2]);
            }

            var matrixResult = _elementBuilder.BuildMatrixQuad(fiber1Nodes, fiber2NodesForQuad, true);
            AddElement(ElementPhase.Matrix, matrixResult);
        }

        /// <summary>
        /// Builds a 3-node fiber element for a boundary edge (linear element with 2 end nodes + 1 midpoint).
        /// </summary>
        private Point2D[] BuildBoundaryFiberElement(Point2D projectedFiberCenter,
            Point2D nodeFromOriginalSide, Point2D nodeFromProjectedSide, double fiberRadius,
            (int ox, int oy) projectionDirection)
        {
            bool isEdgeCCW = true;

            var result = _elementBuilder.BuildFiberTriangle(
                projectedFiberCenter, nodeFromOriginalSide, nodeFromProjectedSide, fiberRadius, isEdgeCCW);

            AddElement(ElementPhase.Fiber, result);

            return result.Nodes;
        }

        /// <summary>
        /// Builds 2 fiber elements for a shared edge between two triangles.
        /// </summary>
        private void BuildFiberElementsForSharedEdge(Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes, Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers, ElementConfig config)
        {
            var fiber1 = fibers[sharedEdgeNodes[0].FiberId];
            var fiber2 = fibers[sharedEdgeNodes[1].FiberId];

            // Use the actual node positions as fiber centers - these already include projection offsets
            // For original fibers (Offset=0,0), node.P is the original position
            // For projected fibers (Offset!=0,0), node.P is the projected position
            var fiber1Center = sharedEdgeNodes[0].P;
            var fiber2Center = sharedEdgeNodes[1].P;

            // Find element nodes by matching fiber IDs and Offsets (element node index == triangle node index)
            var fiber1Node_Tri1 = FindInteriorTriangleNodeByFiberId(triangle1Nodes, triangle1ElementNodes, sharedEdgeNodes[0]);
            var fiber1Node_Tri2 = FindInteriorTriangleNodeByFiberId(triangle2Nodes, triangle2ElementNodes, sharedEdgeNodes[0]);
            var fiber2Node_Tri1 = FindInteriorTriangleNodeByFiberId(triangle1Nodes, triangle1ElementNodes, sharedEdgeNodes[1]);
            var fiber2Node_Tri2 = FindInteriorTriangleNodeByFiberId(triangle2Nodes, triangle2ElementNodes, sharedEdgeNodes[1]);
            // Determine if shared edge is in CCW order in triangle 1
            bool isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(triangle1Nodes, sharedEdgeNodes);

            // Determine fiber node order (
            var fiber1Result = _elementBuilder.BuildFiberTriangle(fiber1Center, fiber1Node_Tri1, fiber1Node_Tri2, fiber1.Radius, isEdgeCCW);
            var fiber1Nodes = fiber1Result.Nodes;

            var fiber2Result = _elementBuilder.BuildFiberTriangle(fiber2Center, fiber2Node_Tri1, fiber2Node_Tri2, fiber2.Radius, !isEdgeCCW);
            var fiber2Nodes = fiber2Result.Nodes;

            // Check for zero thickness (overlap)
            var thicknessCheck = new Point2D(
                fiber1Nodes[3].X - fiber1Nodes[2].X,
                fiber1Nodes[3].Y - fiber1Nodes[2].Y);
            double thickness = Math.Abs(thicknessCheck.X) + Math.Abs(thicknessCheck.Y);

            if (thickness < 1e-5)
                return; // Skip zero-thickness elements

            // Adjust middle nodes if fibers are too close
           (fiber1Nodes, fiber2Nodes) = ChangeMiddleNodeIfFibersAreTooClose(
              fiber1Nodes, fiber2Nodes, fiber1.Radius, fiber2.Radius);
           
            // Build and add the two fiber elements
            AddElement(ElementPhase.Fiber, new ElementBuildResult(fiber1Result.ElementName, fiber1Nodes));
            AddElement(ElementPhase.Fiber, new ElementBuildResult(fiber2Result.ElementName, fiber2Nodes));
        }

        /// <summary>
        /// Builds 1 fiber element for a shared edge with one fiber and one boundary node.
        /// </summary>
        private void BuildSingleFiberElement(Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes,
            Node[] sharedEdgeNodes, IReadOnlyList<Fiber> fibers, ElementConfig config)
        {
            // Identify which node is the fiber and which is the boundary
            Node fiberNode;
            Fiber fiber;

            if (sharedEdgeNodes[0].Type == NodeType.FiberCenter || sharedEdgeNodes[0].Type == NodeType.ProjectedFiber)
            {
                fiberNode = sharedEdgeNodes[0];
                fiber = fibers[fiberNode.FiberId];
            }
            else
            {
                fiberNode = sharedEdgeNodes[1];
                fiber = fibers[fiberNode.FiberId];
            }

            // Get fiber center position (includes projection offset if applicable)
            var fiberCenter = fiberNode.P;

            // Find element nodes by matching the fiber node in both triangles
            var fiberNode_Tri1 = FindInteriorTriangleNodeByNode(triangle1Nodes, triangle1ElementNodes, fiberNode);
            var fiberNode_Tri2 = FindInteriorTriangleNodeByNode(triangle2Nodes, triangle2ElementNodes, fiberNode);

            // Determine if shared edge is in CCW order in triangle 1
            bool isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(triangle1Nodes, sharedEdgeNodes);

            // Determine fiber node order (creates curved fiber surface with 6 nodes)
            var fiberResult = _elementBuilder.BuildFiberTriangle(fiberCenter, fiberNode_Tri1, fiberNode_Tri2, fiber.Radius, isEdgeCCW);
            var fiberNodes = fiberResult.Nodes;

            // Check for zero thickness (overlap)
            var thicknessCheck = new Point2D(
                fiberNodes[3].X - fiberNodes[2].X,
                fiberNodes[3].Y - fiberNodes[2].Y);
            double thickness = Math.Abs(thicknessCheck.X) + Math.Abs(thicknessCheck.Y);

            if (thickness < 1e-5)
                return; // Skip zero-thickness elements

            // Build and add the single fiber element
            AddElement(ElementPhase.Fiber, fiberResult);
        }

        /// <summary>
        /// Builds 1 matrix quad element for a shared edge between two triangles.
        /// </summary>
        private void BuildQuadElementForSharedEdge(Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes, Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers, ElementConfig config)
        {
            var fiber1 = fibers[sharedEdgeNodes[0].FiberId];
            var fiber2 = fibers[sharedEdgeNodes[1].FiberId];

            // Use the actual node positions as fiber centers - these already include projection offsets
            var fiber1Center = sharedEdgeNodes[0].P;
            var fiber2Center = sharedEdgeNodes[1].P;

            // Find element nodes by matching fiber IDs and Offsets
            var fiber1Node_Tri1 = FindInteriorTriangleNodeByFiberId(triangle1Nodes, triangle1ElementNodes, sharedEdgeNodes[0]);
            var fiber1Node_Tri2 = FindInteriorTriangleNodeByFiberId(triangle2Nodes, triangle2ElementNodes, sharedEdgeNodes[0]);
            var fiber2Node_Tri1 = FindInteriorTriangleNodeByFiberId(triangle1Nodes, triangle1ElementNodes, sharedEdgeNodes[1]);
            var fiber2Node_Tri2 = FindInteriorTriangleNodeByFiberId(triangle2Nodes, triangle2ElementNodes, sharedEdgeNodes[1]);

            // Determine if shared edge is in CCW order
            bool isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(triangle1Nodes, sharedEdgeNodes);

            // Determine fiber node order
            var fiber1Result = _elementBuilder.BuildFiberTriangle(fiber1Center, fiber1Node_Tri1, fiber1Node_Tri2, fiber1.Radius, isEdgeCCW);

            var fiber2Result = _elementBuilder.BuildFiberTriangle( fiber2Center, fiber2Node_Tri1, fiber2Node_Tri2, fiber2.Radius, !isEdgeCCW);

            var fiber1Nodes = fiber1Result.Nodes;
            var fiber2Nodes = fiber2Result.Nodes;

            // Check for zero thickness
            var thicknessCheck = new Point2D(
                fiber1Nodes[3].X - fiber1Nodes[2].X,
                fiber1Nodes[3].Y - fiber1Nodes[2].Y);
            double thickness = Math.Abs(thicknessCheck.X) + Math.Abs(thicknessCheck.Y);

            if (thickness < 1e-5)
                return;

            // Adjust middle nodes if fibers are too close
            (fiber1Nodes, fiber2Nodes) = ChangeMiddleNodeIfFibersAreTooClose(
                fiber1Nodes, fiber2Nodes, fiber1.Radius, fiber2.Radius);

            // Determine matrix element node order (creates 8-node quad connecting the two fibers)
            var matrixResult = _elementBuilder.BuildMatrixQuad(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            AddElement(ElementPhase.Matrix, matrixResult);
        }

        /// <summary>
        /// Builds 1 triangular matrix element for a shared edge with one fiber and one boundary node.
        /// Creates a 6-node triangular element connecting the fiber surface to the boundary point.
        /// </summary>
        private void BuildTriangularMatrixElement(Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes, Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers, ElementConfig config)
        {
            // Identify which node is the fiber and which is the boundary
            Node fiberNode, boundaryNode;
            Fiber fiber;

            if (sharedEdgeNodes[0].Type == NodeType.FiberCenter || sharedEdgeNodes[0].Type == NodeType.ProjectedFiber)
            {
                fiberNode = sharedEdgeNodes[0];
                boundaryNode = sharedEdgeNodes[1];
                fiber = fibers[fiberNode.FiberId];
            }
            else
            {
                fiberNode = sharedEdgeNodes[1];
                boundaryNode = sharedEdgeNodes[0];
                fiber = fibers[fiberNode.FiberId];
            }

            // Get positions
            var fiberCenter = fiberNode.P;
            var boundaryPoint = boundaryNode.P;

            // Find element nodes by matching the nodes in both triangles
            var fiberNode_Tri1 = FindInteriorTriangleNodeByNode(triangle1Nodes, triangle1ElementNodes, fiberNode);
            var fiberNode_Tri2 = FindInteriorTriangleNodeByNode(triangle2Nodes, triangle2ElementNodes, fiberNode);
            var boundaryNode_Tri1 = FindInteriorTriangleNodeByNode(triangle1Nodes, triangle1ElementNodes, boundaryNode);
            var boundaryNode_Tri2 = FindInteriorTriangleNodeByNode(triangle2Nodes, triangle2ElementNodes, boundaryNode);

            // Verify the boundary nodes from both triangles are the same point
            if (Math.Abs(boundaryNode_Tri1.X - boundaryNode_Tri2.X) > NodeTolerance ||
                Math.Abs(boundaryNode_Tri1.Y - boundaryNode_Tri2.Y) > NodeTolerance)
            {
                throw new InvalidOperationException("Boundary nodes from adjacent triangles don't match");
            }

            // Determine if shared edge is in CCW order in triangle 1
            bool isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(triangle1Nodes, sharedEdgeNodes);

            // Build and add the triangular matrix element
            var fiberResult = _elementBuilder.BuildFiberTriangle(
                fiberCenter, fiberNode_Tri1, fiberNode_Tri2, fiber.Radius, isEdgeCCW);

            var fiberNodes = fiberResult.Nodes;

            var matrixResult = _elementBuilder.BuildFiberBoundaryMatrixTriangle(
                fiberNodes, boundaryNode_Tri1, isEdgeCCW);

            AddElement(ElementPhase.Matrix, matrixResult);
        }

        private (List<int> topEdge, List<int> rightEdge) BuildBoundaryEdgeNodes(CellBoundary boundary)
        {
            var topEdge = new List<int>();
            var rightEdge = new List<int>();

            double maxY = boundary.ODimensions[2];
            double maxX = boundary.ODimensions[1];

            for (int i = 0; i < _globalNodes.Count; i++)
            {
                var node = _globalNodes[i];
                if (Math.Abs(node.Y - maxY) < NodeTolerance)
                    topEdge.Add(i);
                if (Math.Abs(node.X - maxX) < NodeTolerance)
                    rightEdge.Add(i);
            }

            return (topEdge, rightEdge);
        }

        private void BuildMatrixInteriorTriangle(Node nodeA, Node nodeB, Node nodeC,IReadOnlyList<Fiber> fibers, 
            ElementConfig config, DebugOptions? dOptions = null)
        {
            // Calculate surface points on fibers for interior triangle
            var nodes = new Point2D[3];
            var triangleNodes = new[] { nodeA, nodeB, nodeC };

            // Check for potential overlaps
            var overlapInfo = DetectFiberOverlaps(triangleNodes, fibers);

            // Check for critical overlaps and write error file if found
            bool hasCriticalOverlap = false;
            for (int i = 0; i < 3; i++)
            {
                if (overlapInfo[i].HasCriticalOverlap)
                {
                    hasCriticalOverlap = true;
                    break;
                }
            }

            if (hasCriticalOverlap)
            {
                WriteCriticalOverlapErrorFile(triangleNodes, fibers, overlapInfo, dOptions);
                // Skip processing this triangle since it has critical overlap
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                var currentNode = triangleNodes[i];
                var otherIndices = GetOtherIndices(i);
                var otherNode1 = triangleNodes[otherIndices[0]];
                var otherNode2 = triangleNodes[otherIndices[1]];

                // Calculate surface point for fiber centers, use point as-is for boundary nodes
                if (currentNode.Type == NodeType.FiberCenter || currentNode.Type == NodeType.ProjectedFiber)
                {
                    var fiber = fibers[currentNode.FiberId];
                    // Use the actual node position (which accounts for projection offsets)
                    Point2D fiberCenter = currentNode.P;

                    nodes[i] = CalculateFiberSurfacePoint(
                        fiberCenter,
                        fiber.Radius,
                        otherNode1.P,
                        otherNode2.P,
                        overlapInfo[i].NeedsAdjustment,
                        i,
                        overlapInfo);
                }
                else
                {
                    // Boundary node - use as is
                    nodes[i] = currentNode.P;
                }
            }

            if (SignedArea2(nodes[0], nodes[1], nodes[2]) < 0.0)
            {
                (nodes[1], nodes[2]) = (nodes[2], nodes[1]);
            }

            var result = _elementBuilder.BuildInteriorMatrixTriangle(nodes[0], nodes[1], nodes[2]);
            AddElement(ElementPhase.Matrix, result);
        }

        #endregion

        #region Math Helpers

        private static double SignedArea2(Point2D a, Point2D b, Point2D c)
        {
            return (b.X - a.X) * (c.Y - a.Y)
                 - (b.Y - a.Y) * (c.X - a.X);
        }
        private static double DistanceSquared(Point2D a, Point2D b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Calculates the minimum distance from a point to a line segment.
        /// </summary>
        private double CalculatePointToLineDistance(Point2D point, Point2D lineStart, Point2D lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            if (Math.Abs(dx) < 1e-10 && Math.Abs(dy) < 1e-10)
            {
                // Line is actually a point
                return MathHelper.CalcDistanceBetweenTwoPoints(point, lineStart);
            }

            // Calculate distance using cross product formula
            double numerator = Math.Abs(dy * point.X - dx * point.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X);
            double denominator = Math.Sqrt(dx * dx + dy * dy);

            return numerator / denominator;
        }

        /// <summary>
        /// Calculates a point on the surface of a fiber that faces the interior of the triangle.
        /// Uses vector addition to find the bisector - no angle calculations needed.
        /// Handles overlap cases by using edge directions instead of bisector.
        /// </summary>
        private Point2D CalculateFiberSurfacePoint(Point2D fiberCenter, double fiberRadius, Point2D otherPoint1,
            Point2D otherPoint2, bool needsAdjustment, int currentIndex, FiberOverlapInfo[] allOverlaps)
        {
            // Create vectors from fiber center to the other two points
            var vec1 = MathHelper.MakeVector2D(fiberCenter, otherPoint1);
            var vec2 = MathHelper.MakeVector2D(fiberCenter, otherPoint2);

            // Normalize both vectors
            var vec1Normalized = Normalize(vec1);
            var vec2Normalized = Normalize(vec2);

            Point2D direction;

            // MATLAB logic: If ANY fiber in the triangle needs adjustment (narrow gap),
            // we adjust the OTHER fibers (not the one with the narrow gap).
            // This is the key insight from line 74 of Triad.m:
            //   shiftCenterDueToOverlap = ~obj.willInteriorElementHaveOverlapWithFiber(i)
            // The negation means: "shift if the CURRENT fiber is NOT the overlapping one"

            // First, check if any fiber needs adjustment
            int overlapIndex = -1;
            bool anyFiberNeedsAdjustment = false;
            for (int i = 0; i < allOverlaps.Length; i++)
            {
                if (allOverlaps[i].NeedsAdjustment)
                {
                    anyFiberNeedsAdjustment = true;
                    overlapIndex = i;
                    break;
                }
            }

            // Now, if any fiber needs adjustment AND the current fiber is NOT that fiber,
            // then we should adjust the current fiber's surface point
            bool shouldAdjustCurrentFiber = anyFiberNeedsAdjustment && !needsAdjustment;

            if (shouldAdjustCurrentFiber)
            {
                // Adjust this fiber's surface point away from the overlapping fiber
                // This matches MATLAB's AdjustMidPointDueToOverlap
                direction = GetEdgeDirectionForOverlap(vec1Normalized, vec2Normalized, currentIndex, overlapIndex);
            }
            else
            {
                // Normal case: use bisector
                var bisectorDirection = new Point2D(
                    vec1Normalized.X + vec2Normalized.X,
                    vec1Normalized.Y + vec2Normalized.Y);
                direction = Normalize(bisectorDirection);
            }

            // Calculate point on fiber surface
            return new Point2D(
                fiberCenter.X + fiberRadius * direction.X,
                fiberCenter.Y + fiberRadius * direction.Y);
        }

        private Point2D GetPeriodicShift(CellBoundary boundary, (int ox, int oy) direction)
        {
            double lengthX = boundary.ODimensions[1];
            double lengthY = boundary.ODimensions[2];

            return new Point2D(direction.ox * lengthX, direction.oy * lengthY);
        }

        private Point2D ShiftPoint(Point2D p, Point2D shift)
        {
            return new Point2D(p.X + shift.X, p.Y + shift.Y);
        }

        /// <summary>
        /// Determines which edge direction to use when overlap is detected.
        /// Based on MATLAB's AdjustMidPointDueToOverlap logic.
        /// 
        /// IMPORTANT: MATLAB has special handling for the "middle edge" (fiber index 1 in 0-based).
        /// For fiber 1, T_Unit is based on VAC instead of VAB, which effectively swaps vec1 and vec2.
        /// </summary>
        private Point2D GetEdgeDirectionForOverlap(Point2D vec1, Point2D vec2, int currentIdx, int overlapIdx)
        {
            // MATLAB uses isMiddleEdge flag: if(i == 2) in MATLAB 1-based means (i == 1) in 0-based
            // For the middle fiber, MATLAB swaps which vector is considered "first"
            bool isMiddleEdge = (currentIdx == 1);

            // When overlap is detected, use the vector pointing to one of the edges
            // rather than the bisector, to avoid the interior triangle overlapping the fiber
            //
            // This matches MATLAB's AdjustMidPointDueToOverlap logic:
            // - vec1 corresponds to T_Unit (first edge angle)
            // - vec2 corresponds to T_Unit + TAB_AC (second edge angle)
            // - BUT for middle edge (currentIdx == 1), these are swapped in MATLAB
            // - overlapIdx is 0-based (0, 1, 2) corresponding to MATLAB's 1-based (1, 2, 3)
            // - currentIdx is 0-based (0, 1, 2) corresponding to MATLAB's idx (1, 2, 3)

            // If this is the middle edge, swap vec1 and vec2 to match MATLAB's isMiddleEdge behavior
            if (isMiddleEdge)
            {
                var temp = vec1;
                vec1 = vec2;
                vec2 = temp;
            }

            switch (overlapIdx)
            {
                case 0:
                    // Fiber 0 overlaps (MATLAB case 1)
                    if (currentIdx == 1)
                        return vec1;  // MATLAB: idx==2 → T_Unit (but swapped for middle edge)
                    else if (currentIdx == 2)
                        return vec2;  // MATLAB: idx==3 → T_Unit + TAB_AC
                    break;
                case 1:
                    // Fiber 1 overlaps (MATLAB case 2)
                    if (currentIdx == 0)
                        return vec2;  // MATLAB: idx==1 → T_Unit + TAB_AC
                    else if (currentIdx == 2)
                        return vec1;  // MATLAB: idx==3 → T_Unit
                    break;
                case 2:
                    // Fiber 2 overlaps (MATLAB case 3)
                    if (currentIdx == 0)
                        return vec1;  // MATLAB: idx==1 → T_Unit
                    else if (currentIdx == 1)
                        return vec2;  // MATLAB: idx==2 → T_Unit + TAB_AC (but swapped for middle edge)
                    break;
            }

            // Default: use bisector (shouldn't reach here in normal operation)
            var bisector = new Point2D(vec1.X + vec2.X, vec1.Y + vec2.Y);
            return Normalize(bisector);
        }

        /// <summary>
        /// Normalizes a 2D vector to unit length.
        /// </summary>
        private Point2D Normalize(Point2D vector)
        {
            double length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            if (length < 1e-10)  // Avoid division by zero
            {
                return new Point2D(1, 0);  // Default direction
            }
            return new Point2D(vector.X / length, vector.Y / length);
        }

        #endregion

        #region Data Classes


        private sealed class PeriodicBoundaryData
        {
            public List<(int Node1, int Node2)> Pairs { get; } = new();
            public List<int> X1Nodes { get; } = new();
            public List<int> Y1Nodes { get; } = new();
            public int? PinnedNode { get; set; }
        }

        private class PeriodicFiberBoundaryPair
        {
            public int FiberId { get; set; }
            public int OriginalTriangleIndex { get; set; }
            public int ProjectedTriangleIndex { get; set; }
            public (int ox, int oy) ProjectionDirection { get; set; }
            public Point2D BoundaryPoint { get; set; }
        }

        /// <summary>
        /// Data structure for a periodic fiber pair on a boundary edge.
        /// </summary>
        private class PeriodicFiberPair
        {
            public int Fiber1Id { get; set; }
            public int Fiber2Id { get; set; }
            public int OriginalTriangleIndex { get; set; }
            public int ProjectedTriangleIndex { get; set; }
            public (int ox, int oy) ProjectionDirection { get; set; }
        }
        
        /// <summary>
        /// Encapsulates information about a shared edge between two triangles.
        /// </summary>
        private sealed class EdgeData
        {
            public Node[] Triangle1Nodes { get; }
            public Node[] Triangle2Nodes { get; }
            public Point2D[] Triangle1ElementNodes { get; }
            public Point2D[] Triangle2ElementNodes { get; }
            public Node[] SharedEdgeNodes { get; }
            public EdgeType Type { get; }

            public EdgeData(Node[] tri1Nodes, Node[] tri2Nodes, Point2D[] tri1ElemNodes,
                Point2D[] tri2ElemNodes, Node[] sharedEdgeNodes, EdgeType edgeType)
            {
                Triangle1Nodes = tri1Nodes;
                Triangle2Nodes = tri2Nodes;
                Triangle1ElementNodes = tri1ElemNodes;
                Triangle2ElementNodes = tri2ElemNodes;
                SharedEdgeNodes = sharedEdgeNodes;
                Type = edgeType;
            }
        }

        /// <summary>
        /// Classification of edge types based on node composition.
        /// </summary>
        private enum EdgeType
        {
            /// <summary>Both edge nodes are fiber centers or projected fibers</summary>
            TwoFibers,
            /// <summary>One edge node is a fiber, the other is a boundary point</summary>
            OneFiberOneBoundary,
            /// <summary>Both edge nodes are boundary points</summary>
            TwoBoundaries
        }

        #endregion
    }
}