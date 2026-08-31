using FDEMCore;

namespace FDEMPython
{
    /// <summary>
    /// Public interoperability surface intended for use by external (e.g. Python) callers.
    ///
    /// This is intentionally a very thin pass-through onto FDEMCore's common RVE-generation API
    /// (<see cref="RandomRVEGenerationService"/>). It exists as a separate assembly/project so that:
    ///  - The public contract exposed to Python is decoupled from FDEMCore's internal namespace/assembly.
    ///  - FDEMCore has no dependency on FDEMPython (dependency direction: FDEMPython -> FDEMCore only).
    ///
    /// The request (<see cref="RandomRVEGenerationOptions"/>) and result (<see cref="RandomRVEGenerationResult"/>)
    /// types consist solely of primitive values and arrays of primitives, so they are safe to expose across
    /// a Python interop boundary (e.g. via pythonnet) without leaking FDEM internal domain objects such as
    /// Fiber, Packing, RandomPack, or CellBoundary.
    /// </summary>
    public static class RveApi
    {
        /// <summary>
        /// Generates a single random RVE realization and returns fiber locations, radii, and boundary
        /// dimensions. See <see cref="RandomRVEGenerationResult"/> for the exact coordinate convention.
        /// </summary>
        public static RandomRVEGenerationResult GenerateRandomRVE(RandomRVEGenerationOptions options)
        {
            return RandomRVEGenerationService.Generate(options);
        }
    }
}
