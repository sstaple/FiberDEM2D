using DelaunatorSharp;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FDEMCore;

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
        public TriangulationMesh2D GenerateTriangulation(CellBoundary boundary, IReadOnlyList<Fiber> fibers, DebugOptions? dOptions = null, MeshOptions? options = null)
        {
            options ??= new MeshOptions();
            dOptions ??= new DebugOptions();
            if (fibers == null) throw new ArgumentNullException(nameof(fibers));
            if (fibers.Count == 0) throw new ArgumentException("Need at least one fiber.", nameof(fibers));

            // Step 1: Build original fiber nodes
            var originalFiberNodes = BuildOriginalFiberNodes(fibers);

            // Step 2: Add boundary points (separated into corners and edges)
            var (cornerNodes, edgeNodes) = AddBoundaryPoints(boundary, fibers.Count, options);

            // Step 3: Combine nodes and add periodic projections (corners NOT projected)
            var nodes = CombineNodesAndAddProjections(originalFiberNodes, cornerNodes, edgeNodes, boundary);

            // Step 4: Remove duplicate nodes
            var uniqueResult = RemoveDuplicateNodes(nodes, options.Tolerance);
            var uniqueNodes = uniqueResult.uniqueNodes;

            // Step 5: Perform Delaunay triangulation
            var delaunay = PerformDelaunayTriangulation(uniqueNodes);

            //Debugging
            if (dOptions.Debug)  CreateVTKFileFromTriangulation(uniqueNodes, delaunay, dOptions, 1);


            // Step 6: Filter and clean triangles
            var (cleanedNodes, cleanedTris) = FilterAndCleanTriangles(uniqueNodes, delaunay,
                originalFiberNodes, cornerNodes, edgeNodes, options);

            //Debugging
            if (dOptions.Debug) CreateVTKFileFromTriangles(cleanedNodes, cleanedTris, dOptions, 2);
            

            // Step 7: Find and fix overlapping triads
            FindAndFixOverlappingTriads(cleanedNodes, cleanedTris, fibers, dOptions);

            //Debugging
            if (dOptions.Debug) CreateVTKFileFromTriangles(cleanedNodes, cleanedTris, dOptions, 3);

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

        private void CreateVTKFileFromTriangulation(List<Node> uniqueNodes, Delaunator delauny, DebugOptions dOptions, int ID)
        {
            var debugNodes = uniqueNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)).ToList();
            var debugTris = new List<int[]>();

            for (int t = 0; t < delauny.Triangles.Length; t += 3)
            {
                debugTris.Add(new int[3] { delauny.Triangles[t], delauny.Triangles[t + 1], delauny.Triangles[t + 2] });
            }
            CreateVTKFileFromTriangles(uniqueNodes, debugTris, dOptions, ID);
        }

        private void CreateVTKFileFromTriangles(List<Node> uniqueNodes, List<int[]> triangles, DebugOptions dOptions, int ID)
        {
            var debugNodes = uniqueNodes.Select(e => new Node(new Point2D(e.P.X, e.P.Y), e.FiberId, e.Type, e.Offset)).ToList();
            
            TriangulationMesh2D debugMesh = new TriangulationMesh2D(debugNodes, triangles);
            string path = System.IO.Path.Combine(dOptions.Directory, $"{dOptions.FileName}_DelaunayTriangulation{ID}.vtk");
            IO.VtkLegacyWriter.WriteUnstructuredGrid2D(path, debugMesh);
        }

        private (List<Node> cleanedNodes, List<int[]> cleanedTris) FilterAndCleanTriangles(List<Node> uniqueNodes, Delaunator delaunay,
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

                var a = uniqueNodes[ia];  // Fixed: use uniqueNodes
                var b = uniqueNodes[ib];
                var c = uniqueNodes[ic];

                bool hasOriginalFiber = (a.Type == NodeType.FiberCenter ||
                                         b.Type == NodeType.FiberCenter ||
                                         c.Type == NodeType.FiberCenter);
                bool hasBoundary = (a.Type == NodeType.BoundaryPoint ||
                                         b.Type == NodeType.BoundaryPoint ||
                                         c.Type == NodeType.BoundaryPoint);

                // Define which triangles to keep
                if (hasOriginalFiber || hasBoundary)
                {
                    // Reject triangles with specific projected fiber offsets
                    if ((a.Type == NodeType.ProjectedFiber && (a.Offset.ox == -1 || a.Offset == (0, -1))) ||
                        (b.Type == NodeType.ProjectedFiber && (b.Offset.ox == -1 || b.Offset == (0, -1))) ||
                        (c.Type == NodeType.ProjectedFiber && (c.Offset.ox == -1 || c.Offset == (0, -1))))
                        continue;

                    // Reject triangles with projected boundary vertices
                    if ((a.Type == NodeType.ProjectedBoundary && (a.Offset.ox == -1 || a.Offset.oy == -1)) ||
                        (b.Type == NodeType.ProjectedBoundary && (b.Offset.ox == -1 || b.Offset.oy == -1)) ||
                        (c.Type == NodeType.ProjectedBoundary && (c.Offset.ox == -1 || c.Offset.oy == -1)))
                        continue;
                    //continue;

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

                var a = uniqueNodes[ia];
                var b = uniqueNodes[ib];
                var c = uniqueNodes[ic];

                bool hasOriginalFiber = (a.Type == NodeType.FiberCenter ||
                                         b.Type == NodeType.FiberCenter ||
                                         c.Type == NodeType.FiberCenter);
                bool hasBoundary = (a.Type == NodeType.BoundaryPoint ||
                                         b.Type == NodeType.BoundaryPoint ||
                                         c.Type == NodeType.BoundaryPoint);

                if (hasOriginalFiber || hasBoundary)
                {
                    if ((a.Type == NodeType.ProjectedFiber && (a.Offset.ox == -1 || a.Offset == (0, -1))) ||
                        (b.Type == NodeType.ProjectedFiber && (b.Offset.ox == -1 || b.Offset == (0, -1))) ||
                        (c.Type == NodeType.ProjectedFiber && (c.Offset.ox == -1 || c.Offset == (0, -1))))
                        continue;

                    if ((a.Type == NodeType.ProjectedBoundary && (a.Offset.ox == -1 || a.Offset.oy == -1 )) ||
                        (b.Type == NodeType.ProjectedBoundary && (b.Offset.ox == -1 || b.Offset.oy == -1)) ||
                        (c.Type == NodeType.ProjectedBoundary && (c.Offset.ox == -1 || c.Offset.oy == -1)))
                    continue;

                    // Add triangle with remapped indices ✅
                    cleanedTris.Add(new int[3]
                    {
                oldToNewIndexMap[ia],
                oldToNewIndexMap[ib],
                oldToNewIndexMap[ic]
                    });
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
        /// Finds and fixes triangles where a fiber overlaps the triangle interior.
        /// This can happen in tightly packed configurations and requires retriangulation.
        /// </summary>
        private void FindAndFixOverlappingTriads(List<Node> nodes, List<int[]> triangles, IReadOnlyList<Fiber> fibers, DebugOptions dOptions, int maxIterations = 5)
        {
            int iteration = 0;
            int swapCount = 0;
            bool foundOverlap;

            do
            {
                foundOverlap = false;
                iteration++;

                // Recreate triads at the start of each iteration to reflect any swaps
                var triads = CreateTriadsFromTriangles(triangles, nodes, fibers);
                Console.WriteLine($"Iteration {iteration}: Created {triads.Count} triads for overlap checking");

                for (int i = 0; i < triads.Count; i++)
                {
                    if (triads[i].DetermineIfFibersOverlapTriad())
                    {
                        foundOverlap = true;
                        Console.WriteLine($"  Overlap detected in triad {triads[i].Number}");

                        bool swapped = Retriangulate(triads[i], triads, triangles, nodes);

                        if (swapped)
                        {
                            swapCount++;
                            Console.WriteLine($"  -> Swap #{swapCount} performed, triangulation modified");

                            // Debug: Create VTK file after each swap
                            if (dOptions.Debug)
                            {
                                CreateVTKFileFromTriangles(nodes, triangles, dOptions, 3000 + swapCount);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  -> No adjacent triad found for swap");
                        }

                        break; // Restart search with fresh triads after modifying triangulation
                    }
                }

                if (iteration >= maxIterations)
                {
                    Console.WriteLine($"Warning: Possible fiber/triad overlap after {maxIterations} iterations ({swapCount} swaps performed).");
                    foundOverlap = false;
                }
            }
            while (foundOverlap);

            Console.WriteLine($"Overlap fixing complete: {swapCount} total swaps in {iteration} iterations");
        }

        /// <summary>
        /// Creates Triad objects from triangle connectivity.
        /// Creates triads for triangles with THREE fiber nodes (original or projected).
        /// </summary>
        private List<Triad> CreateTriadsFromTriangles(List<int[]> triangles, List<Node> nodes, IReadOnlyList<Fiber> fibers)
        {
            var triads = new List<Triad>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var nodeA = nodes[tri[0]];
                var nodeB = nodes[tri[1]];
                var nodeC = nodes[tri[2]];

                // Create triads for triangles with THREE fiber nodes (original or projected)
                // Exclude triangles with boundary nodes
                if (nodeA.FiberId.HasValue && nodeB.FiberId.HasValue && nodeC.FiberId.HasValue &&
                    (nodeA.Type == NodeType.FiberCenter || nodeA.Type == NodeType.ProjectedFiber) && 
                    (nodeB.Type == NodeType.FiberCenter || nodeB.Type == NodeType.ProjectedFiber) && 
                    (nodeC.Type == NodeType.FiberCenter || nodeC.Type == NodeType.ProjectedFiber))
                {
                    var triadFibers = new[]
                    {
                        fibers[nodeA.FiberId.Value],
                        fibers[nodeB.FiberId.Value],
                        fibers[nodeC.FiberId.Value]
                    };

                    // Pass actual node positions (which include projection offsets)
                    var nodePositions = new[]
                    {
                        nodeA.P,
                        nodeB.P,
                        nodeC.P
                    };

                    var triad = new Triad(i, triadFibers, nodePositions);
                    triad.SetEdgesWithFiberIndices(
                        nodeA.FiberId.Value,
                        nodeB.FiberId.Value,
                        nodeC.FiberId.Value);

                    triads.Add(triad);
                }
            }

            return triads;
        }

        /// <summary>
        /// Retriangulates by swapping the diagonal of two adjacent triangles.
        /// Returns true if a swap was performed, false otherwise.
        /// </summary>
        private bool Retriangulate(Triad overlappingTriad, List<Triad> allTriads, List<int[]> triangles, List<Node> nodes)
        {
            // Find which fibers don't overlap
            int[] nonOverlapFiberIndices = Enumerable.Range(0, 3)
                .Where(i => overlappingTriad.FibersWhichOverlapTriad[i] == 0)
                .ToArray();

            if (nonOverlapFiberIndices.Length != 2)
                return false; // Can't retriangulate if we don't have exactly one overlapping fiber

            int overlapFiberIdx = Enumerable.Range(0, 3)
                .First(i => overlappingTriad.FibersWhichOverlapTriad[i] != 0);

            // Find which edge connects the two non-overlapping fibers
            int edgeIdx = FindEdgeConnectingFibers(nonOverlapFiberIndices[0], nonOverlapFiberIndices[1]);

            // Get the actual fiber IDs from that edge
            int[] nonOverlappingEdge = new[]
            {
                overlappingTriad.Edges[edgeIdx, 0],
                overlappingTriad.Edges[edgeIdx, 1]
            };

            // Find the adjacent triad sharing this edge
            foreach (var otherTriad in allTriads)
            {
                if (otherTriad.Number == overlappingTriad.Number)
                    continue;

                // Get all three fiber IDs from the other triad
                int[] otherTriadFibers = new[]
                {
                    otherTriad.Edges[0, 0],  // Fiber from edge 0
                    otherTriad.Edges[0, 1],  // Fiber from edge 0
                    otherTriad.Edges[1, 1]   // Third fiber (not in edge 0)
                }.Distinct().ToArray();

                if (Triad.IsFiberPairInTriad(nonOverlappingEdge, otherTriadFibers))
                {
                    // Swap the diagonal (matching MATLAB behavior - no boundary check)
                    SwapTriangleDiagonal(overlappingTriad, otherTriad, overlapFiberIdx, triangles, nodes);
                    return true; // Swap performed
                }
            }

            return false; // No adjacent triad found
        }

        /// <summary>
        /// Checks if a triangle is adjacent to any boundary nodes.
        /// Returns true if the triangle shares an edge with another triangle that contains boundary nodes.
        /// </summary>
        private bool IsAdjacentToBoundary(int triangleIndex, List<int[]> triangles, List<Node> nodes)
        {
            var tri = triangles[triangleIndex];

            // Check all other triangles to see if they share an edge with this one
            for (int i = 0; i < triangles.Count; i++)
            {
                if (i == triangleIndex) continue;

                var otherTri = triangles[i];

                // Count how many nodes are shared between the two triangles
                int sharedNodes = 0;
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        if (tri[j] == otherTri[k])
                        {
                            sharedNodes++;
                            break;
                        }
                    }
                }

                // If they share exactly 2 nodes, they share an edge
                if (sharedNodes == 2)
                {
                    // Check if the other triangle has any boundary nodes
                    for (int j = 0; j < 3; j++)
                    {
                        var nodeType = nodes[otherTri[j]].Type;
                        if (nodeType == NodeType.BoundaryPoint || 
                            nodeType == NodeType.BoundaryCorner)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds which edge index connects two local fiber indices.
        /// </summary>
        private static int FindEdgeConnectingFibers(int fiberIdx1, int fiberIdx2)
        {
            // Edges are defined as:
            // Edge 0: fibers 0-1
            // Edge 1: fibers 0-2
            // Edge 2: fibers 1-2

            var pair = new HashSet<int> { fiberIdx1, fiberIdx2 };

            if (pair.SetEquals(new[] { 0, 1 })) return 0;
            if (pair.SetEquals(new[] { 0, 2 })) return 1;
            if (pair.SetEquals(new[] { 1, 2 })) return 2;

            throw new InvalidOperationException($"Invalid fiber indices: {fiberIdx1}, {fiberIdx2}");
        }

        /// <summary>
        /// Swaps the diagonal between two triangles sharing an edge.
        /// </summary>
        private void SwapTriangleDiagonal(Triad triad1, Triad triad2, int overlapFiberIdx, List<int[]> triangles, List<Node> nodes)
        {
            // Get all unique fiber IDs from each triad
            var fibers1Set = new HashSet<int> { triad1.Edges[0, 0], triad1.Edges[0, 1], triad1.Edges[1, 1] };

            var fibers2Set = new HashSet<int> { triad2.Edges[0, 0], triad2.Edges[0, 1], triad2.Edges[1, 1] };

            // Find the fiber unique to each triad (not on the shared edge)
            int uniqueFiberInTriad1 = fibers1Set.Except(fibers2Set).First();
            int uniqueFiberInTriad2 = fibers2Set.Except(fibers1Set).First();

            // Get triangle connectivity arrays
            var tri1 = triangles[triad1.Number];
            var tri2 = triangles[triad2.Number];

            // Find which positions in each triangle to replace
            // We need to find nodes with uniqueFiberInTriad1 and uniqueFiberInTriad2
            // but they might be at wrong offsets currently, so find by FiberId first
            int posToReplaceInTri1 = -1;
            int posToReplaceInTri2 = -1;

            for (int i = 0; i < 3; i++)
            {
                if (nodes[tri1[i]].FiberId == uniqueFiberInTriad1)
                    posToReplaceInTri1 = i;
                if (nodes[tri2[i]].FiberId == uniqueFiberInTriad2)
                    posToReplaceInTri2 = i;
            }

            if (posToReplaceInTri1 == -1 || posToReplaceInTri2 == -1)
            {
                throw new InvalidOperationException("Could not find positions to swap in triangles");
            }

            // Get the node indices corresponding to the OTHER triad's unique fiber, matching offsets
            int nodeToSwapIntoTri1 = FindNodeIndexForFiberMatchingOffset(nodes, uniqueFiberInTriad2, tri1);
            int nodeToSwapIntoTri2 = FindNodeIndexForFiberMatchingOffset(nodes, uniqueFiberInTriad1, tri2);

            // Perform the swap
            Console.WriteLine($"    Before swap: tri1=[{tri1[0]},{tri1[1]},{tri1[2]}], tri2=[{tri2[0]},{tri2[1]},{tri2[2]}]");
            tri1[posToReplaceInTri1] = nodeToSwapIntoTri1;
            tri2[posToReplaceInTri2] = nodeToSwapIntoTri2;
            Console.WriteLine($"    After swap:  tri1=[{tri1[0]},{tri1[1]},{tri1[2]}], tri2=[{tri2[0]},{tri2[1]},{tri2[2]}]");

            // Update the triad's edge information after swap
            UpdateTriadAfterSwap(triad1, nodes, tri1);
            UpdateTriadAfterSwap(triad2, nodes, tri2);
        }

        /// <summary>
        /// Finds the node index for a given fiber ID that matches the offset of existing nodes in a triangle.
        /// For periodic boundaries, we need to find the node with matching offset.
        /// </summary>
        private int FindNodeIndexForFiberMatchingOffset(List<Node> nodes, int fiberId, int[] triangleConnectivity)
        {
            // Determine the offset from existing nodes in the triangle
            // (all nodes in a triangle should have the same offset for periodic consistency)
            (int ox, int oy) targetOffset = (0, 0);

            for (int i = 0; i < triangleConnectivity.Length; i++)
            {
                var node = nodes[triangleConnectivity[i]];
                if (node.FiberId.HasValue)
                {
                    targetOffset = node.Offset;
                    break;
                }
            }

            // Find node with matching FiberId AND Offset
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].FiberId == fiberId && nodes[i].Offset == targetOffset)
                    return i;
            }

            // Fallback: if no offset match found, return first match (shouldn't happen for periodic)
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].FiberId == fiberId)
                {
                    Console.WriteLine($"Warning: No offset match for fiber {fiberId}, using first occurrence");
                    return i;
                }
            }

            throw new InvalidOperationException($"Could not find node for fiber {fiberId}");
        }

        /// <summary>
        /// Updates a triad's internal state after connectivity changes.
        /// </summary>
        private void UpdateTriadAfterSwap(Triad triad, List<Node> nodes, int[] triangleConnectivity)
        {
            // Get the three fiber IDs in the new triangle
            var fiberIds = triangleConnectivity
                .Select(nodeIdx => nodes[nodeIdx].FiberId.Value)
                .ToArray();

            // Update the edges
            triad.SetEdgesWithFiberIndices(fiberIds[0], fiberIds[1], fiberIds[2]);
        }

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
    }
}
