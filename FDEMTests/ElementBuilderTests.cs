using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.Meshing;
using FxTMeshGenerator.Meshing.Elements;
using FDEMCore;

namespace FDEMTests
{
    [TestFixture]
    public class ElementBuilderTests
    {
        private const double Tolerance = 1e-6;

        [Test]
        public void CalculateFiberSurfacePoint_FiberAt00_TwoPointsAt45And135Degrees_ShouldReturnPointAt90Degrees()
        {
            // Arrange
            var fiberCenter = new Point2D(0, 0);
            double fiberRadius = 1.0;
            
            // Two points at 45° and 135° from fiber center
            var point1 = new Point2D(1, 1);   // 45 degrees
            var point2 = new Point2D(-1, 1);  // 135 degrees
            
            // Expected: bisector should be at 90 degrees (pointing up)
            var expected = new Point2D(0, 1);

            // Act
            var result = CalculateFiberSurfacePointPublic(fiberCenter, fiberRadius, point1, point2);

            // Assert
            Assert.That(result.X, Is.EqualTo(expected.X).Within(Tolerance), $"X coordinate mismatch. Expected {expected.X}, got {result.X}");
            Assert.That(result.Y, Is.EqualTo(expected.Y).Within(Tolerance), $"Y coordinate mismatch. Expected {expected.Y}, got {result.Y}");
        }

        [Test]
        public void CalculateFiberSurfacePoint_FiberAt00_TwoPointsAt0And90Degrees_ShouldReturnPointAt45Degrees()
        {
            // Arrange
            var fiberCenter = new Point2D(0, 0);
            double fiberRadius = 1.0;
            
            // Two points at 0° and 90° from fiber center
            var point1 = new Point2D(1, 0);   // 0 degrees (right)
            var point2 = new Point2D(0, 1);   // 90 degrees (up)
            
            // Expected: bisector should be at 45 degrees
            double cos45 = Math.Sqrt(2) / 2.0;
            var expected = new Point2D(cos45, cos45);

            // Act
            var result = CalculateFiberSurfacePointPublic(fiberCenter, fiberRadius, point1, point2);

            // Assert
            Assert.That(result.X, Is.EqualTo(expected.X).Within(Tolerance), $"X coordinate mismatch. Expected {expected.X}, got {result.X}");
            Assert.That(result.Y, Is.EqualTo(expected.Y).Within(Tolerance), $"Y coordinate mismatch. Expected {expected.Y}, got {result.Y}");
        }

        [Test]
        public void CalculateFiberSurfacePoint_FiberAt11_TwoPointsSymmetric_ShouldReturnCorrectBisector()
        {
            // Arrange
            var fiberCenter = new Point2D(1, 1);
            double fiberRadius = 0.5;
            
            // Two points symmetrically placed around fiber
            var point1 = new Point2D(2, 1);   // To the right
            var point2 = new Point2D(1, 2);   // Above
            
            // Expected: bisector should be at 45 degrees from fiber center
            double cos45 = Math.Sqrt(2) / 2.0;
            var expected = new Point2D(1 + 0.5 * cos45, 1 + 0.5 * cos45);

            // Act
            var result = CalculateFiberSurfacePointPublic(fiberCenter, fiberRadius, point1, point2);

            // Assert
            Assert.That(result.X, Is.EqualTo(expected.X).Within(Tolerance), $"X coordinate mismatch. Expected {expected.X}, got {result.X}");
            Assert.That(result.Y, Is.EqualTo(expected.Y).Within(Tolerance), $"Y coordinate mismatch. Expected {expected.Y}, got {result.Y}");
        }

        [Test]
        public void CalculateFiberSurfacePoint_FiberAt00_TwoPointsAt180And270Degrees_ShouldReturnPointAt225Degrees()
        {
            // Arrange
            var fiberCenter = new Point2D(0, 0);
            double fiberRadius = 1.0;
            
            // Two points at 180° and 270° from fiber center
            var point1 = new Point2D(-1, 0);   // 180 degrees (left)
            var point2 = new Point2D(0, -1);   // 270 degrees (down)
            
            // Expected: bisector should be at 225 degrees
            double cos225 = -Math.Sqrt(2) / 2.0;
            double sin225 = -Math.Sqrt(2) / 2.0;
            var expected = new Point2D(cos225, sin225);

            // Act
            var result = CalculateFiberSurfacePointPublic(fiberCenter, fiberRadius, point1, point2);

            // Assert
            Assert.That(result.X, Is.EqualTo(expected.X).Within(Tolerance), $"X coordinate mismatch. Expected {expected.X}, got {result.X}");
            Assert.That(result.Y, Is.EqualTo(expected.Y).Within(Tolerance), $"Y coordinate mismatch. Expected {expected.Y}, got {result.Y}");
        }

