using DelaunatorSharp;
using FDEMCore.FxTMesh.Geometry;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FDEMCore;

namespace FDEMCore.FxTMesh.Meshing
{
    /// <summary>
    /// Delaunay-based triangulation of fiber centers with optional periodic tiling
    /// and optional boundary points along solid boundaries.
    ///
    /// Design goal: readability and predictable behavior for 20–2000 fibers.
    /// </summary>
    public sealed class DelaunayTriangulator
    {
        private StreamWriter? _logWriter;
        double areaTol;
        public TriangulationMesh2D GenerateTriangulation(CellBoundary boundary, IReadOnlyList<Fiber> fibers, DebugOptions? dOptions = null, MeshOptions? options = null)
        {
            areaTol = 1e-6 * boundary.ODimensions[1] * boundary.ODimensions[2];

            options ??= new MeshOptions();
            dOptions ??= new DebugOptions();
            if (fibers == null) throw new ArgumentNullException(nameof(fibers));
            if (fibers.Count == 0) throw new ArgumentException("Need at least one fiber.", nameof(fibers));

            // Step 1: Build original fiber nodes
            var originalFiberNodes = BuildOriginalFiberNodes(fibers);

            // Step 2: Add boundary points if non-periodic (separated into corners and edges)
            var (cornerNodes, edgeNodes) = AddBoundaryPoints(boundary, fibers.Count, options);

            // Step 3: Combine nodes and add periodic projections (corners NOT projected)
            var nodes = CombineNodesAndAddProjections(originalFiberNodes, cornerNodes, edgeNodes, boundary);

            // Step 4: Remove duplicate nodes
            var uniqueResult = RemoveDuplicateNodes(nodes, options.Tolerance);
            var uniqueNodes = uniqueResult.uniqueNodes;

            // Step 5: Perform Delaunay triangulation
            var delaunay = PerformDelaunayTriangulation(uniqueNodes);

            //Debugging: triangulation with all projections (BEFORE optimization)
            if (dOptions.Debug) CreateVTKFileFromTriangulation(uniqueNodes, delaunay, dOptions);

            // Step 6: Optimize triangulation quality before filtering (work on full triangulation with projections)
            OptimizeTriangulationQuality(uniqueNodes, delaunay, fibers, dOptions);

            //Debugging: triangulation AFTER optimization (before filtering)
            if (dOptions.Debug) CreateVTKFileFromTriangulation(uniqueNodes, delaunay, dOptions);

            // Step 7: Remove the triangles that are part of the projections (keep only those with original fibers or boundary points, and reject based on projection offsets)
            var (cleanedNodes, cleanedTris) = RemoveProjectionTriangles(uniqueNodes, delaunay,
                originalFiberNodes, cornerNodes, edgeNodes, options);

            //Debugging: triangulation after removing unwanted projections
            if (dOptions.Debug) CreateVTKFileFromTriangles(cleanedNodes, cleanedTris, dOptions);

            return new TriangulationMesh2D(cleanedNodes, cleanedTris);
        }

        private List<Node> BuildOriginalFiberNodes(IReadOnlyList<Fiber> fibers)
        {
            var originalFiberNodes = new List<Node>();
            for (int i = 0; i < fibers.Count; i++)
            {
                var f = fibers[i];
                // NOTE: FDEMCore uses 3D coords [x,y,z] where y-z is the working plane
                // Point2D(X,Y) maps to (y,z), so use indices [1] and [2]
                originalFiberNodes.Add(new Node(
                    new Point2D(f.CurrentPosition[1], f.CurrentPosition[2]),
                    i,
                    NodeType.FiberCenter,
                    offset: (0, 0)));
            }
            return originalFiberNodes;
        }

