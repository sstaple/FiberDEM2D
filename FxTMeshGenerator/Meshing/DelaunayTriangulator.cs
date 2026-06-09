using DelaunatorSharp;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FDEMCore;
using System.IO;

namespace FxTMeshGenerator.Meshing
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
        public TriangulationMesh2D GenerateTriangulation(CellBoundary boundary, IReadOnlyList<Fiber> fibers, DebugOptions? dOptions = null, MeshOptions? options = null)
        {
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

        private List<Node> CombineNodesAndAddProjections(List<Node> originalFiberNodes,List<Node> cornerNodes,
            List<Node> edgeNodes,CellBoundary boundary)
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
                        nodes.Add(new Node(p, null, NodeType.ProjectedBoundary, offset: (ox, oy)));
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
                    cleanedTris.Add(new int[3]{oldToNewIndexMap[ia],oldToNewIndexMap[ib],oldToNewIndexMap[ic]});
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
            if (includeCorners)
            {
                var cornerPts = boundary.Find2DCornersAtCurrentStrain();
                foreach (var cp in cornerPts)
                {
                    cornerNodes.Add(new Node(new Point2D(cp.X, cp.Y), null, NodeType.BoundaryCorner, (0, 0)));
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
                        edgeNodes.Add(new Node(new Point2D(p[1], p[2]), null, NodeType.BoundaryPoint, (0, 0)));
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

                            // Only add if BOTH checks pass (fully convex quadrilateral)
                            if (side1 * side2 < 0 && side3 * side4 < 0)
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

            if (side1 * side2 > 0)
            {
                // Both unique nodes on same side = concave quadrilateral = swap would overlap
                // Abort the swap
                return;
            }

            // Check 2: Shared edge nodes must be on OPPOSITE sides of the new diagonal
            // If they're on the same side, the new diagonal exits the quadrilateral
            double side3 = Side(p_unique1, p_unique2, p_shared1);
            double side4 = Side(p_unique1, p_unique2, p_shared2);

            if (side3 * side4 > 0)
            {
                // Both shared nodes on same side of new diagonal = new diagonal exits quad
                // Swapping would create overlapping triangles
                return;
            }

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
            const int MAX_ITERATIONS = 100;
            const int MAX_STALL_ITERATIONS = 5; // Stop if no progress after this many iterations

            int iterationCount = 0;
            int totalSwaps = 0;
            int stallCount = 0;
            var recentSwaps = new HashSet<(int, int)>(); // Track recent swaps to detect cycles
            bool anySwapOccurred;

            do
            {
                anySwapOccurred = false;
                iterationCount++;

                // Find all interior edges
                var interiorEdges = FindAllInteriorEdges(uniqueNodes, delaunay.Triangles);

                // Evaluate all edges and find the best single swap
                double bestPriority = -1;
                int bestTri1 = -1, bestTri2 = -1;
                int[] bestQuad = null;
                int[] bestSharedEdge = null; // Track the shared edge for the best swap
                (int inversions, int overlaps, double innerAspect, double outerAspect) bestCurrent = default;
                (int inversions, int overlaps, double innerAspect, double outerAspect) bestSwapped = default;

                // Track statistics for this iteration
                int inversionsFound = 0;
                int qualityImprovementsFound = 0;

                foreach (var (tri1Idx, tri2Idx, sharedEdge) in interiorEdges)
                {
                    var currentQuality = EvaluateQuadrilateralQuality(
                        tri1Idx, tri2Idx, uniqueNodes, delaunay.Triangles, fibers, out var quad, sharedEdge);

                    var swappedQuality = EvaluateSwappedQuadrilateralQuality(
                        tri1Idx, tri2Idx, quad, sharedEdge, uniqueNodes, delaunay.Triangles, fibers);

                    // Track inversions that could be fixed
                    if (swappedQuality.inversions < currentQuality.inversions)
                        inversionsFound++;

                    // Track quality improvements (overlaps/aspect ratio, no inversion change)
                    if (currentQuality.inversions == swappedQuality.inversions &&
                        IsConfigurationBetter(swappedQuality, currentQuality))
                        qualityImprovementsFound++;

                    // Check if swap is worthwhile and not a recent swap (cycle detection)
                    var swapKey = (Math.Min(tri1Idx, tri2Idx), Math.Max(tri1Idx, tri2Idx));
                    if (IsSwapWorthwhile(currentQuality, swappedQuality, ASPECT_RATIO_THRESHOLD) &&
                        !recentSwaps.Contains(swapKey))
                    {
                        double priority = CalculateSwapPriority(currentQuality, swappedQuality, uniqueNodes, fibers, quad);

                        if (priority > bestPriority)
                        {
                            bestPriority = priority;
                            bestTri1 = tri1Idx;
                            bestTri2 = tri2Idx;
                            bestQuad = quad;
                            bestSharedEdge = sharedEdge; // Store the shared edge
                            bestCurrent = currentQuality;
                            bestSwapped = swappedQuality;
                        }
                    }
                }

                // Perform the best swap if found
                int inversionSwapsPerformed = 0;
                int qualitySwapsPerformed = 0;

                if (bestPriority > -1)
                {
                    PerformEdgeSwap(bestTri1, bestTri2, bestSharedEdge, delaunay.Triangles, uniqueNodes);
                    totalSwaps++;
                    anySwapOccurred = true;
                    stallCount = 0; // Reset stall counter

                    // Track this swap for cycle detection (keep last 10 swaps)
                    var swapKey = (Math.Min(bestTri1, bestTri2), Math.Max(bestTri1, bestTri2));
                    recentSwaps.Add(swapKey);
                    if (recentSwaps.Count > 10)
                    {
                        recentSwaps.Remove(recentSwaps.First());
                    }

                    // Categorize the swap
                    if (bestSwapped.inversions < bestCurrent.inversions)
                        inversionSwapsPerformed = 1;
                    else if (bestSwapped.overlaps < bestCurrent.overlaps)
                        inversionSwapsPerformed = 1;  // Treat overlap fixes as important
                    else
                        qualitySwapsPerformed = 1;

                    // Write VTK after each swap for debugging
                    if (dOptions != null && dOptions.Debug)
                    {
                        // Convert flat triangle array to List<int[]>
                        var debugTris = new List<int[]>();
                        for (int t = 0; t < delaunay.Triangles.Length; t += 3)
                        {
                            debugTris.Add(new int[3] { delaunay.Triangles[t], delaunay.Triangles[t + 1], delaunay.Triangles[t + 2] });
                        }

                        //CreateVTKFileFromTriangles(uniqueNodes,debugTris,dOptions);
                        LogMessage($"  - Swapped triangles {bestTri1} and {bestTri2}: " +
                                  $"inv {bestCurrent.inversions}->{bestSwapped.inversions}, " +
                                  $"overlap {bestCurrent.overlaps}->{bestSwapped.overlaps}, " +
                                  $"innerAR {bestCurrent.innerAspect:F2}->{bestSwapped.innerAspect:F2}", dOptions);
                    }
                }
                else
                {
                    stallCount++;
                }

                // Log iteration summary
                LogMessage($"Iteration {iterationCount}: " +
                          $"# Inversions={inversionsFound}, Inversions Swapped={inversionSwapsPerformed}, " +
                          $"# Quality Improvements={qualityImprovementsFound}, Quality Swapped={qualitySwapsPerformed}", dOptions);

                // Stop if stalled (no beneficial swaps found)
                if (stallCount >= MAX_STALL_ITERATIONS)
                {
                    LogMessage($"Converged: No beneficial swaps found for {MAX_STALL_ITERATIONS} iterations.", dOptions);
                    break;
                }

                // Safety limit
                if (iterationCount >= MAX_ITERATIONS)
                {
                    LogMessage($"Warning: Reached maximum iterations ({MAX_ITERATIONS}). Stopping optimization.", dOptions);
                    break;
                }
            }
            while (anySwapOccurred || stallCount < MAX_STALL_ITERATIONS);

            LogMessage($"\n=== Optimization Complete ===", dOptions);
            LogMessage($"Total iterations: {iterationCount}", dOptions);
            LogMessage($"Total swaps performed: {totalSwaps}", dOptions);

            // Log final inversion count (edge triangles may be removed later during projection filtering)
            int finalInversions = CountTotalInversions(uniqueNodes, delaunay.Triangles, fibers);
            LogMessage($"Final inversion count: {finalInversions}", dOptions);

            // Close log file
            CloseLogFile();
        }

        /// <summary>
        /// Determines if a swap is worthwhile based on improvement and thresholds.
        /// </summary>
        private bool IsSwapWorthwhile((int inversions, int overlaps, double innerAspect, double outerAspect) current,
            (int inversions, int overlaps, double innerAspect, double outerAspect) swapped, double aspectThreshold)
        {
            // Always swap if it reduces inversions
            if (swapped.inversions < current.inversions)
                return true;

            // Don't swap if it increases inversions
            if (swapped.inversions > current.inversions)
                return false;

            // Always swap if it reduces overlaps (same inversions)
            if (swapped.overlaps < current.overlaps)
                return true;

            // Don't swap if it increases overlaps
            if (swapped.overlaps > current.overlaps)
                return false;

            // Same inversions and overlaps: check if we should bother swapping for aspect ratio alone
            // If already good (no inversions/overlaps), don't risk making it worse
            if (current.inversions == 0 && current.overlaps == 0)
                return false;

            // Has problems: only swap if inner aspect ratio is bad enough and swap improves it
            if (current.innerAspect > aspectThreshold && swapped.innerAspect < current.innerAspect)
                return true;

            return false;
        }

        /// <summary>
        /// Counts the total number of topological inversions across all triangles.
        /// </summary>
        private int CountTotalInversions(List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers)
        {
            int totalInversions = 0;
            int triangleCount = triangles.Length / 3;

            for (int i = 0; i < triangleCount; i++)
            {
                var tri = GetTriangleNodes(i, triangles);
                var (inversions, _, _, _) = EvaluateTriangleQuality(tri, nodes, fibers);
                totalInversions += inversions;
            }

            return totalInversions;
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
        private double CalculateSwapPriority(
            (int inversions, int overlaps, double innerAspect, double outerAspect) current,
            (int inversions, int overlaps, double innerAspect, double outerAspect) swapped,
            List<Node> nodes,
            IReadOnlyList<Fiber> fibers,
            int[] quad)
        {
            double priority = 0;

            // CRITICAL: Inversion reduction (weight: 10000 per inversion)
            int inversionReduction = current.inversions - swapped.inversions;
            if (inversionReduction > 0)
            {
                priority += 10000 * inversionReduction;
            }

            // VERY IMPORTANT: Overlap reduction (weight: 1000 per overlap)
            int overlapReduction = current.overlaps - swapped.overlaps;
            if (overlapReduction > 0)
            {
                priority += 1000 * overlapReduction;
            }

            // IMPORTANT: Inner aspect ratio improvement (weight: 100)
            // Lower aspect ratio is better
            double innerImprovement = current.innerAspect - swapped.innerAspect;
            if (innerImprovement > 0)
            {
                priority += 100 * innerImprovement;
            }

            // NICE TO HAVE: Outer aspect ratio improvement (weight: 10)
            double outerImprovement = current.outerAspect - swapped.outerAspect;
            if (outerImprovement > 0)
            {
                priority += 10 * outerImprovement;
            }

            return priority;
        }

        /// <summary>
        /// Evaluates the quality of a quadrilateral formed by two adjacent triangles.
        /// Returns (inversionCount, overlapCount, maxInnerAspectRatio, maxOuterAspectRatio) and outputs the 4-node quadrilateral.
        /// </summary>
        private (int inversions, int overlaps, double innerAspect, double outerAspect) EvaluateQuadrilateralQuality(
            int tri1Idx, int tri2Idx, List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers,
            out int[] quad, int[] sharedEdge)
        {
            var tri1 = GetTriangleNodes(tri1Idx, triangles);
            var tri2 = GetTriangleNodes(tri2Idx, triangles);

            // Find the 4 unique vertices forming the quadrilateral
            var allVertices = tri1.Concat(tri2).Distinct().ToArray();
            quad = allVertices;

            if (quad.Length != 4)
            {
                // Degenerate case - shouldn't happen
                return (int.MaxValue, int.MaxValue, double.MaxValue, double.MaxValue);
            }

            // Evaluate both triangles
            int totalInversions = 0;
            int totalOverlaps = 0;
            double maxInnerAspect = 0;
            double maxOuterAspect = 0;

            foreach (var tri in new[] { tri1, tri2 })
            {
                var (inversions, overlaps, innerAspect, outerAspect) = EvaluateTriangleQuality(tri, nodes, fibers);
                totalInversions += inversions;
                totalOverlaps += overlaps;
                maxInnerAspect = Math.Max(maxInnerAspect, innerAspect);
                maxOuterAspect = Math.Max(maxOuterAspect, outerAspect);
            }

            return (totalInversions, totalOverlaps, maxInnerAspect, maxOuterAspect);
        }

        /// <summary>
        /// Evaluates what the quality would be if we swapped the diagonal of a quadrilateral.
        /// Uses the EXACT same geometric orientation logic as PerformEdgeSwap to ensure consistency.
        /// </summary>
        private (int inversions, int overlaps, double innerAspect, double outerAspect) EvaluateSwappedQuadrilateralQuality(
            int tri1Idx, int tri2Idx, int[] quad, int[] sharedEdge, List<Node> nodes, int[] triangles, IReadOnlyList<Fiber> fibers)
        {
            if (quad.Length != 4)
                return (int.MaxValue, int.MaxValue, double.MaxValue, double.MaxValue);

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
        private (int inversions, int overlaps, double innerAspect, double outerAspect) EvaluateTwoTrianglesQuality(
            int[] tri1, int[] tri2, List<Node> nodes, IReadOnlyList<Fiber> fibers)
        {
            var (inv1, overlap1, inner1, outer1) = EvaluateTriangleQuality(tri1, nodes, fibers);
            var (inv2, overlap2, inner2, outer2) = EvaluateTriangleQuality(tri2, nodes, fibers);

            return (inv1 + inv2, overlap1 + overlap2, Math.Max(inner1, inner2), Math.Max(outer1, outer2));
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
        private (int topologicalInversions, int elementOverlaps, double innerAspectRatio, double outerAspectRatio) EvaluateTriangleQuality(int[] triangleNodeIndices,
            List<Node> nodes, IReadOnlyList<Fiber> fibers)
        {
            // Get the three nodes
            var nodeA = nodes[triangleNodeIndices[0]];
            var nodeB = nodes[triangleNodeIndices[1]];
            var nodeC = nodes[triangleNodeIndices[2]];

            // Calculate interior points for quality assessment
            // For fiber nodes: use surface midpoint; for boundary nodes: use the boundary point itself
            var interiorPoints = new Point2D[3];
            var triangleNodes = new[] { nodeA, nodeB, nodeC };

            for (int i = 0; i < 3; i++)
            {
                var currentNode = triangleNodes[i];
                var otherIndices = GetOtherIndices(i);
                var otherNode1 = triangleNodes[otherIndices[0]];
                var otherNode2 = triangleNodes[otherIndices[1]];

                // If this is a fiber node, calculate surface point; otherwise use boundary point directly
                if (currentNode.FiberId.HasValue)
                {
                    interiorPoints[i] = CalculateFiberSurfacePoint(
                        currentNode.P, fibers[currentNode.FiberId.Value].Radius,
                        otherNode1.P, otherNode2.P);
                }
                else
                {
                    // Boundary node: use its position directly
                    interiorPoints[i] = currentNode.P;
                }
            }

            // Calculate quality metrics
            int topologicalInversionCount = 0;
            int elementOverlapCount = 0;

            for (int i = 0; i < 3; i++)
            {
                // Only check fiber nodes
                if (triangleNodes[i].FiberId.HasValue)
                {
                    var fiberCenter = triangleNodes[i].P;
                    var otherIndices = GetOtherIndices(i);
                    var interiorPoint1 = interiorPoints[otherIndices[0]];
                    var interiorPoint2 = interiorPoints[otherIndices[1]];
                    var nodePos1 = triangleNodes[otherIndices[0]].P;
                    var nodePos2 = triangleNodes[otherIndices[1]].P;

                    // Priority 1: Topological inversion (fiber center vs node positions)
                    // This is a triangle-level problem where fiber center crosses the edge
                    bool topologicalInversion = !SameSide(fiberCenter, interiorPoints[i], nodePos1, nodePos2);
                    if (topologicalInversion)
                        topologicalInversionCount++;

                    // Priority 2: Element-level overlap (fiber surface vs surface points)
                    // This determines if the interior element overlaps the fiber
                    bool elementOverlap = IsInnerTriangleInverted(fiberCenter, interiorPoints[i], interiorPoint1, interiorPoint2);
                    if (elementOverlap)
                        elementOverlapCount++;
                }
            }

            // Priority 3: Inner triangle aspect ratio (lower is better)
            double innerAspectRatio = CalculateAspectRatio(interiorPoints[0], interiorPoints[1], interiorPoints[2]);

            // Priority 4: Outer triangle aspect ratio (lower is better)
            var outerNodePos0 = triangleNodes[0].P;
            var outerNodePos1 = triangleNodes[1].P;
            var outerNodePos2 = triangleNodes[2].P;
            double outerAspectRatio = CalculateAspectRatio(outerNodePos0, outerNodePos1, outerNodePos2);

            return (topologicalInversionCount, elementOverlapCount, innerAspectRatio, outerAspectRatio);
        }

        /// <summary>
        /// Checks if a fiber center crosses the line connecting two surface points (inversion check).
        /// Uses signed distance to determine which side of the line the fiber is on.
        /// </summary>
        private static bool IsInnerTriangleInverted(Point2D fiberCenter, Point2D surfacePointOfFiber, Point2D surfacePoint1, Point2D surfacePoint2)
        {
            // checks that the fiber center is on the same side of the line defined by surfacePoint1 and surfacePoint2 as the surface point of the fiber itself
            return !SameSide(fiberCenter, surfacePointOfFiber, surfacePoint1, surfacePoint2);
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
        private bool IsConfigurationBetter(
            (int inversions, int overlaps, double innerAspect, double outerAspect) config1,
            (int inversions, int overlaps, double innerAspect, double outerAspect) config2)
        {
            // Priority 1: Topological inversions are critical
            if (config1.inversions != config2.inversions)
                return config1.inversions < config2.inversions;

            // Priority 2: Element overlaps are important
            if (config1.overlaps != config2.overlaps)
                return config1.overlaps < config2.overlaps;

            // Priority 3: Inner triangle aspect ratio (lower is better)
            if (Math.Abs(config1.innerAspect - config2.innerAspect) > 0.01)
                return config1.innerAspect < config2.innerAspect;

            // Priority 4: Outer triangle aspect ratio (lower is better)
            return config1.outerAspect < config2.outerAspect;
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
