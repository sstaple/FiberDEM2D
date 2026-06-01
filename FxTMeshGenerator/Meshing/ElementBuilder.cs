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

            public EdgeData(
                Node[] tri1Nodes,
                Node[] tri2Nodes,
                Point2D[] tri1ElemNodes,
                Point2D[] tri2ElemNodes,
                Node[] sharedEdgeNodes,
                EdgeType edgeType)
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

        public FEMesh BuildMesh(TriangulationMesh2D triangulation,IReadOnlyList<Fiber> fibers,
            CellBoundary boundary,ElementConfig config, DebugOptions? dOptions = null)
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
            if (dOptions != null && dOptions.Debug)
            {
                var interiorTriMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>());
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("triMesh"), interiorTriMesh);
            }
            
            // Build fiber and matrix elements between adjacent triangles
            BuildInteriorFiberMatrixElements(triangulation, fibers, config, dOptions);

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
            // All triangles now go through BuildInteriorTriangle
            // It handles both fiber nodes (surface points) and boundary nodes (original points)
            BuildInteriorTriangle(nodeA, nodeB, nodeC, fibers, config);
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
            //
            // This matches MATLAB's AdjustMidPointDueToOverlap logic:
            // - vec1 corresponds to T_Unit (first edge angle)
            // - vec2 corresponds to T_Unit + TAB_AC (second edge angle)
            // - overlapIdx is 0-based (0, 1, 2) corresponding to MATLAB's 1-based (1, 2, 3)
            // - currentIdx is 0-based (0, 1, 2) corresponding to MATLAB's idx (1, 2, 3)

            switch (overlapIdx)
            {
                case 0:
                    // Fiber 0 overlaps (MATLAB case 1)
                    if (currentIdx == 1)
                        return vec1;  // MATLAB: idx==2 → T_Unit
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
                        return vec2;  // MATLAB: idx==2 → T_Unit + TAB_AC
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
        /// Finds all shared edges between adjacent triangles and reconstructs their element nodes.
        /// Returns edge data classified by edge type (TwoFibers, OneFiberOneBoundary, TwoBoundaries).
        /// </summary>
        private List<EdgeData> FindSharedEdgesForFiberElements(
            TriangulationMesh2D triangulation,
            IReadOnlyList<Fiber> fibers)
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

                for (int j = 0; j < 3; j++)
                {
                    var currentNode = nodes[j];
                    var otherIndices = GetOtherIndices(j);
                    var otherNode1 = nodes[otherIndices[0]];
                    var otherNode2 = nodes[otherIndices[1]];

                    // Calculate surface point for fiber centers, use point as-is for boundary nodes
                    if (currentNode.Type == NodeType.FiberCenter || currentNode.Type == NodeType.ProjectedFiber)
                    {
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
        /// Builds fiber and matrix elements between adjacent triangles.
        /// Based on MATLAB FE_Mesh.BuildInteriorFiberMatrixElements (lines 253-300).
        /// </summary>
        private void BuildInteriorFiberMatrixElements(TriangulationMesh2D triangulation,IReadOnlyList<Fiber> fibers,
            ElementConfig config, DebugOptions? dOptions=null)
        {
            // Find all shared edges and classify them
            var edgeDataList = FindSharedEdgesForFiberElements(triangulation, fibers);

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
                    new List<(int, int)>(), new List<int>(), new List<int>());
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

            // Write final mesh: triangles + fiber + quads/triangles
            if (dOptions != null && dOptions.Debug)
            {
                var fullMesh = new FEMesh(_globalNodes.ToList(), _elements.ToList(),
                    new List<(int, int)>(), new List<int>(), new List<int>());
                IO.VtkLegacyWriter.WriteUnstructuredMesh(dOptions.GetDebugFilePath("AllMesh"), fullMesh);
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
        /// Builds 1 fiber element for a shared edge with one fiber and one boundary node.
        /// </summary>
        private void BuildSingleFiberElement(
            Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes,
            Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            // Identify which node is the fiber and which is the boundary
            Node fiberNode;
            Fiber fiber;

            if (sharedEdgeNodes[0].Type == NodeType.FiberCenter || sharedEdgeNodes[0].Type == NodeType.ProjectedFiber)
            {
                fiberNode = sharedEdgeNodes[0];
                fiber = fibers[fiberNode.FiberId.Value];
            }
            else
            {
                fiberNode = sharedEdgeNodes[1];
                fiber = fibers[fiberNode.FiberId.Value];
            }

            // Get fiber center position (includes projection offset if applicable)
            var fiberCenter = fiberNode.P;

            // Find element nodes by matching the fiber node in both triangles
            var fiberNode_Tri1 = FindInteriorTriangleNodeByNode(triangle1Nodes, triangle1ElementNodes, fiberNode);
            var fiberNode_Tri2 = FindInteriorTriangleNodeByNode(triangle2Nodes, triangle2ElementNodes, fiberNode);

            // Determine if shared edge is in CCW order in triangle 1
            bool isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(triangle1Nodes, sharedEdgeNodes);

            // Determine fiber node order (creates curved fiber surface with 6 nodes)
            var fiberNodes = DetermineFiberNodeOrder(fiberCenter, fiberNode_Tri1, fiberNode_Tri2, fiber.Radius, isEdgeCCW);

            // Check for zero thickness (overlap)
            var thicknessCheck = new Point2D(
                fiberNodes[3].X - fiberNodes[2].X,
                fiberNodes[3].Y - fiberNodes[2].Y);
            double thickness = Math.Abs(thicknessCheck.X) + Math.Abs(thicknessCheck.Y);

            if (thickness < 1e-5)
                return; // Skip zero-thickness elements

            // Build and add the single fiber element
            AddFiberElement(fiberNodes, ElementPhase.Fiber);
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
        /// Builds 1 triangular matrix element for a shared edge with one fiber and one boundary node.
        /// Creates a 6-node triangular element connecting the fiber surface to the boundary point.
        /// </summary>
        private void BuildTriangularMatrixElement(
            Node[] triangle1Nodes, Node[] triangle2Nodes,
            Point2D[] triangle1ElementNodes, Point2D[] triangle2ElementNodes,
            Node[] sharedEdgeNodes,
            IReadOnlyList<Fiber> fibers,
            ElementConfig config)
        {
            // Identify which node is the fiber and which is the boundary
            Node fiberNode, boundaryNode;
            Fiber fiber;

            if (sharedEdgeNodes[0].Type == NodeType.FiberCenter || sharedEdgeNodes[0].Type == NodeType.ProjectedFiber)
            {
                fiberNode = sharedEdgeNodes[0];
                boundaryNode = sharedEdgeNodes[1];
                fiber = fibers[fiberNode.FiberId.Value];
            }
            else
            {
                fiberNode = sharedEdgeNodes[1];
                boundaryNode = sharedEdgeNodes[0];
                fiber = fibers[fiberNode.FiberId.Value];
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

            // Determine fiber node order (creates curved fiber surface with 6 nodes)
            var fiberNodes = DetermineFiberNodeOrder(fiberCenter, fiberNode_Tri1, fiberNode_Tri2, fiber.Radius, isEdgeCCW);

            // Build 6-node triangular matrix element
            // Node layout for 6-node triangle:
            //     node2
            //      /\
            //     /  \
            // n5 /    \ n3
            //   /      \
            //  /________\
            // node0  n4  node1
            //
            // Where n3, n4, n5 are midpoint nodes

            var triangleNodes = new Point2D[6];

            // Corner nodes:
            // - Two nodes on fiber surface (from fiber element nodes 2 and 4)
            // - One node at boundary point
            if (isEdgeCCW)
            {
                // Fiber node is first in shared edge
                triangleNodes[0] = fiberNodes[2];  // First point on fiber surface
                triangleNodes[1] = fiberNodes[4];  // Second point on fiber surface
                triangleNodes[2] = boundaryNode_Tri1; // Boundary point
            }
            else
            {
                // Boundary node is first in shared edge
                triangleNodes[0] = boundaryNode_Tri1; // Boundary point
                triangleNodes[1] = fiberNodes[2];  // First point on fiber surface
                triangleNodes[2] = fiberNodes[4];  // Second point on fiber surface
            }

            // Midpoint nodes:
            // n3: midpoint between node1 and node2
            triangleNodes[3] = new Point2D(
                (triangleNodes[1].X + triangleNodes[2].X) / 2.0,
                (triangleNodes[1].Y + triangleNodes[2].Y) / 2.0);

            // n4: midpoint between node0 and node1 (on fiber surface - use node 3 from fiber element)
            triangleNodes[4] = fiberNodes[3];

            // n5: midpoint between node2 and node0
            triangleNodes[5] = new Point2D(
                (triangleNodes[2].X + triangleNodes[0].X) / 2.0,
                (triangleNodes[2].Y + triangleNodes[0].Y) / 2.0);

            // Build and add the triangular matrix element
            AddTriangleElement(triangleNodes, ElementPhase.Matrix);
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
            if (node1.FiberId.HasValue && node2.FiberId.HasValue)
            {
                return node1.FiberId == node2.FiberId && node1.Offset == node2.Offset;
            }
            // For boundary nodes: match by position
            else if (!node1.FiberId.HasValue && !node2.FiberId.HasValue)
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
            string key1 = node1.FiberId.HasValue
                ? $"F{node1.FiberId.Value}_{node1.Offset.ox}_{node1.Offset.oy}"
                : $"B{node1.P.X:F10}_{node1.P.Y:F10}_{node1.Offset.ox}_{node1.Offset.oy}";

            string key2 = node2.FiberId.HasValue
                ? $"F{node2.FiberId.Value}_{node2.Offset.ox}_{node2.Offset.oy}"
                : $"B{node2.P.X:F10}_{node2.P.Y:F10}_{node2.Offset.ox}_{node2.Offset.oy}";

            // Order-independent key
            if (string.CompareOrdinal(key1, key2) < 0)
                return $"{key1}|{key2}";
            else
                return $"{key2}|{key1}";
        }
    }
}