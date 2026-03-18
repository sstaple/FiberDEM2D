using FxTMeshGenerator.Geometry;

namespace FxTMeshGenerator.Meshing.Elements
{
    /// <summary>
    /// Base class for finite elements in the mesh.
    /// </summary>
    public abstract class BaseElement
    {
        /// <summary>Global element ID</summary>
        public int Id { get; init; }
        
        /// <summary>Material phase (Fiber or Matrix)</summary>
        public ElementPhase Phase { get; init; }
        
        /// <summary>Node coordinates for this element</summary>
        public Point2D[] Nodes { get; init; }
        
        /// <summary>Total number of nodes in this element</summary>
        public int NodeCount => Nodes?.Length ?? 0;

        protected BaseElement(int id, ElementPhase phase, Point2D[] nodes)
        {
            Id = id;
            Phase = phase;
            Nodes = nodes;
        }
    }
}