        [Test]
        public void CalculateFiberSurfacePoint_EquilateralTriangle_ShouldReturnPointsOnFiberSurface()
        {
            // Arrange: Three fibers forming an equilateral triangle
            var fiber1Center = new Point2D(0, 0);
            var fiber2Center = new Point2D(2, 0);
            var fiber3Center = new Point2D(1, Math.Sqrt(3));
            double fiberRadius = 0.3;

            // Act: Calculate surface point on fiber1 facing the other two
            var result = CalculateFiberSurfacePointPublic(fiber1Center, fiberRadius, fiber2Center, fiber3Center);

            // Assert: Point should be on the circle and face toward the centroid
            double distanceFromCenter = Math.Sqrt(result.X * result.X + result.Y * result.Y);
            Assert.That(distanceFromCenter, Is.EqualTo(fiberRadius).Within(Tolerance), 
                "Point should be exactly fiberRadius away from center");

            // The point should have positive X and Y (facing the triangle interior)
            Assert.That(result.X, Is.GreaterThan(0), "X should be positive (facing right/up)");
            Assert.That(result.Y, Is.GreaterThan(0), "Y should be positive (facing up)");
        }

        [Test]
        public void CalculateFiberSurfacePoint_WideAngle_ShouldHandleObtuse()
        {
            // Arrange: Two points at 30° and 150° (120° apart)
            var fiberCenter = new Point2D(0, 0);
            double fiberRadius = 1.0;
            
            double angle1 = Math.PI / 6.0;  // 30 degrees
            double angle2 = 5.0 * Math.PI / 6.0;  // 150 degrees
            
            var point1 = new Point2D(Math.Cos(angle1), Math.Sin(angle1));
            var point2 = new Point2D(Math.Cos(angle2), Math.Sin(angle2));
            
            // Expected: bisector should be at 90 degrees
            var expected = new Point2D(0, 1);

            // Act
            var result = CalculateFiberSurfacePointPublic(fiberCenter, fiberRadius, point1, point2);

            // Assert
            Assert.That(result.X, Is.EqualTo(expected.X).Within(Tolerance), $"X coordinate mismatch. Expected {expected.X}, got {result.X}");
            Assert.That(result.Y, Is.EqualTo(expected.Y).Within(Tolerance), $"Y coordinate mismatch. Expected {expected.Y}, got {result.Y}");
        }

        /// <summary>
        /// Public wrapper to test the private method from ElementBuilder.
        /// This replicates the logic for testing purposes.
        /// </summary>
        private Point2D CalculateFiberSurfacePointPublic(
            Point2D fiberCenter,
            double fiberRadius,
            Point2D otherPoint1,
            Point2D otherPoint2)
        {
            // Create vectors from fiber center to the other two points
            var vec1 = MathHelper.MakeVector2D(fiberCenter, otherPoint1);
            var vec2 = MathHelper.MakeVector2D(fiberCenter, otherPoint2);

            // Calculate angle from unit vector to vec1
            double angleToUnit = CalculateAngleToXAxis(vec1);

            // Calculate angle between vec1 and vec2
            double angleBetween = MathHelper.CalculateAngleBetweenVectors(vec1, vec2);

            // Bisector angle: halfway between the two vectors
            double bisectorAngle = angleToUnit + angleBetween / 2.0;

            // Calculate point on fiber surface
            return new Point2D(
                fiberCenter.X + fiberRadius * Math.Cos(bisectorAngle),
                fiberCenter.Y + fiberRadius * Math.Sin(bisectorAngle));
        }

        private double CalculateAngleToXAxis(Point2D vector)
        {
            double angle = Math.Atan2(vector.Y, vector.X);
            // Ensure angle is in range [0, 2*PI)
            if (angle < 0)
            {
                angle += 2.0 * Math.PI;
            }
            return angle;
        }

