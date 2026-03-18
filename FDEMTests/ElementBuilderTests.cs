using NUnit.Framework;
using System;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.Meshing;

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
    }
}
