namespace FDEMCore.FxTMesh.Meshing.Elements
{
    /// <summary>
    /// Represents the material phase of an element.
    /// </summary>
    public enum ElementPhase
    {
        /// <summary>Matrix material (between fibers)</summary>
        Matrix,
        
        /// <summary>Fiber material</summary>
        Fiber,

        /// <summary>
        /// Composite material (used for elements that contain both fiber and matrix, usually part of the top layers in a cross-ply specimen).
        /// </summary>
        Composite
    }
}
