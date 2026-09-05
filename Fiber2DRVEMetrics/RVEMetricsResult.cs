namespace Fiber2DRVEMetrics
{
    /// <summary>
    /// The six descriptors computed by <see cref="RVEMetricsService"/> for a 2D fiber microstructure.
    /// </summary>
    public sealed class RVEMetricsResult
    {
        /// <summary>Median of the local fiber volume fraction distribution.</summary>
        public double VfMedian { get; init; }

        /// <summary>Interquartile range of the local fiber volume fraction distribution.</summary>
        public double VfIqr { get; init; }

        /// <summary>Fiber-cluster area density (fraction of total area occupied by fiber clusters).</summary>
        public double FCAreaDensity { get; init; }

        /// <summary>Matrix-rich-cluster area density (fraction of total area occupied by matrix-rich clusters).</summary>
        public double MRCAreaDensity { get; init; }

        /// <summary>Fiber-cluster number density (number of fiber clusters per fiber).</summary>
        public double FCNumberDensity { get; init; }

        /// <summary>Matrix-rich-cluster number density (number of matrix-rich clusters per fiber).</summary>
        public double MRCNumberDensity { get; init; }
    }
}
