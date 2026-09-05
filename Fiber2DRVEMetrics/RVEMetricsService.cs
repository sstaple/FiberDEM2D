using System;
using System.Collections.Generic;

namespace Fiber2DRVEMetrics
{
    /// <summary>
    /// Public entry point for computing 2D fiber-microstructure clustering metrics directly
    /// from fiber geometry arrays, without requiring any file I/O.
    /// </summary>
    public static class RVEMetricsService
    {
        /// <summary>
        /// Analyzes a 2D fiber microstructure and returns the six clustering/volume-fraction descriptors.
        /// </summary>
        /// <param name="fiberLocations">An Nx2 array of fiber center coordinates (column 0 = Y, column 1 = Z).</param>
        /// <param name="fiberRadii">An array of length N with the fiber radii.</param>
        /// <param name="boundaryDimensions">A 2-element array: [YBoundary, ZBoundary].</param>
        /// <param name="outputOptions">Optional output options controlling optional ParaView file writing. Defaults to no file output.</param>
        /// <param name="saveDirectory">Optional directory to write ParaView output to, if enabled in <paramref name="outputOptions"/>.</param>
        /// <param name="baseName">Optional base name used for any ParaView output files.</param>
        /// <returns>The computed <see cref="RVEMetricsResult"/>.</returns>
        public static RVEMetricsResult Analyze(
            double[,] fiberLocations,
            double[] fiberRadii,
            double[] boundaryDimensions,
            OutputOptions? outputOptions = null,
            string? saveDirectory = null,
            string? baseName = null)
        {
            ValidateInputs(fiberLocations, fiberRadii, boundaryDimensions);

            int n = fiberLocations.GetLength(0);
            var y = new List<double>(n);
            var z = new List<double>(n);
            var r = new List<double>(n);
            for (int i = 0; i < n; i++)
            {
                y.Add(fiberLocations[i, 0]);
                z.Add(fiberLocations[i, 1]);
                r.Add(fiberRadii[i]);
            }

            var options = outputOptions ?? new OutputOptions();

            var microstructure = new Microstructure(
                options,
                filePath: null,
                packFileName: baseName,
                saveDirectory: saveDirectory,
                y, z, r,
                yBoundary: boundaryDimensions[0],
                zBoundary: boundaryDimensions[1]);

            return new RVEMetricsResult
            {
                VfMedian = microstructure.VfMdn,
                VfIqr = microstructure.VfIqr,
                FCAreaDensity = microstructure.FCDensity,
                MRCAreaDensity = microstructure.MRCDensity,
                FCNumberDensity = microstructure.FCNumDensity,
                MRCNumberDensity = microstructure.MRCNumDensity
            };
        }

        private static void ValidateInputs(double[,] fiberLocations, double[] fiberRadii, double[] boundaryDimensions)
        {
            if (fiberLocations == null)
                throw new ArgumentNullException(nameof(fiberLocations));
            if (fiberRadii == null)
                throw new ArgumentNullException(nameof(fiberRadii));
            if (boundaryDimensions == null)
                throw new ArgumentNullException(nameof(boundaryDimensions));

            if (fiberLocations.GetLength(1) != 2)
                throw new ArgumentException("fiberLocations must have exactly 2 columns (Y, Z).", nameof(fiberLocations));

            int n = fiberLocations.GetLength(0);
            if (n == 0)
                throw new ArgumentException("fiberLocations must contain at least one fiber.", nameof(fiberLocations));

            if (fiberRadii.Length != n)
                throw new ArgumentException("fiberRadii length must match the number of rows in fiberLocations.", nameof(fiberRadii));

            if (boundaryDimensions.Length != 2)
                throw new ArgumentException("boundaryDimensions must contain exactly 2 values: [YBoundary, ZBoundary].", nameof(boundaryDimensions));

            if (boundaryDimensions[0] <= 0 || boundaryDimensions[1] <= 0)
                throw new ArgumentException("boundaryDimensions values must be positive.", nameof(boundaryDimensions));

            for (int i = 0; i < n; i++)
            {
                if (fiberRadii[i] <= 0)
                    throw new ArgumentException($"fiberRadii[{i}] must be positive.", nameof(fiberRadii));
                if (double.IsNaN(fiberLocations[i, 0]) || double.IsNaN(fiberLocations[i, 1]) || double.IsNaN(fiberRadii[i]))
                    throw new ArgumentException("fiberLocations and fiberRadii must not contain NaN values.");
            }
        }
    }
}