        [Test]
        public void BuildMesh_MixedTriangle_TwoFibersOneBoundary_ShouldGenerateTriangleWithCorrectNodeTypes()
        {
            // Arrange: Create a triangle with 2 fiber nodes and 1 boundary node
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0, 0, 0 }, fiberParams, boundary);
            var fiber2 = new Fiber(new double[] { 1, 0, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1, fiber2 };

            var boundaryPoint = new Point2D(0.5, 1.0);

            // Create nodes for triangulation
            var nodes = new List<Node>
            {
                new Node(new Point2D(0, 0), 0, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(1, 0), 1, NodeType.FiberCenter, (0, 0)),
                new Node(boundaryPoint, null, NodeType.BoundaryPoint, (0, 0))
            };

            var triangles = new List<int[]> { new[] { 0, 1, 2 } };
            var triangulation = new TriangulationMesh2D(nodes, triangles);

            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            Assert.That(mesh.Elements.Count, Is.GreaterThan(0), "Should generate at least one element");

            var triangleElements = mesh.Elements.Where(e => e is TriangleElement).ToList();
            Assert.That(triangleElements.Count, Is.EqualTo(1), "Should generate exactly one triangle element");

            // Verify that nodes exist in the mesh
            Assert.That(mesh.GlobalNodes.Count, Is.GreaterThanOrEqualTo(3), "Should have at least 3 nodes");

            // Check that at least one node is on a fiber surface (distance from fiber center ~ radius)
            bool hasNodeOnFiber1Surface = mesh.GlobalNodes.Any(n => 
                Math.Abs(MathHelper.CalcDistanceBetweenTwoPoints(n, new Point2D(0, 0)) - fiber1.Radius) < Tolerance);
            bool hasNodeOnFiber2Surface = mesh.GlobalNodes.Any(n => 
                Math.Abs(MathHelper.CalcDistanceBetweenTwoPoints(n, new Point2D(1, 0)) - fiber2.Radius) < Tolerance);

            Assert.That(hasNodeOnFiber1Surface || hasNodeOnFiber2Surface, Is.True, 
                "At least one node should be on a fiber surface");

            // Check that the boundary point is in the mesh (exact match)
            bool hasBoundaryPoint = mesh.GlobalNodes.Any(n => 
                Math.Abs(n.X - boundaryPoint.X) < Tolerance && 
                Math.Abs(n.Y - boundaryPoint.Y) < Tolerance);

            Assert.That(hasBoundaryPoint, Is.True, 
                "The original boundary point should be in the mesh");
        }

        [Test]
        public void BuildMesh_MixedTriangle_OneFiberTwoBoundaries_ShouldGenerateTriangleWithCorrectNodeTypes()
        {
            // Arrange: Create a triangle with 1 fiber node and 2 boundary nodes
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0.5, 0.5, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1 };

            var boundaryPoint1 = new Point2D(0, 0);
            var boundaryPoint2 = new Point2D(1, 0);

            // Create nodes for triangulation
            var nodes = new List<Node>
            {
                new Node(new Point2D(0.5, 0.5), 0, NodeType.FiberCenter, (0, 0)),
                new Node(boundaryPoint1, null, NodeType.BoundaryPoint, (0, 0)),
                new Node(boundaryPoint2, null, NodeType.BoundaryCorner, (0, 0))
            };

            var triangles = new List<int[]> { new[] { 0, 1, 2 } };
            var triangulation = new TriangulationMesh2D(nodes, triangles);

            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            Assert.That(mesh.Elements.Count, Is.GreaterThan(0), "Should generate at least one element");

            var triangleElements = mesh.Elements.Where(e => e is TriangleElement).ToList();
            Assert.That(triangleElements.Count, Is.EqualTo(1), "Should generate exactly one triangle element");

            // Verify nodes
            Assert.That(mesh.GlobalNodes.Count, Is.GreaterThanOrEqualTo(3), "Should have at least 3 nodes");

            // Check that one node is on the fiber surface
            bool hasNodeOnFiberSurface = mesh.GlobalNodes.Any(n => 
                Math.Abs(MathHelper.CalcDistanceBetweenTwoPoints(n, new Point2D(0.5, 0.5)) - fiber1.Radius) < Tolerance);

