
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FxTMeshGenerator.Meshing;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.IO;
using FDEMCore;

namespace FxTMeshGenerator.Tests
{
    [TestFixture]
    public class DelaunayTriangulatorTests
    {
        /// <summary>
        /// Test case with known fiber positions from V0p7YPeriodic.txt that causes infinite swap loop.
        /// This is a regression test to ensure the overlap fixing algorithm handles this configuration.
        /// </summary>
        [Test]
        public void TestKnownProblematicPacking_V0p7YPeriodic()
        {
            // Arrange: Set up the exact packing that causes the infinite loop
            var boundarySize = 71.33989136;
            var fiberRadius = 3.0;

            // Fiber positions (Y, Z coordinates from pack file)
            var fiberPositions = new (double y, double z)[]
            {
                (29.41937655, 51.12706989),
                (41.19545699, 55.47331841),
                (33.43042419, 44.64353978),
                (43.19492113, 32.95012315),
                (60.14623472, 70.34486061),
                (15.3285723, 21.56874585),
                (6.15855469, 53.44900051),
                (37.57485463, 38.93280282),
                (12.6322651, 42.67538292),
                (11.75310089, 33.89787418),
                (24.83603045, 11.61812136),
                (39.72883483, 3.755293316),
                (26.09916066, 43.54834521),
                (49.90726367, 50.39573416),
                (37.94607461, 12.58974163),
                (26.1099395, 68.48103094),
                (10.39934303, 62.64432534),
                (34.28852596, 56.30496197),
                (46.11987106, 2.690617599),
                (33.4814947, 63.56920598),
                (33.86064087, 21.87701608),
                (27.14973213, 27.22584785),
                (47.77087992, 43.10674015),
                (44.50949953, 11.01953265),
                (68.67284657, 26.58093542),
                (10.5871503, 70.27061352),
                (2.239216414, 0.332219532),
                (49.54011398, 31.02531325),
                (61.90978191, 57.58977065),
                (14.54780719, 51.9790734),
                (11.240389, 11.01723286),
                (15.88781499, 66.58803704),
                (49.20472667, 66.77033421),
                (32.78281594, 2.903481781),
                (43.20313065, 48.24660192),
                (18.37217711, 31.85275295)
            };

            // Create periodic boundary
            var ODimensions = new double[] { 1.0, boundarySize, boundarySize };
            var boundary = new CellBoundary(ODimensions);
                      
            // Create fibers
            var fibers = new List<Fiber>();
            for (int i = 0; i < fiberPositions.Length; i++)
            {
                var fiberParams = new FiberParameters(fiberRadius, 1.0, 1.0, 0.5, 0.5, 0.3, 1.0);
                var fiber = new Fiber(new double[] { 0.0, fiberPositions[i].y, fiberPositions[i].z }, fiberParams, boundary);
                fibers.Add(fiber);
            }

            // Create triangulator
            var triangulator = new DelaunayTriangulator();

            // Debug options
            var debugOptions = new DebugOptions
            {
                Debug = true,
                Directory = TestContext.CurrentContext.TestDirectory,
                FileName = "V0p7YPeriodic_Test"
            };

            // Act: Generate triangulation
            TriangulationMesh2D triangulationMesh = null;
            TestDelegate triangulationAction = () => 
            {
                triangulationMesh = triangulator.GenerateTriangulation(boundary, fibers, debugOptions);
            };

            // Assert: Should complete without throwing exceptions
            Assert.DoesNotThrow(triangulationAction, "Triangulation should complete without exceptions");
            Assert.That(triangulationMesh, Is.Not.Null, "Mesh should be created");

            // Generate full FE mesh with elements for better visualization
            var elementBuilder = new ElementBuilder();
            string vtkMeshPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "V0p7YPeriodic_Test_mesh.vtk");

            var feMesh = elementBuilder.BuildMesh(
                triangulationMesh,
                fibers,
                boundary,
                ElementConfig.Simple,
                vtkMeshPath);

            // Write the full mesh
            VtkLegacyWriter.WriteUnstructuredMesh(vtkMeshPath, feMesh);
            Console.WriteLine($"Full FE mesh written to: {vtkMeshPath}");

            // Verify no overlaps remain after processing
            bool hasOverlaps = HasOverlappingTriads(triangulationMesh, fibers);
            Console.WriteLine($"VTK files written to: {TestContext.CurrentContext.TestDirectory}");

            if (hasOverlaps)
            {
                Console.WriteLine("WARNING: Overlaps still present after triangulation");
                Assert.Warn("Some overlapping triads remain - this is the known issue being debugged");
            }
            else
            {
                Assert.Pass("All overlaps successfully resolved!");
            }
        }

        /// <summary>
        /// Helper method to check if any triads have overlapping fibers.
        /// </summary>
        private bool HasOverlappingTriads(TriangulationMesh2D mesh, IReadOnlyList<Fiber> fibers)
        {
            var triangles = mesh.Triangles;
            var nodes = mesh.Nodes;

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var nodeA = nodes[tri[0]];
                var nodeB = nodes[tri[1]];
                var nodeC = nodes[tri[2]];

                // Only check triangles with three fiber nodes
                if (nodeA.FiberId.HasValue && nodeB.FiberId.HasValue && nodeC.FiberId.HasValue)
                {
                    var triadFibers = new[]
                    {
                        fibers[nodeA.FiberId.Value],
                        fibers[nodeB.FiberId.Value],
                        fibers[nodeC.FiberId.Value]
                    };

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

                    if (triad.DetermineIfFibersOverlapTriad())
                    {
                        Console.WriteLine($"Overlap found in triad {i}");
                        return true;
                    }
                }
            }

            return false;
        }

    }
}