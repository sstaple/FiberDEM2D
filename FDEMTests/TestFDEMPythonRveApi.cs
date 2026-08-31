using System;
using System.Linq;
using NUnit.Framework;
using FDEMCore;
using FDEMPython;

namespace FDEMTests
{
    /// <summary>
    /// Tests for the FDEMPython interoperability layer and the underlying FDEMCore
    /// RandomRVEGenerationService that it wraps.
    /// </summary>
    public class TestFDEMPythonRveApi
    {
        private static RandomRVEGenerationOptions MakeBasicOptions()
        {
            return new RandomRVEGenerationOptions
            {
                FiberRadius = 1.0,
                FiberVolumeFraction = 0.4,
                NRows = 3, // -> 9 fibers
                MinSpacingBetweenFibers = 0.05,
                NMaxSteps = 300,
                NUndampedSteps = 50,
            };
        }

        [Test]
        public void GenerateRandomRVE_ReturnsConsistentNonEmptyResult()
        {
            var options = MakeBasicOptions();

            RandomRVEGenerationResult result = RveApi.GenerateRandomRVE(options);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.FiberRadii.Length, Is.GreaterThan(0));
            Assert.That(result.FiberLocations.GetLength(0), Is.EqualTo(result.FiberRadii.Length));
            Assert.That(result.FiberLocations.GetLength(1), Is.EqualTo(2));

            foreach (double radius in result.FiberRadii)
            {
                Assert.That(radius, Is.GreaterThan(0));
            }

            Assert.That(result.BoundaryDimensions.Length, Is.EqualTo(2));
            double width = result.BoundaryDimensions[0];
            double height = result.BoundaryDimensions[1];
            Assert.That(width, Is.GreaterThan(0));
            Assert.That(height, Is.GreaterThan(0));

            // Every fiber center should fall within [0, width] x [0, height] (bottom-left corner origin),
            // per the documented coordinate convention (small tolerance for margin/relaxation).
            for (int i = 0; i < result.FiberRadii.Length; i++)
            {
                double y = result.FiberLocations[i, 0];
                double z = result.FiberLocations[i, 1];
                Assert.That(y, Is.InRange(-0.5, width + 0.5));
                Assert.That(z, Is.InRange(-0.5, height + 0.5));
            }
        }

        [Test]
        public void GenerateRandomRVE_MatchesDirectFDEMCoreService()
        {
            // The FDEMPython API must be a thin pass-through to FDEMCore's common generation API.
            var optionsA = MakeBasicOptions();
            var optionsB = MakeBasicOptions();

            RandomRVEGenerationResult viaPython = RveApi.GenerateRandomRVE(optionsA);
            RandomRVEGenerationResult viaCore = RandomRVEGenerationService.Generate(optionsB);

            // Randomness is not seeded in the underlying algorithm, so we cannot compare exact fiber
            // positions between two independent runs. Instead we verify both pathways produce
            // consistent, well-formed results of the same shape (same fiber count, same boundary
            // sizing logic), demonstrating they share the same generation implementation.
            Assert.That(viaPython.FiberRadii.Length, Is.EqualTo(viaCore.FiberRadii.Length));
            Assert.That(viaPython.BoundaryDimensions[0], Is.EqualTo(viaCore.BoundaryDimensions[0]).Within(1e-9));
            Assert.That(viaPython.BoundaryDimensions[1], Is.EqualTo(viaCore.BoundaryDimensions[1]).Within(1e-9));
        }

        [Test]
        public void RandomRVEGenerationResult_DoesNotExposeFDEMDomainObjects()
        {
            // Verify the public surface of the result type consists only of primitive arrays,
            // not FDEM internal domain objects (Fiber, Packing, RandomPack, CellBoundary, etc.).
            var allowedTypes = new[] { typeof(double[,]), typeof(double[]) };

            foreach (var property in typeof(RandomRVEGenerationResult).GetProperties())
            {
                Assert.That(allowedTypes, Does.Contain(property.PropertyType),
                    $"Property {property.Name} exposes non-primitive type {property.PropertyType}");
            }
        }
    }
}
