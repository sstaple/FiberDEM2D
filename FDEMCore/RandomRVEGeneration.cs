using System;

namespace FDEMCore
{
    /// <summary>
    /// Strongly-typed options for generating a random RVE (representative volume element) of fibers.
    /// This mirrors the set of options that can be supplied through the random-RVE input file
    /// (see <see cref="RandomRVEGeneratorInputFile"/> and <see cref="RandomPack.ReadAndSetRandomPackingOptions"/>),
    /// exposed here as plain properties so that callers (such as FDEMPython) do not need to construct
    /// or parse a textual input file.
    /// </summary>
    public class RandomRVEGenerationOptions
    {
        #region Required generation parameters

        /// <summary>Fiber radius. Ignored if <see cref="MultipleRadii"/> is supplied.</summary>
        public double FiberRadius { get; set; } = 1.0;

        /// <summary>Target fiber volume fraction for the generated RVE.</summary>
        public double FiberVolumeFraction { get; set; } = 0.5;

        /// <summary>
        /// Number of rows used to determine the number of fibers (NRows^2), unless
        /// <see cref="IsNRowsActuallyNFibers"/> is true, in which case this is the fiber count directly.
        /// </summary>
        public int NRows { get; set; } = 5;

        /// <summary>Number of independent repetitions (RVE realizations) to generate.</summary>
        public int NRepetitions { get; set; } = 1;

        #endregion

        #region Fiber material parameters (affect the relaxation algorithm, not just geometry)

        public double FiberLinearDensity { get; set; } = 1.0;
        public double FiberLength { get; set; } = 1.0;
        public double FiberAxialModulus { get; set; } = 1.0;
        public double FiberTransverseModulus { get; set; } = 1.0;
        public double FiberPoissonsRatio { get; set; } = 0.3;
        public double FiberGlobalDamping { get; set; } = 0.0;

        /// <summary>
        /// Optional set of discrete fiber radii to use instead of a single <see cref="FiberRadius"/>.
        /// When supplied, <see cref="MultipleRadiiPercentages"/> must also be supplied with the same length.
        /// </summary>
        public double[] MultipleRadii { get; set; }

        /// <summary>Percentage (by count) of fibers to assign to each radius in <see cref="MultipleRadii"/>.</summary>
        public double[] MultipleRadiiPercentages { get; set; }

        #endregion

        #region Optional RandomPack generation options (see RandomPack.ReadAndSetRandomPackingOptions)

        public double MinSpacingBetweenFibers { get; set; } = 0.0;
        public int NFibersPerSquare { get; set; } = 1;
        public double SquareMargin { get; set; } = 0.75;
        public double RVEHOverW { get; set; } = 1.0;
        public double RVEThickness { get; set; } = -1.0;
        public double ContactDampingCoeff { get; set; } = 0.1;
        public double GlobalDampingCoeff { get; set; } = 1.0;
        public double IncreasingDampingCoeff { get; set; } = 0.001;
        public double PerKETol { get; set; } = 0.01;
        public int NMaxSteps { get; set; } = 3000;
        public int NUndampedSteps { get; set; } = 500;
        public bool IsNRowsActuallyNFibers { get; set; } = false;
        public bool DoNotAllowOverlaps { get; set; } = false;
        public double MinSpacingBetweenFiberAndSolidBoundary { get; set; } = 0.0;

        /// <summary>If true, the Y (width) boundary is a solid boundary instead of periodic.</summary>
        public bool SolidBoundaryY { get; set; } = false;

        /// <summary>If true, the Z (height) boundary is a solid boundary instead of periodic.</summary>
        public bool SolidBoundaryZ { get; set; } = false;

        #endregion
    }

    /// <summary>
    /// Result of a random RVE generation. Contains only primitive numerical arrays so that it can be
    /// consumed by non-.NET callers (e.g. Python) without exposing internal FDEM domain objects.
    ///
    /// Coordinate convention:
    /// - This is a 2-D (Y/Z cross-section) representation of the RVE. The fiber axis (X/length direction)
    ///   is not included.
    /// - The origin (0, 0) is the bottom-left corner of the RVE boundary (matching FDEMCore's
    ///   <see cref="CellBoundary"/> convention, where the bottom-left-back corner defines the origin).
    /// - Units are the same (consistent, unit-less) length units used throughout FDEM input; the caller
    ///   is responsible for interpreting them consistently with the fiber radius/RVE size supplied.
    /// - <see cref="FiberLocations"/>[i, 0] is the Y coordinate and [i, 1] is the Z coordinate of fiber i;
    ///   <see cref="FiberRadii"/>[i] is the radius of that same fiber (same index).
    /// - Fibers that are periodically projected across the RVE boundary (for visualization/meshing of
    ///   periodic wrap-around) are NOT included in the returned data; only the "true" (unwrapped) fiber
    ///   centers, one per generated fiber, are returned.
    /// </summary>
    public class RandomRVEGenerationResult
    {
        /// <summary>N x 2 array of fiber center locations: [i,0] = Y, [i,1] = Z.</summary>
        public double[,] FiberLocations { get; set; }

