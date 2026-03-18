using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.Meshing.Elements;
using FDEMCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FxTMeshGenerator.Meshing
{
    /// <summary>
    /// Builds finite elements from a Delaunay triangulation.
    /// </summary>
    public sealed class ElementBuilder
    {
        private readonly List<Point2D> _globalNodes = new();
        private readonly Dictionary<string, int> _nodeToIndex = new();
        private readonly List<BaseElement> _elements = new();
        private int _elementIdCounter = 0;
        private const double NodeTolerance = 1e-10;

        public FEMesh BuildMesh(
            TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers,
            CellBoundary boundary,
            ElementConfig config,
            string debugOutputPath = null)
        {
            // Reset state
            _globalNodes.Clear();
            _nodeToIndex.Clear();
            _elements.Clear();
            _elementIdCounter = 0;

            // Process each triangle to build interior matrix elements
            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                var tri = triangulation.Triangles[i];
                var nodeA = triangulation.Nodes[tri[0]];
                var nodeB = triangulation.Nodes[tri[1]];
                var nodeC = triangulation.Nodes[tri[2]];

                ProcessTriangle(nodeA, nodeB, nodeC, fibers, config);
            }

            // Write intermediate mesh: just interior triangles
            if (debugOutputPath != null)
            {
                var interiorTriMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(), 
                    new List<(int, int)>(), new List<int>(), new List<int>());
                IO.VtkLegacyWriter.WriteUnstructuredMesh(
                    debugOutputPath.Replace(".vtk", "_mesh_tri.vtk"), interiorTriMesh);
            }

            // Build fiber and matrix elements between adjacent triangles
            BuildInteriorFiberMatrixElements(triangulation, fibers, config, debugOutputPath);

            // Build periodic node pairs
            var periodicPairs = BuildPeriodicNodePairs(triangulation, boundary);
            var (topEdge, rightEdge) = BuildBoundaryEdgeNodes(boundary);

            return new FEMesh(_globalNodes, _elements, periodicPairs, topEdge, rightEdge);
        }

        private void ProcessTriangle(
            Node nodeA, Node nodeB, Node nodeC,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            // Classify triangle based on node types
            var nodes = new[] { nodeA, nodeB, nodeC };
            int fiberCount = nodes.Count(n => n.Type == NodeType.FiberCenter || n.Type == NodeType.ProjectedFiber);
            int boundaryCount = nodes.Count(n => n.Type == NodeType.BoundaryPoint || n.Type == NodeType.BoundaryCorner);

            if (fiberCount == 3)
            {
                // Interior triangle: 3 fiber centers
                BuildInteriorTriangle(nodeA, nodeB, nodeC, fibers, config);
            }
            else if (boundaryCount == 3)
            {
                // All boundary: simple matrix triangle
                BuildBoundaryOnlyTriangle(nodeA, nodeB, nodeC, config);
            }
        }

        private void BuildInteriorTriangle(
            Node nodeA, Node nodeB, Node nodeC,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            // Calculate surface points on fibers for interior triangle
            var nodes = new Point2D[3];
            var triangleNodes = new[] { nodeA, nodeB, nodeC };

            // Check for potential overlaps
            var overlapInfo = DetectFiberOverlaps(triangleNodes, fibers);

            for (int i = 0; i < 3; i++)
            {
                var currentNode = triangleNodes[i];
                var otherIndices = GetOtherIndices(i);
                var otherNode1 = triangleNodes[otherIndices[0]];
                var otherNode2 = triangleNodes[otherIndices[1]];

                // Calculate surface point for fiber centers, use point as-is for boundary nodes
                if (currentNode.Type == NodeType.FiberCenter || currentNode.Type == NodeType.ProjectedFiber)
                {
                    var fiber = fibers[currentNode.FiberId.Value];
                    // Use the actual node position (which accounts for projection offsets)
                    Point2D fiberCenter = currentNode.P;

                    nodes[i] = CalculateFiberSurfacePoint(
                        fiberCenter,
                        fiber.Radius,
                        otherNode1.P,
                        otherNode2.P,
                        overlapInfo[i],
                        i,
                        overlapInfo);
                }
                else
                {
                    // Boundary node - use as is
                    nodes[i] = currentNode.P;
                }
            }

            AddTriangleElement(nodes, ElementPhase.Matrix);
        }

        private void BuildBoundaryOnlyTriangle(Node nodeA, Node nodeB, Node nodeC, ElementConfig config)
        {
            // Simple matrix triangle with boundary points
            var nodes = new[] { nodeA.P, nodeB.P, nodeC.P };
            AddTriangleElement(nodes, ElementPhase.Matrix);
        }

        private void AddTriangleElement(Point2D[] nodes, ElementPhase phase)
        {
            var element = new TriangleElement(_elementIdCounter++, phase, nodes);
            _elements.Add(element);

            // Add nodes to global list
            foreach (var node in nodes)
            {
                AddOrGetGlobalNode(node);
            }
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

        private List<(int, int)> BuildPeriodicNodePairs(TriangulationMesh2D triangulation, CellBoundary boundary)
        {
            var pairs = new List<(int, int)>();
            // Simplified - full implementation would use offset information from nodes
            return pairs;
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

        /// <summary>
        /// Detects if any fibers in the triangle are too close to the opposite edge,
        /// which would cause the interior triangle element to overlap with a fiber.
        /// Based on MATLAB Triad.DetermineIfFibersOverlapTriad.
        /// </summary>
        private bool[] DetectFiberOverlaps(Node[] triangleNodes, IReadOnlyList<Fiber> fibers)
        {
            bool[] hasOverlap = new bool[3];

            for (int i = 0; i < 3; i++)
            {
                var currentNode = triangleNodes[i];
                if (currentNode.Type != NodeType.FiberCenter && currentNode.Type != NodeType.ProjectedFiber)
                    continue;

                var fiber = fibers[currentNode.FiberId.Value];
                var otherIndices = GetOtherIndices(i);
                var otherNode1 = triangleNodes[otherIndices[0]];
                var otherNode2 = triangleNodes[otherIndices[1]];

                // Calculate minimum distance from fiber to the opposite edge
                double minDist = CalculatePointToLineDistance(
                    currentNode.P,
                    otherNode1.P,
                    otherNode2.P);

                // Check if too close (factor of 2 from MATLAB code)
                double threshold = fiber.Radius + fiber.Radius / 2.0;
                hasOverlap[i] = minDist <= threshold;
            }

            return hasOverlap;
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
        private Point2D CalculateFiberSurfacePoint(
            Point2D fiberCenter,
            double fiberRadius,
            Point2D otherPoint1,
            Point2D otherPoint2,
            bool hasOverlap,
            int currentIndex,
            bool[] allOverlaps)
        {
            // Create vectors from fiber center to the other two points
            var vec1 = MathHelper.MakeVector2D(fiberCenter, otherPoint1);
            var vec2 = MathHelper.MakeVector2D(fiberCenter, otherPoint2);

            // Normalize both vectors
            var vec1Normalized = Normalize(vec1);
            var vec2Normalized = Normalize(vec2);

            Point2D direction;

            if (hasOverlap)
            {
                // If overlap detected, use edge direction instead of bisector
                // This matches MATLAB's AdjustMidPointDueToOverlap
                int overlapIndex = Array.IndexOf(allOverlaps, true);
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

        /// <summary>
        /// Determines which edge direction to use when overlap is detected.
        /// Based on MATLAB's AdjustMidPointDueToOverlap logic.
        /// </summary>
        private Point2D GetEdgeDirectionForOverlap(Point2D vec1, Point2D vec2, int currentIdx, int overlapIdx)
        {
            // When overlap is detected, use the vector pointing to one of the edges
            // rather than the bisector, to avoid the interior triangle overlapping the fiber

            // Logic based on MATLAB switch statement
            switch (overlapIdx)
            {
                case 0:
                    // Fiber 0 has overlap
                    return currentIdx == 1 ? vec2 : vec1;
                case 1:
                    // Fiber 1 has overlap
                    return currentIdx == 0 ? vec2 : vec1;
                case 2:
                    // Fiber 2 has overlap
                    return currentIdx == 0 ? vec1 : vec2;
                default:
                    // Default: use bisector (shouldn't reach here)
                    var bisector = new Point2D(vec1.X + vec2.X, vec1.Y + vec2.Y);
                    return Normalize(bisector);
            }
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
        /// Builds fiber and matrix elements between adjacent triangles.
        /// Based on MATLAB FE_Mesh.BuildInteriorFiberMatrixElements (lines 253-300).
        /// </summary>
        private void BuildInteriorFiberMatrixElements(
            TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config,
            string debugOutputPath = null)
        {
            // Store which triangle elements we've already built (mapping from triangle index to built interior triangle nodes)
            var triangleElements = new Dictionary<int, Point2D[]>();

            // Store edge data for building elements
            var edgeData = new List<(Node[] nodes1, Node[] nodes2, Point2D[] tri1Elem, Point2D[] tri2Elem, Node[] sharedEdgeNodes)>();
            // Extract interior triangle element nodes for lookup
            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                var tri = triangulation.Triangles[i];
                var nodeA = triangulation.Nodes[tri[0]];
                var nodeB = triangulation.Nodes[tri[1]];
                var nodeC = triangulation.Nodes[tri[2]];

                // Only process interior triangles (3 fiber centers)
                var nodes = new[] { nodeA, nodeB, nodeC };
                int fiberCount = nodes.Count(n => n.Type == NodeType.FiberCenter || n.Type == NodeType.ProjectedFiber);

                if (fiberCount == 3)
                {
                    // Reconstruct the interior triangle element nodes the same way we built them
                    var elementNodes = new Point2D[3];
                    var overlapInfo = DetectFiberOverlaps(nodes, fibers);

                    for (int j = 0; j < 3; j++)
                    {
                        var currentNode = nodes[j];
                        var otherIndices = GetOtherIndices(j);
                        var otherNode1 = nodes[otherIndices[0]];
                        var otherNode2 = nodes[otherIndices[1]];

                        var fiber = fibers[currentNode.FiberId.Value];
                        Point2D fiberCenter = currentNode.P;

                        elementNodes[j] = CalculateFiberSurfacePoint(
                            fiberCenter,
                            fiber.Radius,
                            otherNode1.P,
                            otherNode2.P,
                            overlapInfo[j],
                            j,
                            overlapInfo);
                    }

                    triangleElements[i] = elementNodes;
                }
            }

            // Find adjacent triangle pairs and build fiber/matrix elements
            // Based on MATLAB CreateArrayOfAllTriadPairs and BuildInteriorFiberMatrixElements
            var processedEdges = new HashSet<string>();

            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                if (!triangleElements.ContainsKey(i))
                    continue; // Skip non-interior triangles

                var tri1 = triangulation.Triangles[i];
                var nodes1 = new[] { 
                    triangulation.Nodes[tri1[0]], 
                    triangulation.Nodes[tri1[1]], 
                    triangulation.Nodes[tri1[2]] 
                };

                // Check only interior triangles
                if (nodes1.Any(n => n.Type != NodeType.FiberCenter && n.Type != NodeType.ProjectedFiber))
                    continue;

                // Find all edges of this triangle (store both FiberId and Offset)
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

                        if (nodes2.Any(n => n.Type != NodeType.FiberCenter && n.Type != NodeType.ProjectedFiber))
                            continue;

                        // Check if this triangle shares the edge
                        // Must match both FiberId AND Offset to avoid connecting original to projected
                        bool sharesEdge = SharesEdge(nodes2, edge.Item1, edge.Item2);

                        if (sharesEdge)
                        {
                            // Mark edge as processed
                            processedEdges.Add(edgeKey);

                            // Store edge data for later processing - preserve full Node information
                            edgeData.Add((nodes1, nodes2, triangleElements[i], triangleElements[j], new[] { edge.Item1, edge.Item2 }));

                            break; // Only one adjacent triangle per edge
                        }
                    }
                }
            }

            // Now build fiber elements first
            foreach (var (nodes1, nodes2, tri1Elem, tri2Elem, sharedEdgeNodes) in edgeData)
            {
                BuildFiberElementsForSharedEdge(nodes1, nodes2, tri1Elem, tri2Elem, sharedEdgeNodes, fibers, config);
            }

            // Write intermediate mesh: triangles + fiber elements (before quads)
            if (debugOutputPath != null)
            {
                var triPlusFibMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(), 
                    new List<(int, int)>(), new List<int>(), new List<int>());
                IO.VtkLegacyWriter.WriteUnstructuredMesh(
                    debugOutputPath.Replace(".vtk", "_mesh_triPlusFib.vtk"), triPlusFibMesh);
            }

            // Now build quad elements
            foreach (var (nodes1, nodes2, tri1Elem, tri2Elem, sharedEdgeNodes) in edgeData)
            {
                BuildQuadElementForSharedEdge(nodes1, nodes2, tri1Elem, tri2Elem, sharedEdgeNodes, fibers, config);
            }

            // Write final mesh: triangles + fiber + quads
            if (debugOutputPath != null)
            {
                var fullMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(), 
                    new List<(int, int)>(), new List<int>(), new List<int>());
                IO.VtkLegacyWriter.WriteUnstructuredMesh(
                    debugOutputPath.Replace(".vtk", "_mesh_triPlusFibPlusQuad.vtk"), fullMesh);
            }
        }

        /// <summary>
        /// Builds 2 fiber elements for a shared edge between two triangles.
        /// </summary>
        private void BuildFiberElementsForSharedEdge(
            Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes,
            Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            var fiber1 = fibers[sharedEdgeNodes[0].FiberId.Value];
            var fiber2 = fibers[sharedEdgeNodes[1].FiberId.Value];

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

            // Determine fiber node order (creates curved fiber surface with 6 nodes)
            var fiber1Nodes = DetermineFiberNodeOrder(fiber1Center, fiber1Node_Tri1, fiber1Node_Tri2, fiber1.Radius, isEdgeCCW);
            var fiber2Nodes = DetermineFiberNodeOrder(fiber2Center, fiber2Node_Tri1, fiber2Node_Tri2, fiber2.Radius, !isEdgeCCW);

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
            AddFiberElement(fiber1Nodes, ElementPhase.Fiber);
            AddFiberElement(fiber2Nodes, ElementPhase.Fiber);
        }

        /// <summary>
        /// Builds 1 matrix quad element for a shared edge between two triangles.
        /// </summary>
        private void BuildQuadElementForSharedEdge(
            Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes,
            Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            var fiber1 = fibers[sharedEdgeNodes[0].FiberId.Value];
            var fiber2 = fibers[sharedEdgeNodes[1].FiberId.Value];

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
            var fiber1Nodes = DetermineFiberNodeOrder(fiber1Center, fiber1Node_Tri1, fiber1Node_Tri2, fiber1.Radius, isEdgeCCW);
            var fiber2Nodes = DetermineFiberNodeOrder(fiber2Center, fiber2Node_Tri1, fiber2Node_Tri2, fiber2.Radius, !isEdgeCCW);

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
            var matrixNodes = DetermineInteriorMatrixNodeOrder(fiber1Nodes, fiber2Nodes, isEdgeCCW);

            // Build and add the quad element
            AddQuadElement(matrixNodes, ElementPhase.Matrix);
        }

        /// <summary>
        /// Builds 2 fiber elements and 1 matrix element for a shared edge between two triangles.
        /// DEPRECATED: Split into BuildFiberElementsForSharedEdge and BuildQuadElementForSharedEdge for debug output.
        /// </summary>
        private void BuildFiberMatrixElementsForSharedEdge(
            Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes,
            Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            // Call both methods
            BuildFiberElementsForSharedEdge(triangle1Nodes, triangle2Nodes, triangle1ElementNodes, triangle2ElementNodes, sharedEdgeNodes, fibers, config);
            BuildQuadElementForSharedEdge(triangle1Nodes, triangle2Nodes, triangle1ElementNodes, triangle2ElementNodes, sharedEdgeNodes, fibers, config);
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
        /// Determines the node ordering for a curved fiber element (6 nodes).
        /// Based on MATLAB FE_Mesh_2D.DetermineFiberNodeOrder (lines 286-305).
        /// </summary>
        private Point2D[] DetermineFiberNodeOrder(
            Point2D fiberCenter, Point2D node1, Point2D node2, double fiberRadius, bool isEdgeCCW)
        {
            var nodes = new Point2D[6];
            nodes[0] = fiberCenter;

            if (isEdgeCCW)
            {
                nodes[2] = node2;
                nodes[4] = node1;
            }
            else
            {
                nodes[2] = node1;
                nodes[4] = node2;
            }

            // Midpoint nodes
            nodes[1] = new Point2D((nodes[0].X + nodes[2].X) / 2.0, (nodes[0].Y + nodes[2].Y) / 2.0);
            nodes[5] = new Point2D((nodes[0].X + nodes[4].X) / 2.0, (nodes[0].Y + nodes[4].Y) / 2.0);

            // Middle node on fiber surface
            Point2D midPointBetweenNodes2And4 = new Point2D(
                (nodes[2].X + nodes[4].X) / 2.0,
                (nodes[2].Y + nodes[4].Y) / 2.0);

            var midPointVector = MathHelper.MakeVector2D(fiberCenter, midPointBetweenNodes2And4);
            double midPointAngle = Math.Atan2(midPointVector.Y, midPointVector.X);

            nodes[3] = new Point2D(
                fiberCenter.X + fiberRadius * Math.Cos(midPointAngle),
                fiberCenter.Y + fiberRadius * Math.Sin(midPointAngle));

            return nodes;
        }

        /// <summary>
        /// Determines the node ordering for an interior matrix quad element (8 nodes).
        /// Based on MATLAB FE_Mesh_2D.DetermineInteriorMatrixNodeOrder (lines 308-328).
        /// </summary>
        private Point2D[] DetermineInteriorMatrixNodeOrder(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = new Point2D[8];

            if (isEdgeCCW)
            {
                nodes[0] = fiber1Nodes[4];
                nodes[1] = fiber1Nodes[3];
                nodes[2] = fiber1Nodes[2];
                nodes[4] = fiber2Nodes[4];
                nodes[5] = fiber2Nodes[3];
                nodes[6] = fiber2Nodes[2];
            }
            else
            {
                nodes[0] = fiber2Nodes[4];
                nodes[1] = fiber2Nodes[3];
                nodes[2] = fiber2Nodes[2];
                nodes[4] = fiber1Nodes[4];
                nodes[5] = fiber1Nodes[3];
                nodes[6] = fiber1Nodes[2];
            }

            // Midpoint nodes
            nodes[3] = new Point2D((nodes[2].X + nodes[4].X) / 2.0, (nodes[2].Y + nodes[4].Y) / 2.0);
            nodes[7] = new Point2D((nodes[0].X + nodes[6].X) / 2.0, (nodes[0].Y + nodes[6].Y) / 2.0);

            return nodes;
        }

        /// <summary>
        /// Checks if a shared edge is in counter-clockwise order within a triangle.
        /// Based on MATLAB Triad.CheckIfSharedEdgeIsCCWOrder (lines 119-130).
        /// </summary>
        private bool CheckIfSharedEdgeIsCCWOrder(Node[] triangleNodes, Node[] sharedEdgeNodes)
        {
            var fiberIds = new[] { 
                triangleNodes[0].FiberId.Value, 
                triangleNodes[1].FiberId.Value, 
                triangleNodes[2].FiberId.Value 
            };

            int idx1 = Array.IndexOf(fiberIds, sharedEdgeNodes[0].FiberId.Value);
            int idx2 = Array.IndexOf(fiberIds, sharedEdgeNodes[1].FiberId.Value);

            if (idx1 == 0 && idx2 == 2)
                return false;
            else if (idx1 < idx2 || (idx1 == fiberIds.Length - 1 && idx2 == 0))
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

        private void AddFiberElement(Point2D[] nodes, ElementPhase phase)
        {
            var element = new TriangleElement(_elementIdCounter++, phase, nodes);
            _elements.Add(element);

            foreach (var node in nodes)
            {
                AddOrGetGlobalNode(node);
            }
        }

        private void AddQuadElement(Point2D[] nodes, ElementPhase phase)
        {
            var element = new QuadElement(_elementIdCounter++, phase, nodes);
            _elements.Add(element);

            foreach (var node in nodes)
            {
                AddOrGetGlobalNode(node);
            }
        }

        /// <summary>
        /// Checks if a triangle shares an edge with two given nodes.
        /// Matches both FiberId AND Offset to prevent connecting original fibers to projected ones.
        /// </summary>
        private bool SharesEdge(Node[] triangleNodes, Node edgeNode1, Node edgeNode2)
        {
            int matchCount = 0;

            foreach (var node in triangleNodes)
            {
                // Check if node matches edgeNode1
                if (node.FiberId == edgeNode1.FiberId && 
                    node.Offset == edgeNode1.Offset)
                {
                    matchCount++;
                }
                // Check if node matches edgeNode2
                else if (node.FiberId == edgeNode2.FiberId && 
                         node.Offset == edgeNode2.Offset)
                {
                    matchCount++;
                }
            }

            return matchCount == 2;
        }

        /// <summary>
        /// Creates a unique key for an edge including offset information.
        /// This prevents matching original fibers with their projections.
        /// </summary>
        private string GetEdgeKey(Node node1, Node node2)
        {
            // Create a unique key that includes both FiberId and Offset
            string key1 = $"{node1.FiberId.Value}_{node1.Offset.ox}_{node1.Offset.oy}";
            string key2 = $"{node2.FiberId.Value}_{node2.Offset.ox}_{node2.Offset.oy}";

            // Order-independent key
            if (string.CompareOrdinal(key1, key2) < 0)
                return $"{key1}|{key2}";
            else
                return $"{key2}|{key1}";
        }
    }
}