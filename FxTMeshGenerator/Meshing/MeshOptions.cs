
namespace FxTMeshGenerator.Meshing
{
    public sealed class MeshOptions
    {
        /// <summary>
        /// For solid boundaries, boundary-point spacing is estimated as sqrt(area / nFibers) * multiplier.
        /// </summary>
        public double BoundaryPointSpacingMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Numerical tolerance for inside tests and de-duplication.
        /// </summary>
        public double Tolerance { get; set; } = 1e-10;
    }
    public sealed class DebugOptions
    {
        /// <summary>
        /// File path for debug options output (e.g., for logging or intermediate files).
        /// </summary>
        public string Directory { get; set; } = "debug_output";

        public string FileName { get; set; } = "debug_output";

        public bool Debug { get; set; } = false;

    }
}