        /// <summary>N-element array of fiber radii, aligned by index with <see cref="FiberLocations"/>.</summary>
        public double[] FiberRadii { get; set; }

        /// <summary>[0] = RVE width (Y extent), [1] = RVE height (Z extent).</summary>
        public double[] BoundaryDimensions { get; set; }
    }

    /// <summary>
    /// Programmatic entry point for random RVE generation. This is the common generation API used both by
    /// the textual input-file pathway (<see cref="RandomRVEGeneratorInputFile"/>) and by any other caller
    /// (such as FDEMPython), so that both pathways invoke the exact same underlying <see cref="RandomPack"/>
    /// algorithm. This class does not alter or duplicate that algorithm; it only translates strongly-typed
    /// options into the existing <see cref="RandomPack"/> configuration and reads back the results.
    /// </summary>
    public static class RandomRVEGenerationService
    {
        public static RandomRVEGenerationResult Generate(RandomRVEGenerationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            FiberParameters fiberParams = options.MultipleRadii != null
                ? new FiberMultipleRadiiParameters(
                    options.MultipleRadii,
                    options.MultipleRadiiPercentages,
                    options.FiberLinearDensity,
                    options.FiberLength,
                    options.FiberAxialModulus,
                    options.FiberTransverseModulus,
                    options.FiberPoissonsRatio,
                    options.FiberGlobalDamping)
                : new FiberParameters(
                    options.FiberRadius,
                    options.FiberLinearDensity,
                    options.FiberLength,
                    options.FiberAxialModulus,
                    options.FiberTransverseModulus,
                    options.FiberPoissonsRatio,
                    options.FiberGlobalDamping);

            RandomPack packing = new RandomPack(options.NRows, options.FiberVolumeFraction, fiberParams)
            {
                minSpacingBetweenFibers = options.MinSpacingBetweenFibers,
                nFibersPerSquare = options.NFibersPerSquare,
                squareMargin = options.SquareMargin,
                RVEHOverW = options.RVEHOverW,
                RVEThickness = options.RVEThickness,
                contactDampingCoeff = options.ContactDampingCoeff,
                globalDampingCoeff = options.GlobalDampingCoeff,
                increasingDampingCoeff = options.IncreasingDampingCoeff,
                perKETol = options.PerKETol,
                nMaxSteps = options.NMaxSteps,
                nUndampedSteps = options.NUndampedSteps,
                isNRowsActuallyNFibers = options.IsNRowsActuallyNFibers,
                doNotAllowOverlaps = options.DoNotAllowOverlaps,
                minSpacingBetweenFiberAndSolidBoundary = options.MinSpacingBetweenFiberAndSolidBoundary,
            };

            packing.BoundaryTypes[1] = options.SolidBoundaryY ? BoundaryType.Solid : BoundaryType.Periodic;
            packing.BoundaryTypes[2] = options.SolidBoundaryZ ? BoundaryType.Solid : BoundaryType.Periodic;

            // Re-trigger boundary/row recalculation now that all options (which SetNRows depends on,
            // e.g. RVEHOverW, RVEThickness, IsNRowsActuallyNFibers, multiple-radii fiber parameters)
            // have been applied. This mirrors the re-computation performed by
            // RandomPack.ReadAndSetRandomPackingOptions for the same set of options.
            packing.NRows = options.NRows;

            packing.SetPacking();

            return ToResult(packing);
        }

        private static RandomRVEGenerationResult ToResult(RandomPack packing)
        {
            var fibers = packing.LFibers;
            double[,] locations = new double[fibers.Count, 2];
            double[] radii = new double[fibers.Count];

            for (int i = 0; i < fibers.Count; i++)
            {
                Fiber f = fibers[i];
                locations[i, 0] = f.CurrentPosition[1]; // Y
                locations[i, 1] = f.CurrentPosition[2]; // Z
                radii[i] = f.Radius;
            }

            return new RandomRVEGenerationResult
            {
                FiberLocations = locations,
                FiberRadii = radii,
                BoundaryDimensions = new double[] { packing.Width, packing.Height },
            };
        }
    }
}
