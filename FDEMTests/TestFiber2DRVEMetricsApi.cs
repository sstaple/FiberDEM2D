using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Fiber2DRVEMetrics;

namespace FDEMTests
{
    /// <summary>
    /// Tests for the Fiber2DRVEMetrics reusable library API: argument validation on
    /// RVEMetricsService.Analyze, and a regression test confirming the direct-array API
    /// produces the same six metrics as the existing pack/CSV file pathway.
    /// </summary>
    public class TestFiber2DRVEMetricsApi
    {
        private static (double[,] locations, double[] radii, double[] boundary) MakeSampleFibers()
        {
            // 5x4 grid of fibers, radius 1.0, inside a 20x20 boundary.
            var locationsList = new System.Collections.Generic.List<(double y, double z)>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double y = 2.0 + i * 4.0;
                    double z = 2.0 + j * 4.0;
                    locationsList.Add((y, z));
                }
            }

            double[,] locations = new double[locationsList.Count, 2];
            double[] radii = new double[locationsList.Count];
            for (int k = 0; k < locationsList.Count; k++)
            {
                locations[k, 0] = locationsList[k].y;
                locations[k, 1] = locationsList[k].z;
                radii[k] = 1.0;
            }

            double[] boundary = new double[] { 20.0, 20.0 };
            return (locations, radii, boundary);
        }

        #region Argument Validation

        [Test]
        public void Analyze_NullFiberLocations_Throws()
        {
            var (_, radii, boundary) = MakeSampleFibers();
            Assert.Throws<ArgumentNullException>(() => RVEMetricsService.Analyze(null!, radii, boundary));
        }

        [Test]
        public void Analyze_NullFiberRadii_Throws()
        {
            var (locations, _, boundary) = MakeSampleFibers();
            Assert.Throws<ArgumentNullException>(() => RVEMetricsService.Analyze(locations, null!, boundary));
        }

        [Test]
        public void Analyze_NullBoundaryDimensions_Throws()
        {
            var (locations, radii, _) = MakeSampleFibers();
            Assert.Throws<ArgumentNullException>(() => RVEMetricsService.Analyze(locations, radii, null!));
        }

        [Test]
        public void Analyze_FiberLocationsWithWrongColumnCount_Throws()
        {
            double[,] badLocations = new double[3, 3];
            double[] radii = new double[] { 1.0, 1.0, 1.0 };
            double[] boundary = new double[] { 20.0, 20.0 };
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(badLocations, radii, boundary));
        }

        [Test]
        public void Analyze_EmptyFiberLocations_Throws()
        {
            double[,] emptyLocations = new double[0, 2];
            double[] radii = Array.Empty<double>();
            double[] boundary = new double[] { 20.0, 20.0 };
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(emptyLocations, radii, boundary));
        }

        [Test]
        public void Analyze_MismatchedRadiiLength_Throws()
        {
            var (locations, _, boundary) = MakeSampleFibers();
            double[] wrongRadii = new double[] { 1.0, 1.0 };
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(locations, wrongRadii, boundary));
        }

        [Test]
        public void Analyze_WrongBoundaryDimensionsLength_Throws()
        {
            var (locations, radii, _) = MakeSampleFibers();
            double[] badBoundary = new double[] { 20.0 };
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(locations, radii, badBoundary));
        }

        [Test]
        public void Analyze_NonPositiveBoundaryDimensions_Throws()
        {
            var (locations, radii, _) = MakeSampleFibers();
            double[] badBoundary = new double[] { 20.0, 0.0 };
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(locations, radii, badBoundary));
        }

        [Test]
        public void Analyze_NonPositiveRadius_Throws()
        {
            var (locations, radii, boundary) = MakeSampleFibers();
            radii[0] = 0.0;
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(locations, radii, boundary));
        }

        [Test]
        public void Analyze_NaNValue_Throws()
        {
            var (locations, radii, boundary) = MakeSampleFibers();
            locations[0, 0] = double.NaN;
            Assert.Throws<ArgumentException>(() => RVEMetricsService.Analyze(locations, radii, boundary));
        }

        #endregion

        #region Regression: direct-array API vs. pack/CSV pathway

        [Test]
        public void Analyze_MatchesPackFilePathway_ForSameFiberData()
        {
            var (locations, radii, boundary) = MakeSampleFibers();

            // Direct-array API
            RVEMetricsResult directResult = RVEMetricsService.Analyze(locations, radii, boundary);

            // Existing pack/CSV pathway, using the same fiber data
            string tempDir = Path.Combine(Path.GetTempPath(), "Fiber2DRVEMetricsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string csvPath = Path.Combine(tempDir, "sample.csv");
            try
            {
                WritePackFileCsv(csvPath, locations, radii, boundary[0], boundary[1]);

                PackFile packFile = new PackFile(csvPath, tempDir);
                packFile.Initiate(new OutputOptions());
                RVEMetricsResult? packResult = packFile.Result;

                Assert.That(packResult, Is.Not.Null);
                const double tolerance = 1e-9;
                Assert.That(packResult!.VfMedian, Is.EqualTo(directResult.VfMedian).Within(tolerance));
                Assert.That(packResult.VfIqr, Is.EqualTo(directResult.VfIqr).Within(tolerance));
                Assert.That(packResult.FCAreaDensity, Is.EqualTo(directResult.FCAreaDensity).Within(tolerance));
                Assert.That(packResult.MRCAreaDensity, Is.EqualTo(directResult.MRCAreaDensity).Within(tolerance));
                Assert.That(packResult.FCNumberDensity, Is.EqualTo(directResult.FCNumberDensity).Within(tolerance));
                Assert.That(packResult.MRCNumberDensity, Is.EqualTo(directResult.MRCNumberDensity).Within(tolerance));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        private static void WritePackFileCsv(string path, double[,] locations, double[] radii, double yBoundary, double zBoundary)
        {
            using var writer = new StreamWriter(path);
            writer.WriteLine("dummy,length Y,length Z");
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, ",{0},{1}", yBoundary, zBoundary));

            int n = locations.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                writer.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2}",
                    locations[i, 0], locations[i, 1], radii[i]));
            }
        }

        #endregion
    }
}