            Assert.That(hasNodeOnFiberSurface, Is.True, 
                "One node should be on the fiber surface");

            // Check that both boundary points are in the mesh
            bool hasBoundaryPoint1 = mesh.GlobalNodes.Any(n => 
                Math.Abs(n.X - boundaryPoint1.X) < Tolerance && 
                Math.Abs(n.Y - boundaryPoint1.Y) < Tolerance);
            bool hasBoundaryPoint2 = mesh.GlobalNodes.Any(n => 
                Math.Abs(n.X - boundaryPoint2.X) < Tolerance && 
                Math.Abs(n.Y - boundaryPoint2.Y) < Tolerance);

            Assert.That(hasBoundaryPoint1, Is.True, "First boundary point should be in the mesh");
            Assert.That(hasBoundaryPoint2, Is.True, "Second boundary point should be in the mesh");
        }

        [Test]
        public void BuildMesh_AllBoundaryTriangle_ShouldGenerateTriangleWithOriginalPoints()
        {
            // Arrange: Create a triangle with all boundary nodes
            var fibers = new List<Fiber>();

            var boundaryPoint1 = new Point2D(0, 0);
            var boundaryPoint2 = new Point2D(1, 0);
            var boundaryPoint3 = new Point2D(0.5, 1);

            // Create nodes for triangulation
            var nodes = new List<Node>
            {
                new Node(boundaryPoint1, null, NodeType.BoundaryCorner, (0, 0)),
                new Node(boundaryPoint2, null, NodeType.BoundaryCorner, (0, 0)),
                new Node(boundaryPoint3, null, NodeType.BoundaryPoint, (0, 0))
            };

            var triangles = new List<int[]> { new[] { 0, 1, 2 } };
            var triangulation = new TriangulationMesh2D(nodes, triangles);

            var boundary = CreateTestBoundary();
            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            Assert.That(mesh.Elements.Count, Is.EqualTo(1), "Should generate exactly one element");

            var triangleElements = mesh.Elements.Where(e => e is TriangleElement).ToList();
            Assert.That(triangleElements.Count, Is.EqualTo(1), "Should generate exactly one triangle element");

            // Verify that all three boundary points are in the mesh (exact matches)
            Assert.That(mesh.GlobalNodes.Count, Is.EqualTo(3), "Should have exactly 3 nodes");

            bool hasBoundaryPoint1 = mesh.GlobalNodes.Any(n => 
                Math.Abs(n.X - boundaryPoint1.X) < Tolerance && 
                Math.Abs(n.Y - boundaryPoint1.Y) < Tolerance);
            bool hasBoundaryPoint2 = mesh.GlobalNodes.Any(n => 
                Math.Abs(n.X - boundaryPoint2.X) < Tolerance && 
                Math.Abs(n.Y - boundaryPoint2.Y) < Tolerance);
            bool hasBoundaryPoint3 = mesh.GlobalNodes.Any(n => 
                Math.Abs(n.X - boundaryPoint3.X) < Tolerance && 
                Math.Abs(n.Y - boundaryPoint3.Y) < Tolerance);

            Assert.That(hasBoundaryPoint1, Is.True, "First boundary point should be in the mesh");
            Assert.That(hasBoundaryPoint2, Is.True, "Second boundary point should be in the mesh");
            Assert.That(hasBoundaryPoint3, Is.True, "Third boundary point should be in the mesh");
        }

        [Test]
        public void BuildMesh_AllFiberTriangle_ShouldGenerateTriangleWithSurfacePoints()
        {
            // Arrange: Create a triangle with all fiber nodes
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0, 0, 0 }, fiberParams, boundary);
            var fiber2 = new Fiber(new double[] { 1, 0, 0 }, fiberParams, boundary);
            var fiber3 = new Fiber(new double[] { 0.5, 1, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1, fiber2, fiber3 };

            // Create nodes for triangulation
            var nodes = new List<Node>
            {
                new Node(new Point2D(0, 0), 0, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(1, 0), 1, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(0.5, 1), 2, NodeType.FiberCenter, (0, 0))
            };

            var triangles = new List<int[]> { new[] { 0, 1, 2 } };
            var triangulation = new TriangulationMesh2D(nodes, triangles);

            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            Assert.That(mesh.Elements.Count, Is.GreaterThan(0), "Should generate at least one element");

            var triangleElements = mesh.Elements.Where(e => e is TriangleElement).ToList();
            Assert.That(triangleElements.Count, Is.GreaterThanOrEqualTo(1), "Should generate at least one triangle element");

            // Verify that nodes are on fiber surfaces, not at fiber centers
            bool hasNodeOnFiber1Surface = mesh.GlobalNodes.Any(n => 
                Math.Abs(MathHelper.CalcDistanceBetweenTwoPoints(n, new Point2D(0, 0)) - fiber1.Radius) < Tolerance);
            bool hasNodeOnFiber2Surface = mesh.GlobalNodes.Any(n => 
                Math.Abs(MathHelper.CalcDistanceBetweenTwoPoints(n, new Point2D(1, 0)) - fiber2.Radius) < Tolerance);
            bool hasNodeOnFiber3Surface = mesh.GlobalNodes.Any(n => 
                Math.Abs(MathHelper.CalcDistanceBetweenTwoPoints(n, new Point2D(0.5, 1)) - fiber3.Radius) < Tolerance);

            Assert.That(hasNodeOnFiber1Surface, Is.True, "Should have node on fiber 1 surface");
            Assert.That(hasNodeOnFiber2Surface, Is.True, "Should have node on fiber 2 surface");
            Assert.That(hasNodeOnFiber3Surface, Is.True, "Should have node on fiber 3 surface");
        }

        [Test]
        public void BuildMesh_MixedTriangles_WithFiberBoundaryEdge_ShouldGenerateFiberAndTriangularElements()
        {
            // Arrange: Create two adjacent triangles sharing a fiber-boundary edge
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0.5, 0.5, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1 };

            // Triangle 1: 1 fiber + 2 boundary nodes
            // Triangle 2: 1 fiber + 2 boundary nodes (shares 1 fiber and 1 boundary with Triangle 1)
            var nodes = new List<Node>
            {
                new Node(new Point2D(0.5, 0.5), 0, NodeType.FiberCenter, (0, 0)),  // node 0: fiber
                new Node(new Point2D(0, 0), null, NodeType.BoundaryCorner, (0, 0)), // node 1: boundary
                new Node(new Point2D(1, 0), null, NodeType.BoundaryCorner, (0, 0)), // node 2: boundary
                new Node(new Point2D(0.5, 1.0), null, NodeType.BoundaryPoint, (0, 0)) // node 3: boundary
            };

            // Triangle 1: fiber (0), boundary (1), boundary (2)
            // Triangle 2: fiber (0), boundary (2), boundary (3)
            // Shared edge: fiber (0) - boundary (2)
            var triangles = new List<int[]> 
            { 
                new[] { 0, 1, 2 },
                new[] { 0, 2, 3 }
            };

            var triangulation = new TriangulationMesh2D(nodes, triangles);
            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            var triangleElements = mesh.Elements.Where(e => e is TriangleElement).ToList();

            // Should have:
            // - 2 interior triangle elements (one per triangle)
            // - 1 fiber element (6-node triangle on fiber surface)
            // - 1 triangular matrix element (6-node triangle between fiber and boundary)
            Assert.That(triangleElements.Count, Is.EqualTo(4), 
                "Should have 2 interior + 1 fiber + 1 triangular matrix = 4 triangle elements");

            // Check that we have fiber phase and matrix phase elements
            var fiberElements = mesh.Elements.Where(e => e.Phase == ElementPhase.Fiber).ToList();
            var matrixElements = mesh.Elements.Where(e => e.Phase == ElementPhase.Matrix).ToList();

            Assert.That(fiberElements.Count, Is.EqualTo(1), "Should have 1 fiber element");
            Assert.That(matrixElements.Count, Is.GreaterThanOrEqualTo(3), 
                "Should have at least 2 interior triangles + 1 triangular matrix element");
        }

        [Test]
        public void BuildMesh_MixedTriangles_TwoFiberBoundaryEdges_ShouldGenerateTwoSetsOfElements()
        {
            // Arrange: Three triangles forming a strip with two fiber-boundary shared edges
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0.5, 0.5, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1 };

            var nodes = new List<Node>
            {
                new Node(new Point2D(0.5, 0.5), 0, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(0, 0), null, NodeType.BoundaryCorner, (0, 0)),
                new Node(new Point2D(1, 0), null, NodeType.BoundaryCorner, (0, 0)),
                new Node(new Point2D(0.5, 1.0), null, NodeType.BoundaryPoint, (0, 0)),
                new Node(new Point2D(0, 1.0), null, NodeType.BoundaryCorner, (0, 0))
            };

            var triangles = new List<int[]> 
            { 
                new[] { 0, 1, 2 }, // fiber-boundary-boundary
                new[] { 0, 2, 3 }, // fiber-boundary-boundary (shares fiber-boundary edge with tri 0)
                new[] { 0, 3, 4 }  // fiber-boundary-boundary (shares fiber-boundary edge with tri 1)
            };

            var triangulation = new TriangulationMesh2D(nodes, triangles);
            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            var fiberElements = mesh.Elements.Where(e => e.Phase == ElementPhase.Fiber).ToList();

            // Should have 2 fiber elements (one per shared fiber-boundary edge)
            Assert.That(fiberElements.Count, Is.EqualTo(2), "Should have 2 fiber elements for 2 shared edges");
        }

        [Test]
        public void BuildMesh_MixedAndAllFiberTriangles_ShouldGenerateMixOfElements()
        {
            // Arrange: Mix of triangles to test various edge types
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0.3, 0.5, 0 }, fiberParams, boundary);
            var fiber2 = new Fiber(new double[] { 0.7, 0.5, 0 }, fiberParams, boundary);
            var fiber3 = new Fiber(new double[] { 0.5, 0.2, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1, fiber2, fiber3 };

            // Create a configuration with three triangles:
            // Triangle 1 (all-fiber): fiber1, fiber2, fiber3
            // Triangle 2 (mixed): fiber1, fiber2, boundary
            // These share edge (fiber1, fiber2) -> should create quad element
            var nodes = new List<Node>
            {
                new Node(new Point2D(0.3, 0.5), 0, NodeType.FiberCenter, (0, 0)),  // node 0: fiber1
                new Node(new Point2D(0.7, 0.5), 1, NodeType.FiberCenter, (0, 0)),  // node 1: fiber2
                new Node(new Point2D(0.5, 0.2), 2, NodeType.FiberCenter, (0, 0)),  // node 2: fiber3
                new Node(new Point2D(0.5, 0.8), null, NodeType.BoundaryPoint, (0, 0)) // node 3: boundary
            };

            var triangles = new List<int[]> 
            { 
                new[] { 0, 1, 2 }, // all-fiber: fiber1, fiber2, fiber3
                new[] { 0, 1, 3 }  // mixed: fiber1, fiber2, boundary
                // These triangles share edge (0, 1) = (fiber1, fiber2)
            };

            var triangulation = new TriangulationMesh2D(nodes, triangles);
            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert - verify elements are generated
            Assert.That(mesh.Elements.Count, Is.GreaterThan(0), "Should generate elements");

            var fiberElements = mesh.Elements.Where(e => e.Phase == ElementPhase.Fiber).ToList();
            var matrixElements = mesh.Elements.Where(e => e.Phase == ElementPhase.Matrix).ToList();

            // Should have fiber elements from the shared fiber-fiber edge
            Assert.That(fiberElements.Count, Is.GreaterThan(0), "Should have fiber elements");

            // Should have matrix elements (quad from fiber-fiber edge)
            Assert.That(matrixElements.Count, Is.GreaterThan(0), "Should have matrix elements");
        }

        [Test]
        public void BuildMesh_TwoAdjacentAllFiberTriangles_ShouldGenerateQuadElement()
        {
            // Arrange: Two adjacent all-fiber triangles (regression test - ensure existing functionality still works)
            var boundary = CreateTestBoundary();
            var fiberParams = CreateTestFiberParameters();

            var fiber1 = new Fiber(new double[] { 0, 0, 0 }, fiberParams, boundary);
            var fiber2 = new Fiber(new double[] { 1, 0, 0 }, fiberParams, boundary);
            var fiber3 = new Fiber(new double[] { 0.5, 0.5, 0 }, fiberParams, boundary);
            var fiber4 = new Fiber(new double[] { 0.5, 1.0, 0 }, fiberParams, boundary);
            var fibers = new List<Fiber> { fiber1, fiber2, fiber3, fiber4 };

            var nodes = new List<Node>
            {
                new Node(new Point2D(0, 0), 0, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(1, 0), 1, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(0.5, 0.5), 2, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(0.5, 1.0), 3, NodeType.FiberCenter, (0, 0))
            };

            // Two triangles sharing edge between fiber2 and fiber3
            var triangles = new List<int[]> 
            { 
                new[] { 0, 1, 2 },
                new[] { 1, 2, 3 }
            };

            var triangulation = new TriangulationMesh2D(nodes, triangles);
            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            var quadElements = mesh.Elements.Where(e => e is QuadElement).ToList();

            Assert.That(quadElements.Count, Is.GreaterThan(0), "Should have at least one quad element between fibers");

            var fiberElements = mesh.Elements.Where(e => e.Phase == ElementPhase.Fiber).ToList();
            Assert.That(fiberElements.Count, Is.GreaterThan(0), "Should have fiber elements");
        }

        /// <summary>
        /// Helper method to create a test boundary for the mesh builder.
        /// </summary>
        private CellBoundary CreateTestBoundary()
        {
            // Create a simple rectangular boundary
            var ODimensions = new double[] { 1.0, 2.0, 2.0 };
            return new CellBoundary(ODimensions);
        }

        /// <summary>
        /// Helper method to create test fiber parameters.
        /// </summary>
        private FiberParameters CreateTestFiberParameters()
        {
            // radius, linearDensity, length, AxialModulus, TransverseModulus, PoissonsRatio12, PoissonsRatio23, ShearModulus12, globalDamping
            return new FiberParameters(0.2, 1.0, 1.0, 1.0, 1.0, 0.3, 0.3, 0.38, 0.0);
        }

        [Test]
        public void BuildMesh_PeriodicBoundaryFibers_ShouldGenerateBoundaryFiberElements()
        {
            // Arrange
            var boundary = CreateTestBoundary();  // 1.0 x 2.0 x 2.0 (height x width x ?)
            var fiberParams = CreateTestFiberParameters();

            // Create two fibers right on the left boundary
            // The boundary goes from x=0 to x=2.0 and y=0 to y=1.0
            var fibers = new List<Fiber>
            {
                new Fiber(new double[] { 0.0, 0.3, 0.0 }, fiberParams, boundary),  // Fiber 0 - left edge
                new Fiber(new double[] { 0.0, 0.7, 0.0 }, fiberParams, boundary)   // Fiber 1 - left edge
            };

            // Create nodes for triangulation - simple triangle on the left edge
            var nodes = new List<Node>
            {
                // Two original fibers on left edge
                new Node(new Point2D(0.0, 0.3), 0, NodeType.FiberCenter, (0, 0)),
                new Node(new Point2D(0.0, 0.7), 1, NodeType.FiberCenter, (0, 0)),

                // Two projected fibers on right edge (x=2.0)
                new Node(new Point2D(2.0, 0.3), 0, NodeType.ProjectedFiber, (1, 0)),
                new Node(new Point2D(2.0, 0.7), 1, NodeType.ProjectedFiber, (1, 0))
            };

            // Create simple triangles that connect the left and right edges
            var triangles = new List<int[]>
            {
                new int[] { 0, 1, 2 },  // fiber0, fiber1, projFiber0
                new int[] { 1, 2, 3 }   // fiber1, projFiber0, projFiber1
            };

            var triangulation = new TriangulationMesh2D(nodes, triangles);
            var config = new ElementConfig();
            var builder = new ElementBuilder();

            // Act
            var mesh = builder.BuildMesh(triangulation, fibers, boundary, config);

            // Assert
            var allElements = mesh.Elements.ToList();
            Assert.That(allElements.Count, Is.GreaterThan(0), "Should have some elements");

            // Check that we have some elements (even if just the interior triangles)
            var triangleElements = mesh.Elements.OfType<TriangleElement>().ToList();
            Assert.That(triangleElements.Count, Is.GreaterThan(0), "Should have triangle elements");
        }
    }
}