        private List<Node> CombineNodesAndAddProjections(List<Node> originalFiberNodes, List<Node> cornerNodes,
            List<Node> edgeNodes, CellBoundary boundary)
        {
            // Start with fibers, corners (no projections), and edge points
            var nodes = new List<Node>();
            nodes.AddRange(originalFiberNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)));
            nodes.AddRange(cornerNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)));
            nodes.AddRange(edgeNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)));

            // Get periodic projection information
            CellWall leftWall = boundary.Walls[2];
            var leftProj = leftWall.PeriodicProjection;
            CellWall bottomWall = boundary.Walls[4];
            var bottomProj = bottomWall.PeriodicProjection;

            var offsetsX = (leftWall.BoundaryType == BoundaryType.Periodic)
                ? new[] { -1, 0, 1 }
                : new[] { 0 };

            var offsetsY = (bottomWall.BoundaryType == BoundaryType.Periodic)
                ? new[] { -1, 0, 1 }
                : new[] { 0 };

            // Add projected fibers
            foreach (var ox in offsetsX)
                foreach (var oy in offsetsY)
                {
                    if (ox == 0 && oy == 0) continue;

                    for (int i = 0; i < originalFiberNodes.Count; i++)
                    {
                        var projectionVector = new Point2D(
                            ox * leftProj[1] + oy * bottomProj[1],
                            ox * leftProj[2] + oy * bottomProj[2]);
                        var f = originalFiberNodes[i];
                        var p = new Point2D(f.P.X + projectionVector.X, f.P.Y + projectionVector.Y);
                        nodes.Add(new Node(p, i, NodeType.ProjectedFiber, offset: (ox, oy)));
                    }
                }

            // Add projected boundary EDGE points (NOT corners)
            foreach (var ox in offsetsX)
                foreach (var oy in offsetsY)
                {
                    if (ox == 0 && oy == 0) continue;

                    for (int i = 0; i < edgeNodes.Count; i++)
                    {
                        var projectionVector = new Point2D(
                            ox * leftProj[1] + oy * bottomProj[1],
                            ox * leftProj[2] + oy * bottomProj[2]);
                        var b = edgeNodes[i];
                        var p = new Point2D(b.P.X + projectionVector.X, b.P.Y + projectionVector.Y);
                        nodes.Add(new Node(p, NodeType.ProjectedBoundary, offset: (ox, oy)));
                    }
                }

            return nodes;
        }

        private Delaunator PerformDelaunayTriangulation(List<Node> uniqueNodes)
        {
            var pts = uniqueNodes.Select(e => new MyPoint(e.P.X, e.P.Y)).ToArray();
            return new Delaunator(pts);
        }

        private (List<Node> cleanedNodes, List<int[]> cleanedTris) RemoveProjectionTriangles(List<Node> uniqueNodes, Delaunator delaunay,
            List<Node> originalFiberNodes, List<Node> cornerNodes, List<Node> edgeNodes, MeshOptions options)
        {
            var cleanedTris = new List<int[]>();
            var cleanedNodes = new List<Node>();
            var oldToNewIndexMap = new Dictionary<int, int>();

            // First pass: identify which nodes are used in valid triangles
            var usedNodeIndices = new HashSet<int>();

            for (int t = 0; t < delaunay.Triangles.Length; t += 3)
            {
                var ia = delaunay.Triangles[t];
                var ib = delaunay.Triangles[t + 1];
                var ic = delaunay.Triangles[t + 2];

                if (ShouldTheTriangleBeKept(uniqueNodes[ia], uniqueNodes[ib], uniqueNodes[ic]))
                {
                    // Mark these nodes as used
                    usedNodeIndices.Add(ia);
                    usedNodeIndices.Add(ib);
                    usedNodeIndices.Add(ic);
                }
            }

            // Second pass: build cleaned node list and create index mapping
            foreach (var oldIndex in usedNodeIndices.OrderBy(x => x))
            {
                int newIndex = cleanedNodes.Count;
                oldToNewIndexMap[oldIndex] = newIndex;
                cleanedNodes.Add(uniqueNodes[oldIndex]);
            }

            // Third pass: add triangles with remapped indices
            for (int t = 0; t < delaunay.Triangles.Length; t += 3)
            {
                var ia = delaunay.Triangles[t];
                var ib = delaunay.Triangles[t + 1];
                var ic = delaunay.Triangles[t + 2];

                // Skip if any node isn't in our used set
                if (!usedNodeIndices.Contains(ia) ||
                    !usedNodeIndices.Contains(ib) ||
                    !usedNodeIndices.Contains(ic))
                    continue;

                if (ShouldTheTriangleBeKept(uniqueNodes[ia], uniqueNodes[ib], uniqueNodes[ic]))
                {
                    // Add triangle with remapped indices ✅
                    cleanedTris.Add(new int[3] { oldToNewIndexMap[ia], oldToNewIndexMap[ib], oldToNewIndexMap[ic] });
                }
            }

            return (cleanedNodes, cleanedTris);
        }

        private static (List<Node> cornerNodes, List<Node> edgeNodes) AddBoundaryPoints(CellBoundary boundary, int nFibers, MeshOptions opt)
        {
            var cornerNodes = new List<Node>();
            var edgeNodes = new List<Node>();

            double area = boundary.ODimensions[1] * boundary.ODimensions[2];
            double spacing = Math.Sqrt(area / Math.Max(1, nFibers)) * opt.BoundaryPointSpacingMultiplier;

            // Add corner points as special nodes (will NOT be projected)
            bool includeCorners = boundary.Walls.Any(cw => cw.BoundaryType == BoundaryType.Solid);
            //setup the offsets of all of the corners:
            var cornerOffsets = new (int, int)[] { (0, 0), (0, 1), (1, 1), (1, 0) };// LowerLeft, UpperLeft, UpperRight, LowerRight

            if (includeCorners)
            {
                var cornerPts = boundary.Find2DCornersAtCurrentStrainDouble();
                for (int i = 0; i < cornerPts.Length; i++)
                {
                    cornerNodes.Add(new Node(new Point2D(cornerPts[i][0], cornerPts[i][1]),
                        NodeType.BoundaryCorner, cornerOffsets[i]));
                }
            }

            // Add edge points (will be projected for periodic boundaries)
            for (int i = 2; i < boundary.Walls.Length; i++)
            {
                if (boundary.Walls[i].BoundaryType == BoundaryType.Solid)
                {
                    var pts = boundary.GetBoundaryPoints(i, spacing, includeCorners: false);
                    foreach (var p in pts)
                    {
                        edgeNodes.Add(new Node(new Point2D(p[1], p[2]), NodeType.BoundaryPoint, (0, 0)));
                    }
                }
            }

            return (cornerNodes, edgeNodes);
        }


        #region Helper Methods


        private bool ShouldTheTriangleBeKept(Node a, Node b, Node c)
        {
            bool hasOriginalFiber = (a.Type == NodeType.FiberCenter ||
                                     b.Type == NodeType.FiberCenter ||
                                     c.Type == NodeType.FiberCenter);
            bool hasBoundary = (a.Type == NodeType.BoundaryPoint ||
                                     b.Type == NodeType.BoundaryPoint ||
                                     c.Type == NodeType.BoundaryPoint);
            bool keepTriangle = false;
            if (hasOriginalFiber || hasBoundary)
            {
                if ((a.Type == NodeType.ProjectedFiber && (a.Offset.ox == -1 || a.Offset == (0, -1))) ||
                    (b.Type == NodeType.ProjectedFiber && (b.Offset.ox == -1 || b.Offset == (0, -1))) ||
                    (c.Type == NodeType.ProjectedFiber && (c.Offset.ox == -1 || c.Offset == (0, -1))))
                { }


                else if ((a.Type == NodeType.ProjectedBoundary && (a.Offset.ox == -1 || a.Offset.oy == -1)) ||
                    (b.Type == NodeType.ProjectedBoundary && (b.Offset.ox == -1 || b.Offset.oy == -1)) ||
                    (c.Type == NodeType.ProjectedBoundary && (c.Offset.ox == -1 || c.Offset.oy == -1)))
                { }

                else
                    keepTriangle = true;
            }
            return keepTriangle;
        }

        private static (List<Node> uniqueNodes, Dictionary<int, int> indexMapping) RemoveDuplicateNodes(List<Node> nodes, double tolerance)
        {
            var uniqueNodes = new List<Node>();
            var indexMapping = new Dictionary<int, int>(); // old index -> new index
            var toleranceSq = tolerance * tolerance;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                bool isDuplicate = false;
                int duplicateIndex = -1;

                // Check if this node is a duplicate of any already added unique node
                for (int j = 0; j < uniqueNodes.Count; j++)
                {
                    var uniqueNode = uniqueNodes[j];
                    var dx = node.P.X - uniqueNode.P.X;
                    var dy = node.P.Y - uniqueNode.P.Y;
                    var distSq = dx * dx + dy * dy;

                    if (distSq <= toleranceSq)
                    {
                        isDuplicate = true;
                        duplicateIndex = j;
                    }
                }

                if (isDuplicate)
                {
                    // Map this old index to the existing unique node's index
                    indexMapping[i] = duplicateIndex;
                }
                else
                {
                    // Add as unique node and map to its new index
                    indexMapping[i] = uniqueNodes.Count;
                    uniqueNodes.Add(node);
                }
            }

            return (uniqueNodes, indexMapping);
        }

        /// <summary>
        /// Finds all interior edges (edges shared by two triangles, both with 3 fiber nodes).
        /// Returns list of (triangle1Index, triangle2Index, sharedEdgeNodeIndices[2]).
        /// </summary>
        private List<(int tri1, int tri2, int[] sharedEdge)> FindAllInteriorEdges(List<Node> nodes, int[] triangles)
        {
            var interiorEdges = new List<(int, int, int[])>();
            var processedPairs = new HashSet<(int, int)>();

            int triangleCount = triangles.Length / 3;

            for (int i = 0; i < triangleCount; i++)
            {
                var tri1 = GetTriangleNodes(i, triangles);

                // Check edges of this triangle
                var edges = new[] {
                    new[] { tri1[0], tri1[1] },
                    new[] { tri1[1], tri1[2] },
                    new[] { tri1[2], tri1[0] }
                };

                foreach (var edge in edges)
                {
                    // Find adjacent triangle sharing this edge
                    for (int j = i + 1; j < triangleCount; j++)
                    {
                        if (processedPairs.Contains((i, j)))
                            continue;

                        var tri2 = GetTriangleNodes(j, triangles);


                        // Check if tri2 shares this edge
                        int matchCount = 0;
                        foreach (var nodeIdx in tri2)
                        {
                            if (nodeIdx == edge[0] || nodeIdx == edge[1])
                                matchCount++;
                        }

                        if (matchCount == 2)
                        {
                            // Found adjacent triangles sharing an edge
                            // Before adding, check if the quadrilateral is CONVEX
                            // If concave, swapping would create overlapping triangles

                            // Find the two nodes NOT on the shared edge
                            var uniqueToTri1 = tri1.Except(edge).First();
                            var uniqueToTri2 = tri2.Except(edge).First();

                            // Get positions
                            var p_unique1 = nodes[uniqueToTri1].P;
                            var p_unique2 = nodes[uniqueToTri2].P;
                            var p_shared1 = nodes[edge[0]].P;
                            var p_shared2 = nodes[edge[1]].P;

                            // COMPLETE Convexity check for quadrilateral:
                            // 1. Unique nodes must be on OPPOSITE sides of current shared edge
                            double side1 = Side(p_shared1, p_shared2, p_unique1);
                            double side2 = Side(p_shared1, p_shared2, p_unique2);

                            // 2. Shared edge nodes must be on OPPOSITE sides of new diagonal (unique1-unique2)
                            double side3 = Side(p_unique1, p_unique2, p_shared1);
                            double side4 = Side(p_unique1, p_unique2, p_shared2);

                            // Only add if BOTH checks pass (fully convex quadrilateral) plus not colinear (area > areaTol)
                            if (side1 * side2 < 0 &&  side3 * side4 < 0 && Math.Abs(side1) > areaTol &&
                                Math.Abs(side2) > areaTol && Math.Abs(side3) > areaTol &&  Math.Abs(side4) > areaTol)
                            {
                                interiorEdges.Add((i, j, edge));
                            }
                            // else: concave quadrilateral or new diagonal exits quad, skip (swap would overlap)

                            processedPairs.Add((i, j));
                            break;
                        }
                    }
                }
            }

            return interiorEdges;
        }

        /// <summary>
        /// Gets the 3 node indices for a triangle.
        /// </summary>
        private int[] GetTriangleNodes(int triangleIndex, int[] triangles)
        {
            int baseIdx = triangleIndex * 3;
            return new[] { triangles[baseIdx], triangles[baseIdx + 1], triangles[baseIdx + 2] };
        }

        /// <summary>
        /// Calculates a surface point on a fiber (simplified version without overlap handling).
        /// </summary>
        private Point2D CalculateFiberSurfacePoint(Point2D fiberCenter, double fiberRadius, Point2D otherPoint1,
            Point2D otherPoint2)
        {
            // Create vectors from fiber center to the other two points
            var vec1 = MathHelper.MakeVector2D(fiberCenter, otherPoint1);
            var vec2 = MathHelper.MakeVector2D(fiberCenter, otherPoint2);

            // Normalize both vectors
            var vec1Normalized = Normalize(vec1);
            var vec2Normalized = Normalize(vec2);

            // Use bisector direction
            var bisectorDirection = new Point2D(
                vec1Normalized.X + vec2Normalized.X,
                vec1Normalized.Y + vec2Normalized.Y);
            var direction = Normalize(bisectorDirection);

            // Calculate point on fiber surface
            return new Point2D(
                fiberCenter.X + fiberRadius * direction.X,
                fiberCenter.Y + fiberRadius * direction.Y);
        }

        /// <summary>
        /// Normalizes a 2D vector.
        /// </summary>
        private Point2D Normalize(Point2D vector)
        {
            double length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            if (length < 1e-10)
                return new Point2D(1, 0);
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


        #endregion


        #region Methods to help quality evaltuation and re-triangulation decisions
        
        
        public sealed class SwapCandidate
        {
            public int Tri1 { get; init; }
            public int Tri2 { get; init; }
            public int[] SharedEdge { get; init; }
            public double Priority { get; init; }
            public int Version1 { get; init; }
            public int Version2 { get; init; }
        }


        /// <summary>
        /// Performs an edge swap on two adjacent triangles.
        /// Ensures proper winding order is maintained using geometric orientation.
        /// </summary>
        private void PerformEdgeSwap(int tri1Idx, int tri2Idx, int[] sharedEdge, int[] triangles, List<Node> nodes)
        {
            // Get current triangles
            var tri1 = GetTriangleNodes(tri1Idx, triangles);
            var tri2 = GetTriangleNodes(tri2Idx, triangles);

            // CRITICAL: Recompute the actual shared edge from the current triangle state
            // The passed-in sharedEdge might be stale if triangles were modified
            var actualSharedNodes = tri1.Intersect(tri2).ToArray();

            if (actualSharedNodes.Length != 2)
            {
                // Invalid configuration - triangles don't share exactly one edge
                return;
            }

            // Use the actual shared edge, not the passed-in one
            int sharedNode1 = actualSharedNodes[0];
            int sharedNode2 = actualSharedNodes[1];

            // Find the two nodes that are NOT on the shared edge (these form the new diagonal)
            var uniqueToTri1 = tri1.Except(actualSharedNodes).First();
            var uniqueToTri2 = tri2.Except(actualSharedNodes).First();

            // Get node positions for orientation checks
            var p_unique1 = nodes[uniqueToTri1].P;
            var p_unique2 = nodes[uniqueToTri2].P;
            var p_shared1 = nodes[sharedNode1].P;
            var p_shared2 = nodes[sharedNode2].P;

            // COMPLETE CONVEXITY CHECK: The quadrilateral must be convex for the swap to be valid
            // Check 1: If both unique nodes are on the SAME side of the shared edge, 
            // the quad is concave and swapping would create overlapping triangles
            double side1 = Side(p_shared1, p_shared2, p_unique1);
            double side2 = Side(p_shared1, p_shared2, p_unique2);

            // Check 2: Shared edge nodes must be on OPPOSITE sides of the new diagonal
            // If they're on the same side, the new diagonal exits the quadrilateral
            double side3 = Side(p_unique1, p_unique2, p_shared1);
            double side4 = Side(p_unique1, p_unique2, p_shared2);

            // Both unique nodes on same side = concave quadrilateral = swap would overlap
            // Abort the swap
            bool uniqueNodesOpposite =side1 * side2 < 0 && Math.Abs(side1) > areaTol && Math.Abs(side2) > areaTol;

            bool sharedNodesOpposite = side3 * side4 < 0 && Math.Abs(side3) > areaTol && Math.Abs(side4) > areaTol;

            if (!uniqueNodesOpposite || !sharedNodesOpposite)
                return;
            
            int baseTri1 = tri1Idx * 3;
            int baseTri2 = tri2Idx * 3;

            // Triangle 1: uniqueToTri1, sharedNode1, uniqueToTri2
            // Check orientation: uniqueToTri1 -> sharedNode1 -> uniqueToTri2
            double orient1 = Side(p_unique1, p_shared1, p_unique2);
            if (orient1 > 0)
            {
                // CCW
                triangles[baseTri1] = uniqueToTri1;
                triangles[baseTri1 + 1] = sharedNode1;
                triangles[baseTri1 + 2] = uniqueToTri2;
            }
            else
            {
                // CW: reverse to CCW
                triangles[baseTri1] = uniqueToTri1;
                triangles[baseTri1 + 1] = uniqueToTri2;
                triangles[baseTri1 + 2] = sharedNode1;
            }

            // Triangle 2: uniqueToTri2, sharedNode2, uniqueToTri1
            // Check orientation: uniqueToTri2 -> sharedNode2 -> uniqueToTri1
            double orient2 = Side(p_unique2, p_shared2, p_unique1);
            if (orient2 > 0)
            {
                // CCW
                triangles[baseTri2] = uniqueToTri2;
                triangles[baseTri2 + 1] = sharedNode2;
                triangles[baseTri2 + 2] = uniqueToTri1;
            }
            else
            {
                // CW: reverse to CCW
                triangles[baseTri2] = uniqueToTri2;
                triangles[baseTri2 + 1] = uniqueToTri1;
                triangles[baseTri2 + 2] = sharedNode2;
            }
        }

        /// <summary>
        /// Optimizes triangulation quality by evaluating and swapping edges based on:
        /// 1. Inversion prevention (CRITICAL): No fiber should cross the line connecting adjacent surface midpoints
        /// 2. Quality ratio optimization (IMPORTANT): Minimize ratio of surface spacing to inversion distance
        /// 
        /// Uses a priority-based single-swap approach with cycle detection.
        /// Operates on the full triangulation before filtering to allow optimization across periodic boundaries.
        /// </summary>
        private void OptimizeTriangulationQuality(List<Node> uniqueNodes, Delaunator delaunay, IReadOnlyList<Fiber> fibers, DebugOptions dOptions)
        {
            // Initialize log file
            InitializeLogFile(dOptions);

            LogMessage($"\n=== Starting Quality-Based Triangulation Optimization ===", dOptions);

            // Configuration parameters
            const double ASPECT_RATIO_THRESHOLD = 2.5; // Don't bother swapping if aspect ratio is already decent
            const int MAX_SWAPS = 10000;

            int triangleCount = delaunay.Triangles.Length / 3;
            int[] triangleVersions = new int[triangleCount];

            var queue = new PriorityQueue<SwapCandidate, double>();

            var initialEdges = FindAllInteriorEdges(uniqueNodes, delaunay.Triangles);

            foreach (var (tri1Idx, tri2Idx, sharedEdge) in initialEdges)
            {
                var candidate = CreateSwapCandidate(tri1Idx, tri2Idx, sharedEdge, uniqueNodes, delaunay.Triangles, fibers, triangleVersions, ASPECT_RATIO_THRESHOLD);
                EnqueueCandidate(queue, candidate);
            }

            int totalSwaps = 0;
            int staleCandidates = 0;
            int rejectedCandidates = 0;

            var recentSwapKeys = new Queue<string>();
            var recentSwapSet = new HashSet<string>();
            const int RECENT_SWAP_LIMIT = 50;

            // Continue processing candidate edge swaps until there are no more candidates,
            // or until we hit the safety limit on the total number of swaps.
            while (queue.Count > 0 && totalSwaps < MAX_SWAPS)
            {
                // Pull the currently best swap candidate from the priority queue.
                // This candidate was evaluated earlier, so it may now be stale.
                var candidate = queue.Dequeue();

                // Check whether either triangle has changed since this candidate was created.
                // If either version number is different, then the candidate was based on old geometry/connectivity.
                if (candidate.Version1 != triangleVersions[candidate.Tri1] || candidate.Version2 != triangleVersions[candidate.Tri2])
                {
                    staleCandidates++;
                    continue;
                }

                // Recompute the edge currently shared by the two triangles.
                // Even if the triangle versions match, this is a defensive check that they still share one edge.
                int[] currentSharedEdge = GetCurrentSharedEdge(candidate.Tri1, candidate.Tri2, delaunay.Triangles);

                // If the triangles no longer share exactly two nodes, they are no longer adjacent.
                // This candidate is invalid and should not be used.
                if (currentSharedEdge.Length != 2)
                {
                    rejectedCandidates++;
                    continue;
                }

                // Re-evaluate this swap using the current triangulation.
                // This protects against using an old priority/quality score.
                var refreshed = CreateSwapCandidate(candidate.Tri1, candidate.Tri2, currentSharedEdge, uniqueNodes, delaunay.Triangles, fibers, triangleVersions, ASPECT_RATIO_THRESHOLD);

                // If the refreshed candidate is not worthwhile, reject it.
                if (refreshed == null)
                {
                    rejectedCandidates++;
                    continue;
                }

                string swapKey = GetSwapKey(refreshed.Tri1, refreshed.Tri2);

                if (recentSwapSet.Contains(swapKey))
                {
                    rejectedCandidates++;
                    continue;
                }

                if (dOptions != null && dOptions.Debug && totalSwaps % 100 == 0)
                {
                    LogMessage(
                        $"Accepted tri=({refreshed.Tri1},{refreshed.Tri2}), priority={refreshed.Priority:G6}",
                        dOptions);
                }

                // Perform the actual edge swap on the two triangles.
                PerformEdgeSwap(refreshed.Tri1, refreshed.Tri2, refreshed.SharedEdge, delaunay.Triangles, uniqueNodes);

                recentSwapKeys.Enqueue(swapKey);
                recentSwapSet.Add(swapKey);

                if (recentSwapKeys.Count > RECENT_SWAP_LIMIT)
                {
                    string oldKey = recentSwapKeys.Dequeue();
                    recentSwapSet.Remove(oldKey);
                }

                // Mark both triangles as changed so any old queued candidates involving them become stale.
                triangleVersions[refreshed.Tri1]++;
                triangleVersions[refreshed.Tri2]++;

                // Count only accepted/performed swaps.
                totalSwaps++;

                // Periodically log the global mesh quality and queue statistics.
                if (dOptions != null && dOptions.Debug && totalSwaps % 100 == 0)
                {
                    var quality = CountTotalQuality(uniqueNodes, delaunay.Triangles, fibers);

                    LogMessage(
                        $"Swaps={totalSwaps}, queue={queue.Count}, stale={staleCandidates}, rejected={rejectedCandidates}, " +
                        $"inv={quality.Inversions}, crit={quality.CriticalOverlaps}, adj={quality.AdjustableOverlaps}, " +
                        $"innerAR={quality.InnerAspectRatio:F3}, outerAR={quality.OuterAspectRatio:F3}",
                        dOptions);
                }

                // Only the local neighborhood around the two changed triangles can have changed quality.
                // Re-evaluate and enqueue new candidates involving those local triangles.
                RequeueLocalCandidates(refreshed.Tri1, refreshed.Tri2, uniqueNodes, delaunay.Triangles, fibers, triangleVersions, queue, ASPECT_RATIO_THRESHOLD);
            }

            //If there are still inversions/overlaps and they are part of boundary points, we can try to move the boundary points to fix them.
            //This is a last resort, and should be done after all other swaps have been exhausted.
            //Check if there are any inversions or overlaps remaining, and if they are part of boundary points.
            //TODO: Implement boundary point adjustment to fix remaining inversions/overlaps.

            LogMessage($"\n=== Optimization Complete ===", dOptions);
            LogMessage($"Total swaps performed: {totalSwaps}", dOptions);
            LogMessage($"Stale candidates skipped: {staleCandidates}", dOptions);
            LogMessage($"Rejected candidates skipped: {rejectedCandidates}", dOptions);


            CloseLogFile();
        }

        private static string GetSwapKey(int tri1, int tri2)
        {
            return $"{Math.Min(tri1, tri2)}_{Math.Max(tri1, tri2)}";
        }

        private void RequeueLocalCandidates(int tri1Idx, int tri2Idx, List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers, int[] triangleVersions, PriorityQueue<SwapCandidate, double> queue, double aspectThreshold)
        {
            var affectedTriangles = FindAffectedTriangles(tri1Idx, tri2Idx, triangles);

            var addedPairs = new HashSet<string>();

            foreach (int triIdx in affectedTriangles)
            {
                var neighbors = FindTriangleNeighbors(triIdx, triangles);

                foreach (int neighborIdx in neighbors)
                {
                    int a = Math.Min(triIdx, neighborIdx);
                    int b = Math.Max(triIdx, neighborIdx);
                    string key = $"{a}_{b}";

                    if (!addedPairs.Add(key))
                        continue;

                    int[] sharedEdge = GetCurrentSharedEdge(a, b, triangles);

                    if (sharedEdge.Length != 2)
                        continue;

                    var candidate = CreateSwapCandidate(a, b, sharedEdge, nodes, triangles, fibers, triangleVersions, aspectThreshold);
                    EnqueueCandidate(queue, candidate);
                }
            }
        }

        private int[] GetCurrentSharedEdge(int tri1Idx, int tri2Idx, int[] triangles)
        {
            var tri1 = GetTriangleNodes(tri1Idx, triangles);
            var tri2 = GetTriangleNodes(tri2Idx, triangles);

            return tri1.Intersect(tri2).ToArray();
        }

        private HashSet<int> FindAffectedTriangles(int tri1Idx, int tri2Idx, int[] triangles)
        {
            var affected = new HashSet<int> { tri1Idx, tri2Idx };

            foreach (int neighbor in FindTriangleNeighbors(tri1Idx, triangles))
                affected.Add(neighbor);

            foreach (int neighbor in FindTriangleNeighbors(tri2Idx, triangles))
                affected.Add(neighbor);

            return affected;
        }

        private List<int> FindTriangleNeighbors(int triIdx, int[] triangles)
        {
            var neighbors = new List<int>();
            var tri = GetTriangleNodes(triIdx, triangles);
            int triangleCount = triangles.Length / 3;

            for (int otherIdx = 0; otherIdx < triangleCount; otherIdx++)
            {
                if (otherIdx == triIdx)
                    continue;

                var other = GetTriangleNodes(otherIdx, triangles);
                int sharedCount = tri.Count(node => other.Contains(node));

                if (sharedCount == 2)
                    neighbors.Add(otherIdx);
            }

            return neighbors;
        }

        /// <summary>
        /// Determines if a swap is worthwhile based on improvement and thresholds.
        /// </summary>
        private bool IsSwapWorthwhile(TriangleQuality current, TriangleQuality swapped, double aspectThreshold)
        {
            // Always swap if it reduces inversions
            if (swapped.Inversions < current.Inversions)
                return true;

            // Don't swap if it increases inversions
            if (swapped.Inversions > current.Inversions)
                return false;

            // Always swap if it reduces overlaps (same inversions)
            if (swapped.CriticalOverlaps < current.CriticalOverlaps)
                return true;

            // Don't swap if it increases overlaps
            if (swapped.CriticalOverlaps > current.CriticalOverlaps)
                return false;

            // Always swap if it reduces overlaps (same inversions)
            if (swapped.AdjustableOverlaps < current.AdjustableOverlaps)
                return true;

            // Don't swap if it increases overlaps
            if (swapped.AdjustableOverlaps > current.AdjustableOverlaps)
                return false;

            // Same inversions and overlaps: check if we should bother swapping for aspect ratio alone
            // If already good (no inversions/overlaps), don't risk making it worse
            if (current.Inversions == 0 && current.CriticalOverlaps == 0 && current.AdjustableOverlaps == 0)
                return false;

            // Has problems: only swap if inner aspect ratio is bad enough and swap improves it
            return current.InnerAspectRatio > aspectThreshold && swapped.InnerAspectRatio < current.InnerAspectRatio;
        }

        /// <summary>
        /// Counts the total number of topological inversions across all triangles.
        /// </summary>
        private TriangleQuality CountTotalQuality(List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers)
        {
            int totalInversions = 0;
            int totalCriticalOverlaps = 0;
            int totalAdjustableOverlaps = 0;
            double maxInnerAspectRatio = 0.0;
            double maxOuterAspectRatio = 0.0;

            int triangleCount = triangles.Length / 3;

            for (int i = 0; i < triangleCount; i++)
            {
                var tri = GetTriangleNodes(i, triangles);
                var quality = EvaluateTriangleQuality(tri, nodes, fibers);

                totalInversions += quality.Inversions;
                totalCriticalOverlaps += quality.CriticalOverlaps;
                totalAdjustableOverlaps += quality.AdjustableOverlaps;

                maxInnerAspectRatio = Math.Max(maxInnerAspectRatio, quality.InnerAspectRatio);
                maxOuterAspectRatio = Math.Max(maxOuterAspectRatio, quality.OuterAspectRatio);
            }

            return new TriangleQuality
            {
                Inversions = totalInversions,
                CriticalOverlaps = totalCriticalOverlaps,
                AdjustableOverlaps = totalAdjustableOverlaps,
                InnerAspectRatio = maxInnerAspectRatio,
                OuterAspectRatio = maxOuterAspectRatio
            };
        }

        /// <summary>
        /// Calculates priority score for a potential edge swap.
        /// Higher values indicate more important swaps.
        /// Priority hierarchy:
        /// 1. Inversion elimination (weight: 10000 per inversion)
        /// 2. Overlap elimination (weight: 1000 per overlap)
        /// 3. Inner aspect ratio improvement (weight: 100)
        /// 4. Outer aspect ratio improvement (weight: 10)
        /// </summary>
        private double CalculateSwapPriority(TriangleQuality current, TriangleQuality swapped)
        {
            double priority = 0.0;

            priority += 100000.0 * (current.Inversions - swapped.Inversions);
            priority += 10000.0 * (current.CriticalOverlaps - swapped.CriticalOverlaps);
            priority += 1000.0 * (current.AdjustableOverlaps - swapped.AdjustableOverlaps);
            priority += 100.0 * (current.InnerAspectRatio - swapped.InnerAspectRatio);
            priority += 10.0 * (current.OuterAspectRatio - swapped.OuterAspectRatio);

            return priority;
        }

        /// <summary>
        /// Evaluates the quality of a quadrilateral formed by two adjacent triangles.
        /// Returns (inversionCount, overlapCount, maxInnerAspectRatio, maxOuterAspectRatio) and outputs the 4-node quadrilateral.
        /// </summary>
        private TriangleQuality EvaluateQuadrilateralQuality(int tri1Idx, int tri2Idx, List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers,
            out int[] quad, int[] sharedEdge)
        {
            var tri1 = GetTriangleNodes(tri1Idx, triangles);
            var tri2 = GetTriangleNodes(tri2Idx, triangles);

            // Find the 4 unique vertices forming the quadrilateral
            var allVertices = tri1.Concat(tri2).Distinct().ToArray();
            quad = allVertices;

            // Evaluate both triangles
            var q1 = EvaluateTriangleQuality(tri1, nodes, fibers);
            var q2 = EvaluateTriangleQuality(tri2, nodes, fibers);

            return CombineTriangleQuality(q1, q2);
        }

        private SwapCandidate? CreateSwapCandidate(int tri1Idx, int tri2Idx, int[] sharedEdge, List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers, int[] triangleVersions, double aspectThreshold)
        {
            var currentQuality = EvaluateQuadrilateralQuality(tri1Idx, tri2Idx, nodes, triangles, fibers, out var quad, sharedEdge);
            var swappedQuality = EvaluateSwappedQuadrilateralQuality(tri1Idx, tri2Idx, quad, sharedEdge, nodes, triangles, fibers);

            if (!IsSwapWorthwhile(currentQuality, swappedQuality, aspectThreshold))
                return null;

            double priority = CalculateSwapPriority(currentQuality, swappedQuality);

            if (priority <= 1e-12)
                return null;

            return new SwapCandidate
            {
                Tri1 = tri1Idx,
                Tri2 = tri2Idx,
                SharedEdge = sharedEdge,
                Priority = priority,
                Version1 = triangleVersions[tri1Idx],
                Version2 = triangleVersions[tri2Idx]
            };
        }

        private void EnqueueCandidate(PriorityQueue<SwapCandidate, double> queue, SwapCandidate? candidate)
        {
            if (candidate == null)
                return;

            queue.Enqueue(candidate, -candidate.Priority);
        }

        /// <summary>
        /// Evaluates what the quality would be if we swapped the diagonal of a quadrilateral.
        /// Uses the EXACT same geometric orientation logic as PerformEdgeSwap to ensure consistency.
        /// </summary>
        private TriangleQuality EvaluateSwappedQuadrilateralQuality(
            int tri1Idx, int tri2Idx, int[] quad, int[] sharedEdge, List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers)
        {
            // Get the current triangles
            var tri1 = GetTriangleNodes(tri1Idx, triangles);
            var tri2 = GetTriangleNodes(tri2Idx, triangles);

            // Find the two nodes that are NOT on the shared edge (these will form the new diagonal)
            var uniqueToTri1 = tri1.Except(sharedEdge).First();
            var uniqueToTri2 = tri2.Except(sharedEdge).First();

            int sharedNode1 = sharedEdge[0];
            int sharedNode2 = sharedEdge[1];

            // Get node positions for orientation checks (EXACT SAME logic as PerformEdgeSwap)
            var p_unique1 = nodes[uniqueToTri1].P;
            var p_unique2 = nodes[uniqueToTri2].P;
            var p_shared1 = nodes[sharedNode1].P;
            var p_shared2 = nodes[sharedNode2].P;

            // Create swapped triangles with proper geometric orientation
            int[] swappedTri1, swappedTri2;

            // Triangle 1: uniqueToTri1, sharedNode1, uniqueToTri2
            // MUST MATCH PerformEdgeSwap line 1047
            double orient1 = Side(p_unique1, p_shared1, p_unique2);
            if (orient1 > 0)
            {
                // CCW
                swappedTri1 = new[] { uniqueToTri1, sharedNode1, uniqueToTri2 };
            }
            else
            {
                // CW: reverse to CCW
                swappedTri1 = new[] { uniqueToTri1, uniqueToTri2, sharedNode1 };
            }

            // Triangle 2: uniqueToTri2, sharedNode2, uniqueToTri1
            // MUST MATCH PerformEdgeSwap line 1065
            double orient2 = Side(p_unique2, p_shared2, p_unique1);
            if (orient2 > 0)
            {
                // CCW
                swappedTri2 = new[] { uniqueToTri2, sharedNode2, uniqueToTri1 };
            }
            else
            {
                // CW: reverse to CCW
                swappedTri2 = new[] { uniqueToTri2, uniqueToTri1, sharedNode2 };
            }

            // Evaluate the swapped configuration
            return EvaluateTwoTrianglesQuality(swappedTri1, swappedTri2, nodes, fibers);
        }

        /// <summary>
        /// Evaluates the combined quality of two triangles.
        /// Returns worst-case metrics across both triangles.
        /// </summary>
        private TriangleQuality EvaluateTwoTrianglesQuality(int[] tri1, int[] tri2, List<Node> nodes, IReadOnlyList<Fiber> fibers)
        {
            var q1 = EvaluateTriangleQuality(tri1, nodes, fibers);
            var q2 = EvaluateTriangleQuality(tri2, nodes, fibers);

            return CombineTriangleQuality(q1, q2);
        }

        private TriangleQuality CombineTriangleQuality(TriangleQuality q1, TriangleQuality q2)
        {
            return new TriangleQuality
            {
                Inversions = q1.Inversions + q2.Inversions,
                CriticalOverlaps = q1.CriticalOverlaps + q2.CriticalOverlaps,
                AdjustableOverlaps = q1.AdjustableOverlaps + q2.AdjustableOverlaps,
                InnerAspectRatio = Math.Max(q1.InnerAspectRatio, q2.InnerAspectRatio),
                OuterAspectRatio = Math.Max(q1.OuterAspectRatio, q2.OuterAspectRatio)
            };
        }

        /// <summary>
        /// Evaluates the quality of a single triangle using aspect ratio.
        /// Returns (topologicalInversions, elementOverlapCount, innerTriangleAspectRatio, outerTriangleAspectRatio).
        /// Priority for swapping (lower is better):
        /// 1. Topological inversions (fiber center outside triangle edge) - CRITICAL
        /// 2. Element overlaps (fiber surface outside inner triangle) - count how many fibers
        /// 3. Inner triangle aspect ratio (minimize for good element quality)
        /// 4. Outer triangle aspect ratio (minimize for good triangle shape)
        /// Goal: minimize in order of priority.
        /// </summary>
        private TriangleQuality EvaluateTriangleQuality(int[] triangleNodeIndices, List<Node> nodes, IReadOnlyList<Fiber> fibers)
        {
            var triangleNodes = new[]{ nodes[triangleNodeIndices[0]],nodes[triangleNodeIndices[1]],nodes[triangleNodeIndices[2]]};

            return new TriangleQuality(triangleNodes, fibers);
        }

        /// <summary>
        /// Checks if a fiber center is on the same side of a triangle edge as the fiber's surface point.
        /// </summary>
        private static bool DoesFiberOverlapTriangleSide(Point2D fiberCenter, Point2D surfacePointOfFiber, Point2D triangleCorner1, Point2D triangleCorner2)
        {
            // checks that the fiber center is on the same side of the line defined by surfacePoint1 and surfacePoint2 as the surface point of the fiber itself
            return !SameSide(fiberCenter, surfacePointOfFiber, triangleCorner1, triangleCorner2);
        }

        private static double Side(Point2D p2, Point2D p3, Point2D q)
        {
            return (p3.X - p2.X) * (q.Y - p2.Y)
                 - (p3.Y - p2.Y) * (q.X - p2.X);
        }

        private static bool SameSide(Point2D p1, Point2D pref, Point2D p2, Point2D p3, double tol = 1e-12)
        {
            double s1 = Side(p2, p3, p1);
            double s2 = Side(p2, p3, pref);

            return s1 * s2 > tol;
        }

        /// <summary>
        /// Calculates the aspect ratio of a triangle (longest edge / shortest edge).
        /// Lower values are better (1.0 = equilateral).
        /// </summary>
        private double CalculateAspectRatio(Point2D p1, Point2D p2, Point2D p3)
        {
            double edge1 = MathHelper.CalcDistanceBetweenTwoPoints(p1, p2);
            double edge2 = MathHelper.CalcDistanceBetweenTwoPoints(p2, p3);
            double edge3 = MathHelper.CalcDistanceBetweenTwoPoints(p3, p1);

            double maxEdge = Math.Max(edge1, Math.Max(edge2, edge3));
            double minEdge = Math.Min(edge1, Math.Min(edge2, edge3));

            if (minEdge < 1e-10)
                return double.MaxValue; // Degenerate triangle

            return maxEdge / minEdge;
        }

        /// <summary>
        /// Compares two quality configurations.
        /// Returns true if config1 is better than config2.
        /// Priority (in order):
        /// 1. Fewer topological inversions (CRITICAL)
        /// 2. Fewer element overlaps (IMPORTANT)
        /// 3. Lower inner triangle aspect ratio (QUALITY)
        /// 4. Lower outer triangle aspect ratio (SHAPE)
        /// </summary>
        private bool IsConfigurationBetter(TriangleQuality config1, TriangleQuality config2)
        {
            if (config1.Inversions != config2.Inversions)
                return config1.Inversions < config2.Inversions;

            if (config1.CriticalOverlaps != config2.CriticalOverlaps)
                return config1.CriticalOverlaps < config2.CriticalOverlaps;

            if (config1.AdjustableOverlaps != config2.AdjustableOverlaps)
                return config1.AdjustableOverlaps < config2.AdjustableOverlaps;

            if (Math.Abs(config1.InnerAspectRatio - config2.InnerAspectRatio) > 0.01)
                return config1.InnerAspectRatio < config2.InnerAspectRatio;

            return config1.OuterAspectRatio < config2.OuterAspectRatio;
        }

        #endregion


        #region Error Handling, debugging, Logging


        private void CreateVTKFileFromTriangulation(List<Node> uniqueNodes, Delaunator delauny, DebugOptions dOptions, string suffix = "DelaunayTriangulation")
        {
            var debugNodes = uniqueNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)).ToList();
            var debugTris = new List<int[]>();

            for (int t = 0; t < delauny.Triangles.Length; t += 3)
            {
                debugTris.Add(new int[3] { delauny.Triangles[t], delauny.Triangles[t + 1], delauny.Triangles[t + 2] });
            }
            CreateVTKFileFromTriangles(uniqueNodes, debugTris, dOptions, suffix);
        }

        private void CreateVTKFileFromTriangles(List<Node> uniqueNodes, List<int[]> triangles, DebugOptions dOptions, string suffix = "DelaunayTriangulation")
        {
            var debugNodes = uniqueNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)).ToList();

            TriangulationMesh2D debugMesh = new TriangulationMesh2D(debugNodes, triangles);
            IO.VtkLegacyWriter.WriteUnstructuredGrid2D(dOptions.GetDebugFilePath(suffix), debugMesh);
        }

        /// <summary>
        /// Helper method to write messages to console and log file.
        /// </summary>
        private void LogMessage(string message, DebugOptions dOptions)
        {
            if (dOptions != null && dOptions.Debug)
            {
                Console.WriteLine(message);
                _logWriter?.WriteLine(message);
                _logWriter?.Flush(); // Ensure it's written immediately
            }
        }

        /// <summary>
        /// Initializes the log file for optimization logging.
        /// </summary>
        private void InitializeLogFile(DebugOptions dOptions)
        {
            if (dOptions != null && dOptions.Debug)
            {
                try
                {
                    // Ensure directory exists
                    if (!Directory.Exists(dOptions.Directory))
                        Directory.CreateDirectory(dOptions.Directory);

                    // Create log file in the same directory as VTK files
                    string logPath = Path.Combine(dOptions.Directory, $"{dOptions.FileName}_optimization_log.txt");
                    _logWriter = new StreamWriter(logPath, false); // false = overwrite existing
                    _logWriter.WriteLine($"Triangulation Optimization Log - {DateTime.Now}");
                    _logWriter.WriteLine(new string('=', 60));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create log file: {ex.Message}");
                    _logWriter = null;
                }
            }
        }

        /// <summary>
        /// Closes the log file.
        /// </summary>
        private void CloseLogFile()
        {
            if (_logWriter != null)
            {
                _logWriter.WriteLine(new string('=', 60));
                _logWriter.WriteLine($"Log closed - {DateTime.Now}");
                _logWriter.Close();
                _logWriter.Dispose();
                _logWriter = null;
            }
        }

        #endregion


        #region Helper Classes


        /// <summary>
        /// This is a little class needed for the Delaunator.  Kind of dumb actually, but whatever
        /// </summary>
        public class MyPoint : DelaunatorSharp.IPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public MyPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
        #endregion
    }
}
