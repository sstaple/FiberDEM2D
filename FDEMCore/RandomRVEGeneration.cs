using System;
using System.IO;

namespace FDEMCore
{
    public class RandomRVEGenerationOptions
    {
        public double FiberRadius { get; set; } = 1.0;
        public double FiberVolumeFraction { get; set; } = 0.5;
        public int NRows { get; set; } = 5;

        public double FiberLinearDensity { get; set; } = 1.0;
        public double FiberLength { get; set; } = 1.0;
        public double FiberAxialModulus { get; set; } = 1.0;
        public double FiberTransverseModulus { get; set; } = 1.0;
        public double FiberPoissonsRatio { get; set; } = 0.3;
        public double FiberGlobalDamping { get; set; } = 0.0;

        public double[] MultipleRadii { get; set; }
        public double[] MultipleRadiiPercentages { get; set; }

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
        public bool SolidBoundaryY { get; set; } = false;
        public bool SolidBoundaryZ { get; set; } = false;

        public bool SaveResults { get; set; } = false;
        public bool SaveFinalPositions { get; set; } = false;
        public bool SaveFinalPositionsWithoutProjections { get; set; } = false;
        public bool SaveVfStatistics { get; set; } = false;
        public bool SaveConnectionPlot { get; set; } = false;
        public string OutputDirectory { get; set; }
        public string OutputFileName { get; set; } = "FDEMPython_RVE";
    }

    public class RandomRVEGenerationResult
    {
        public double[,] FiberLocations { get; set; }
        public double[] FiberRadii { get; set; }
        public double[] BoundaryDimensions { get; set; }
    }

    public static class RandomRVEGenerationService
    {
        public static RandomRVEGenerationResult Generate(RandomRVEGenerationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            FiberParameters fiberParams = options.MultipleRadii != null
                ? new FiberMultipleRadiiParameters(options.MultipleRadii, options.MultipleRadiiPercentages, options.FiberLinearDensity, options.FiberLength, options.FiberAxialModulus, options.FiberTransverseModulus, options.FiberPoissonsRatio, options.FiberGlobalDamping)
                : new FiberParameters(options.FiberRadius, options.FiberLinearDensity, options.FiberLength, options.FiberAxialModulus, options.FiberTransverseModulus, options.FiberPoissonsRatio, options.FiberGlobalDamping);

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
                saveResults = options.SaveResults,
                saveFinalPositions = options.SaveFinalPositions,
                saveFinalPositionsWithoutProjections = options.SaveFinalPositionsWithoutProjections,
                saveVfStatistics = options.SaveVfStatistics,
                saveConnectionPlot = options.SaveConnectionPlot,
            };

            packing.BoundaryTypes[1] = options.SolidBoundaryY ? BoundaryType.Solid : BoundaryType.Periodic;
            packing.BoundaryTypes[2] = options.SolidBoundaryZ ? BoundaryType.Solid : BoundaryType.Periodic;
            packing.NRows = options.NRows;

            bool saveAnyOutput = options.SaveResults || options.SaveFinalPositions || options.SaveFinalPositionsWithoutProjections || options.SaveVfStatistics;
            if (saveAnyOutput)
            {
                string outputDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory) ? Directory.GetCurrentDirectory() : options.OutputDirectory;
                Directory.CreateDirectory(outputDirectory);
                string outputFileName = string.IsNullOrWhiteSpace(options.OutputFileName) ? "FDEMPython_RVE" : options.OutputFileName;
                OutputParameters outputParameters = new OutputParameters(outputDirectory, options.SaveResults, false, false, false)
                {
                    FileName = outputFileName,
                    FileIndex = string.Empty
                };
                packing.SetPacking(outputParameters);
            }
            else
            {
                packing.SetPacking();
            }

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
                locations[i, 0] = f.CurrentPosition[1];
                locations[i, 1] = f.CurrentPosition[2];
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
