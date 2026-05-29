using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FxTMeshGenerator.Meshing;
using FxTMeshGenerator.Geometry;
using FDEMCore;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace FxTMeshGenerator.Tests
{
    [TestFixture]
    public class EdgeSwapValidationTests
    {
        /// <summary>
        /// Test that verifies the four vertices of a quad stay local to the two triangles after swap.
        /// This is the core invariant: only the diagonal should change.
        /// </summary>
        [Test]
        public void TestEdgeSwap_FourVerticesStayLocal()
        {
            // Arrange: Create a simple quad configuration through triangulation
            var fiberRadius = 1.0;
            var boundarySize = 10.0;
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);

            // Create a simple quad: 4 fibers in a square
            var fiberPositions = new (double y, double z)[]
            {
                (3, 3),  // 0
                (7, 3),  // 1
                (7, 7),  // 2
                (3, 7)   // 3
            };

            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Length; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            // Create initial triangulation (will create nodes internally)
            var triangulator = new DelaunayTriangulator();
            var mesh = triangulator.GenerateTriangulation(boundary, fibers);

            // Find two adjacent triangles
            int tri1Idx = -1, tri2Idx = -1;
            int[] sharedEdge = null;

            for (int i = 0; i < mesh.Triangles.Count - 1; i++)
            {
                for (int j = i + 1; j < mesh.Triangles.Count; j++)
                {
                    var tri1 = mesh.Triangles[i];
                    var tri2 = mesh.Triangles[j];

                    // Find shared edge
                    var shared = tri1.Intersect(tri2).ToArray();
                    if (shared.Length == 2)
                    {
                        // Check if both triangles have only fiber nodes
                        var nodes1 = tri1.Select(idx => mesh.Nodes[idx]).ToArray();
                        var nodes2 = tri2.Select(idx => mesh.Nodes[idx]).ToArray();

                        if (nodes1.All(n => n.FiberId.HasValue) && nodes2.All(n => n.FiberId.HasValue))
                        {
                            tri1Idx = i;
                            tri2Idx = j;
                            sharedEdge = shared;
                            break;
                        }
                    }
                }
                if (tri1Idx >= 0) break;
            }

            if (tri1Idx < 0)
            {
                Assert.Warn("Could not find adjacent fiber triangles");
                return;
            }

            // Convert mesh.Triangles to flat int[] array for PerformEdgeSwap
            var triangles = new int[mesh.Triangles.Count * 3];
            for (int i = 0; i < mesh.Triangles.Count; i++)
            {
                triangles[i * 3] = mesh.Triangles[i][0];
                triangles[i * 3 + 1] = mesh.Triangles[i][1];
                triangles[i * 3 + 2] = mesh.Triangles[i][2];
            }

            var oldTri1 = new[] { triangles[tri1Idx * 3], triangles[tri1Idx * 3 + 1], triangles[tri1Idx * 3 + 2] };
            var oldTri2 = new[] { triangles[tri2Idx * 3], triangles[tri2Idx * 3 + 1], triangles[tri2Idx * 3 + 2] };

            // Collect the four unique vertices before swap
            var quadVerticesBefore = new HashSet<int>(oldTri1.Concat(oldTri2));
            Assert.That(quadVerticesBefore.Count, Is.EqualTo(4), "Quad should have exactly 4 unique vertices");

            // Act: Perform edge swap using reflection
            var performSwapMethod = typeof(DelaunayTriangulator).GetMethod(
                "PerformEdgeSwap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            performSwapMethod.Invoke(triangulator, new object[] { tri1Idx, tri2Idx, sharedEdge, triangles, mesh.Nodes });

            var newTri1 = new[] { triangles[tri1Idx * 3], triangles[tri1Idx * 3 + 1], triangles[tri1Idx * 3 + 2] };
            var newTri2 = new[] { triangles[tri2Idx * 3], triangles[tri2Idx * 3 + 1], triangles[tri2Idx * 3 + 2] };

            // Assert: The four vertices should be the same, just redistributed
            var quadVerticesAfter = new HashSet<int>(newTri1.Concat(newTri2));

            Console.WriteLine($"Before swap:");
            Console.WriteLine($"  Tri1: [{string.Join(", ", oldTri1)}]");
            Console.WriteLine($"  Tri2: [{string.Join(", ", oldTri2)}]");
            Console.WriteLine($"  Quad vertices: [{string.Join(", ", quadVerticesBefore.OrderBy(x => x))}]");
            Console.WriteLine($"After swap:");
            Console.WriteLine($"  Tri1: [{string.Join(", ", newTri1)}]");
            Console.WriteLine($"  Tri2: [{string.Join(", ", newTri2)}]");
            Console.WriteLine($"  Quad vertices: [{string.Join(", ", quadVerticesAfter.OrderBy(x => x))}]");

            Assert.That(quadVerticesAfter, Is.EquivalentTo(quadVerticesBefore),
                "The four quad vertices should remain the same after swap, just redistributed between the two triangles");

            // Additionally check that the shared edge changed
            var newSharedEdge = newTri1.Intersect(newTri2).OrderBy(x => x).ToArray();
            Assert.That(newSharedEdge.Length, Is.EqualTo(2), "Triangles should share exactly 2 vertices (an edge)");
            Assert.That(newSharedEdge, Is.Not.EquivalentTo(sharedEdge.OrderBy(x => x).ToArray()),
                "The shared edge should have changed from the old diagonal");
        }

        /// <summary>
        /// Test that validates edge swaps preserve mesh topology and orientation.
        /// This test performs triangulation optimization and validates every swap.
        /// </summary>
        [Test]
        public void TestEdgeSwapValidation_AllSwapsPreserveTopology()
        {
            // Arrange: Create a configuration that will trigger multiple swaps
            var fiberRadius = 1.0;
            var boundarySize = 20.0;
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);

            // Create a grid of fibers that will need optimization
            var fiberPositions = new List<(double y, double z)>();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    fiberPositions.Add((5 + i * 3.5, 5 + j * 3.5));
                }
            }

            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Count; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            var debugOptions = new DebugOptions
            {
                Debug = true,
                Directory = TestContext.CurrentContext.TestDirectory,
                FileName = "EdgeSwapValidation_Test"
            };

            // Act: Generate triangulation with optimization
            var triangulationMesh = new DelaunayTriangulator().GenerateTriangulation(boundary, fibers, debugOptions);

            // Assert: Validate the final mesh
            var nodes = triangulationMesh.Nodes.ToList();
            var trianglesList = triangulationMesh.Triangles.ToList();

            var validationErrors = ValidateMeshTopology(nodes, trianglesList);

            if (validationErrors.Any())
            {
                Console.WriteLine("Mesh validation errors found:");
                foreach (var error in validationErrors)
                {
                    Console.WriteLine($"  ❌ {error}");
                }
            }

            Assert.That(validationErrors, Is.Empty, "Mesh should have no topology errors after optimization");
        }

        /// <summary>
        /// Test that edge swaps correctly swap the diagonal in a known quadrilateral.
        /// </summary>
        [Test]
        public void TestEdgeSwap_DiagonalIsCorrectlySwapped()
        {
            // Arrange: Create a simple square of 4 fibers
            var fiberRadius = 0.5;
            var boundarySize = 10.0;
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);

            var fiberPositions = new[]
            {
                (y: 4.0, z: 4.0),  // 0: Bottom-left
                (y: 6.0, z: 4.0),  // 1: Bottom-right
                (y: 6.0, z: 6.0),  // 2: Top-right
                (y: 4.0, z: 6.0)   // 3: Top-left
            };

            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Length; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            var triangulationMesh = new DelaunayTriangulator().GenerateTriangulation(boundary, fibers, null);

            // Find two adjacent triangles sharing an edge
            var nodes = triangulationMesh.Nodes.ToList();
            var trianglesList = triangulationMesh.Triangles.ToList();

            // Convert to flat array for easier manipulation
            var triangles = new int[trianglesList.Count * 3];
            for (int i = 0; i < trianglesList.Count; i++)
            {
                triangles[i * 3] = trianglesList[i][0];
                triangles[i * 3 + 1] = trianglesList[i][1];
                triangles[i * 3 + 2] = trianglesList[i][2];
            }

            // Find adjacent fiber triangles
            (int tri1Idx, int tri2Idx, int[] sharedEdge, int[] oldTri1, int[] oldTri2) = FindAdjacentFiberTriangles(trianglesList, nodes);

            if (tri1Idx < 0)
            {
                Assert.Warn("Could not find adjacent fiber triangles");
                return;
            }

            // Act: Simulate edge swap using reflection
            var triangulator = new DelaunayTriangulator();
            var performSwapMethod = typeof(DelaunayTriangulator).GetMethod(
                "PerformEdgeSwap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            performSwapMethod.Invoke(triangulator, new object[] { tri1Idx, tri2Idx, sharedEdge, triangles, nodes });

            // Get new triangles after swap
            var newTri1 = new[] { triangles[tri1Idx * 3], triangles[tri1Idx * 3 + 1], triangles[tri1Idx * 3 + 2] };
            var newTri2 = new[] { triangles[tri2Idx * 3], triangles[tri2Idx * 3 + 1], triangles[tri2Idx * 3 + 2] };

            // Assert: Validate the swap
            var errors = ValidateEdgeSwap(oldTri1, oldTri2, newTri1, newTri2, sharedEdge, nodes);

            if (errors.Any())
            {
                Console.WriteLine("Edge swap validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  ❌ {error}");
                }

                // Print detailed swap information
                Console.WriteLine($"\nSwap Details:");
                Console.WriteLine($"  Old tri1: [{string.Join(", ", oldTri1)}]");
                Console.WriteLine($"  Old tri2: [{string.Join(", ", oldTri2)}]");
                Console.WriteLine($"  Shared edge: [{string.Join(", ", sharedEdge)}]");
                Console.WriteLine($"  New tri1: [{string.Join(", ", newTri1)}]");
                Console.WriteLine($"  New tri2: [{string.Join(", ", newTri2)}]");
            }

            Assert.That(errors, Is.Empty, "Edge swap should preserve topology and orientation");
        }

        /// <summary>
        /// Test that mesh connectivity remains valid after multiple swaps.
        /// </summary>
        [Test]
        public void TestMultipleSwaps_ConnectivityRemainsValid()
        {
            // Arrange: Dense packing that will trigger many swaps
            var fiberRadius = 0.8;
            var boundarySize = 15.0;
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);

            var fiberPositions = new List<(double y, double z)>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    fiberPositions.Add((3 + i * 2.5, 3 + j * 2.5));
                }
            }

            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Count; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            var debugOptions = new DebugOptions
            {
                Debug = true,
                Directory = TestContext.CurrentContext.TestDirectory,
                FileName = "MultiSwap_Test"
            };

            // Act
            var triangulationMesh = new DelaunayTriangulator().GenerateTriangulation(boundary, fibers, debugOptions);

            // Assert: Check for specific connectivity issues
            var nodes = triangulationMesh.Nodes.ToList();
            var trianglesList = triangulationMesh.Triangles.ToList();

            Console.WriteLine($"Validating mesh with {trianglesList.Count} triangles...");

            // 1. No duplicate triangles
            var duplicateTriangles = FindDuplicateTriangles(trianglesList);
            if (duplicateTriangles.Any())
            {
                Console.WriteLine($"Found {duplicateTriangles.Count} duplicate triangles:");
                duplicateTriangles.ForEach(e => Console.WriteLine($"  {e}"));
            }
            Assert.That(duplicateTriangles, Is.Empty, "Should not have duplicate triangles");

            // 2. All edges have at most 2 adjacent triangles
            var invalidEdges = FindInvalidEdges(trianglesList);
            if (invalidEdges.Any())
            {
                Console.WriteLine($"Found {invalidEdges.Count} invalid edges:");
                invalidEdges.ForEach(e => Console.WriteLine($"  {e}"));
            }
            Assert.That(invalidEdges, Is.Empty, "All edges should have at most 2 adjacent triangles");

            // 4. No degenerate triangles
            var degenerateTriangles = FindDegenerateTriangles(trianglesList, nodes);
            if (degenerateTriangles.Any())
            {
                Console.WriteLine($"Found {degenerateTriangles.Count} degenerate triangles:");
                degenerateTriangles.ForEach(e => Console.WriteLine($"  {e}"));
            }
            Assert.That(degenerateTriangles, Is.Empty, "Should not have degenerate triangles");

            Console.WriteLine($"✓ All validation checks passed!");
        }

        [Test]
        public void TestTriangleQuality()
        {
            // make all of the fibers first so we can use them in the quality evaluation
            var fiberRadius = 3.0;
            var boundarySize = 40.0;
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);

            var fiberPositions = new List<(double y, double z)>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    fiberPositions.Add((3 + i * 2.5, 3 + j * 2.5));
                }
            }

            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Count; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            // Populate with the 4 unique nodes of the quad
            List<Node> nodes = new List<Node>() {
                new Node(new Point2D(28.96111299,6.525019829), 0, NodeType.FiberCenter,(0,0)),
                new Node(new Point2D(27.53266495,33.55938244), 0, NodeType.FiberCenter,(0,0)),
                new Node(new Point2D(31.58765642,23.96879123), 0, NodeType.FiberCenter,(0,0)),
                new Node(new Point2D(25.96610501,27.22389207), 0, NodeType.FiberCenter,(0,0))
            };

            int[] triangles = new int[] { 0, 3, 2, 3, 1, 2 };

            //reflection to access private method for testing
            var triangulator = new DelaunayTriangulator();
            var evaluateQuadrilateralQualityMethod = typeof(DelaunayTriangulator).GetMethod(
                "EvaluateQuadrilateralQuality",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var parameters = new object[] { 0, 1, nodes, triangles, fibers, null, new int[] {3,2}};

            var currentQualityObj = evaluateQuadrilateralQualityMethod.Invoke(triangulator, parameters);
            var currentQuality = ((int inversions, double worstQualityRatio))currentQualityObj;

            var evaluateSwappedQuadrilateralQualityMethod = typeof(DelaunayTriangulator).GetMethod(
                "EvaluateSwappedQuadrilateralQuality",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var parametersSwapped = new object[] { 0, 1, parameters[5], new int[] { 0, 1 }, nodes, triangles, fibers };

            var swappedQualityObj = evaluateSwappedQuadrilateralQualityMethod.Invoke(triangulator, parametersSwapped);
            var swappedQuality = ((int inversions, double worstQualityRatio))swappedQualityObj;


            //assert that the quality of the swapped configuration is better than the original
            Assert.That(swappedQuality.inversions, Is.GreaterThan(currentQuality.inversions), 
                "original configuration should have better quality than swapped");
        }

        [Test]
        public void TestConcaveQuadrilateral()
        {
            // Create a quadrilateral with a concave side that would cause overlapping triangles if swapped
            // Configuration:
            //    Node 3 (10, 10)
            //    /\
            //   /  \
            //  /    \
            // 0------2
            // (3,3)  (17,3)
            //   \  /
            //    \/
            //  Node 1 (10, 3.5) <- slightly above line 0-2, making edge 0-1-2 concave
            //
            // Current triangles: [0,1,3] and [1,2,3] sharing edge [1,3]
            // If swapped to [0,1,2] and [0,2,3], the new diagonal 0-2 would be at y=3
            // Both nodes 1(y=3.5) and 3(y=10) are ABOVE this line (same side)
            // This means triangles would overlap!

            var fiberRadius = 3.0;
            var boundarySize = 40.0;
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);

            var fiberPositions = new List<(double y, double z)>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    fiberPositions.Add((3 + i * 2.5, 3 + j * 2.5));
                }
            }

            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Count; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            // Populate with the 4 unique nodes of the quad
            List<Node> nodes = new List<Node>() {
                new Node(new Point2D(3,3), 0, NodeType.FiberCenter,(0,0)),      // Node 0
                new Node(new Point2D(10,3.5), 0, NodeType.FiberCenter,(0,0)),   // Node 1 (concave point)
                new Node(new Point2D(17,3), 0, NodeType.FiberCenter,(0,0)),     // Node 2
                new Node(new Point2D(10,10), 0, NodeType.FiberCenter,(0,0))     // Node 3
            };

            int[] triangles = new int[] { 0, 1, 3, 1, 2, 3 };
            int[] trianglesBeforeSwap = (int[])triangles.Clone();

            // Try edge swap using reflection
            var triangulator = new DelaunayTriangulator();
            var swapMethod = typeof(DelaunayTriangulator).GetMethod(
                "PerformEdgeSwap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var swappingParams = new object[] { 0, 1, new int[] { 1, 3 }, triangles, nodes };

            Console.WriteLine("Before swap attempt:");
            Console.WriteLine($"  Triangle 0: [{triangles[0]}, {triangles[1]}, {triangles[2]}]");
            Console.WriteLine($"  Triangle 1: [{triangles[3]}, {triangles[4]}, {triangles[5]}]");
            Console.WriteLine($"  Shared edge: [1, 3]");
            Console.WriteLine($"  New diagonal would be: [0, 2]");
            Console.WriteLine($"  Node 1 (10, 3.5) and Node 3 (10, 10) are both ABOVE line 0-2 (y=3)");
            Console.WriteLine($"  This is a concave configuration - swap should be prevented!");

            swapMethod.Invoke(triangulator, swappingParams);

            Console.WriteLine("\nAfter swap attempt:");
            Console.WriteLine($"  Triangle 0: [{triangles[0]}, {triangles[1]}, {triangles[2]}]");
            Console.WriteLine($"  Triangle 1: [{triangles[3]}, {triangles[4]}, {triangles[5]}]");

            // Assert: The swap should NOT have occurred (triangles should be unchanged)
            Assert.That(triangles[0], Is.EqualTo(trianglesBeforeSwap[0]), "Triangle 0 node 0 should be unchanged");
            Assert.That(triangles[1], Is.EqualTo(trianglesBeforeSwap[1]), "Triangle 0 node 1 should be unchanged");
            Assert.That(triangles[2], Is.EqualTo(trianglesBeforeSwap[2]), "Triangle 0 node 2 should be unchanged");
            Assert.That(triangles[3], Is.EqualTo(trianglesBeforeSwap[3]), "Triangle 1 node 0 should be unchanged");
            Assert.That(triangles[4], Is.EqualTo(trianglesBeforeSwap[4]), "Triangle 1 node 1 should be unchanged");
            Assert.That(triangles[5], Is.EqualTo(trianglesBeforeSwap[5]), "Triangle 1 node 2 should be unchanged");

            Console.WriteLine("\n✓ Swap was correctly prevented - triangles remain unchanged!");
        }

        #region Helper Methods

        private List<string> ValidateMeshTopology(List<Node> nodes, List<int[]> triangles)
        {
            var errors = new List<string>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];

                // Check for out-of-bounds indices
                foreach (var nodeIdx in tri)
                {
                    if (nodeIdx < 0 || nodeIdx >= nodes.Count)
                    {
                        errors.Add($"Triangle {i}: Invalid node index {nodeIdx} (valid range: 0-{nodes.Count - 1})");
                    }
                }

                // Check for duplicate nodes within a triangle
                if (tri[0] == tri[1] || tri[1] == tri[2] || tri[0] == tri[2])
                {
                    errors.Add($"Triangle {i}: Duplicate nodes [{string.Join(",", tri)}]");
                }

                // Check for degenerate triangles (zero area)
                if (tri.All(idx => idx >= 0 && idx < nodes.Count))
                {
                    var p0 = nodes[tri[0]].P;
                    var p1 = nodes[tri[1]].P;
                    var p2 = nodes[tri[2]].P;

                    double area = Math.Abs((p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X));
                    if (area < 1e-10)
                    {
                        errors.Add($"Triangle {i}: Degenerate (area={area:E3}), nodes [{string.Join(",", tri)}]");
                    }
                }
            }

            return errors;
        }

        private List<string> ValidateEdgeSwap(int[] oldTri1, int[] oldTri2,int[] newTri1, int[] newTri2,
            int[] sharedEdge, List<Node> nodes)
        {
            var errors = new List<string>();

            // 1. Check that the four unique nodes are preserved
            var oldQuad = oldTri1.Concat(oldTri2).Distinct().OrderBy(x => x).ToArray();
            var newQuad = newTri1.Concat(newTri2).Distinct().OrderBy(x => x).ToArray();

            if (!oldQuad.SequenceEqual(newQuad))
            {
                errors.Add($"Quad vertices changed! Old: [{string.Join(",", oldQuad)}] New: [{string.Join(",", newQuad)}]");
            }

            // 2. Check that the old shared edge is no longer shared
            var oldSharedSet = new HashSet<int>(sharedEdge);
            int oldSharedInNew = newTri1.Intersect(newTri2).Count(x => oldSharedSet.Contains(x));
            if (oldSharedInNew == 2)
            {
                errors.Add($"Old shared edge [{sharedEdge[0]},{sharedEdge[1]}] is still shared after swap!");
            }

            // 3. Check that exactly one new edge is now shared
            var newShared = newTri1.Intersect(newTri2).ToArray();
            if (newShared.Length != 2)
            {
                errors.Add($"After swap, triangles should share exactly 2 nodes, but found {newShared.Length}");
            }
            else
            {
                var uniqueToTri1 = oldTri1.Except(sharedEdge).First();
                var uniqueToTri2 = oldTri2.Except(sharedEdge).First();
                var expectedNewDiagonal = new[] { uniqueToTri1, uniqueToTri2 }.OrderBy(x => x).ToArray();
                var actualNewDiagonal = newShared.OrderBy(x => x).ToArray();

                if (!expectedNewDiagonal.SequenceEqual(actualNewDiagonal))
                {
                    errors.Add($"New diagonal incorrect! Expected: [{string.Join(",", expectedNewDiagonal)}] Got: [{string.Join(",", actualNewDiagonal)}]");
                }
            }

            // 4. Check triangle orientations
            foreach (var (tri, name) in new[] { (newTri1, "tri1"), (newTri2, "tri2") })
            {
                var p0 = nodes[tri[0]].P;
                var p1 = nodes[tri[1]].P;
                var p2 = nodes[tri[2]].P;

                double orient = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
                if (orient <= 0)
                {
                    errors.Add($"{name} has invalid orientation: {orient:F6}");
                }
            }

            return errors;
        }

        private (int tri1Idx, int tri2Idx, int[] sharedEdge, int[] oldTri1, int[] oldTri2) FindAdjacentFiberTriangles(
            List<int[]> triangles, List<Node> nodes)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                var tri1 = triangles[i];
                if (!tri1.All(idx => nodes[idx].FiberId.HasValue))
                    continue;

                for (int j = i + 1; j < triangles.Count; j++)
                {
                    var tri2 = triangles[j];
                    if (!tri2.All(idx => nodes[idx].FiberId.HasValue))
                        continue;

                    var shared = tri1.Intersect(tri2).ToArray();
                    if (shared.Length == 2)
                    {
                        return (i, j, shared, tri1, tri2);
                    }
                }
            }

            return (-1, -1, null, null, null);
        }

        private List<string> FindDuplicateTriangles(List<int[]> triangles)
        {
            var errors = new List<string>();
            var seen = new HashSet<string>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var key = string.Join(",", triangles[i].OrderBy(x => x));
                if (!seen.Add(key))
                {
                    errors.Add($"Duplicate triangle at index {i}: [{string.Join(",", triangles[i])}]");
                }
            }

            return errors;
        }

        private List<string> FindWrongOrientationTriangles(List<int[]> triangles, List<Node> nodes)
        {
            var errors = new List<string>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var p0 = nodes[tri[0]].P;
                var p1 = nodes[tri[1]].P;
                var p2 = nodes[tri[2]].P;

                double orient = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
                if (orient <= 0)
                {
                    errors.Add($"Triangle {i} has wrong orientation: {orient:F6}");
                }
            }

            return errors;
        }

        private List<string> FindInvalidEdges(List<int[]> triangles)
        {
            var errors = new List<string>();
            var edgeCount = new Dictionary<string, int>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var edges = new[]
                {
                    string.Join(",", new[] { Math.Min(tri[0], tri[1]), Math.Max(tri[0], tri[1]) }),
                    string.Join(",", new[] { Math.Min(tri[1], tri[2]), Math.Max(tri[1], tri[2]) }),
                    string.Join(",", new[] { Math.Min(tri[2], tri[0]), Math.Max(tri[2], tri[0]) })
                };

                foreach (var edge in edges)
                {
                    edgeCount[edge] = edgeCount.GetValueOrDefault(edge, 0) + 1;
                }
            }

            foreach (var kvp in edgeCount.Where(x => x.Value > 2))
            {
                errors.Add($"Edge [{kvp.Key}] is shared by {kvp.Value} triangles (should be ≤ 2)");
            }

            return errors;
        }

        private List<string> FindDegenerateTriangles(List<int[]> triangles, List<Node> nodes)
        {
            var errors = new List<string>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var p0 = nodes[tri[0]].P;
                var p1 = nodes[tri[1]].P;
                var p2 = nodes[tri[2]].P;

                double area = Math.Abs((p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X));
                if (area < 1e-10)
                {
                    errors.Add($"Triangle {i} is degenerate (area={area:E3})");
                }
            }

            return errors;
        }

        
        #endregion
    }
}
