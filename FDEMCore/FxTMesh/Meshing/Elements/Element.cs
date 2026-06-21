using FDEMCore.FxTMesh.Geometry;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    /// <summary>
    /// Base class for finite elements in the mesh.
    /// </summary>
    public class Element
    {
        /// <summary>Global element ID</summary>
        public int Id { get; init; }
        
        /// <summary>Material phase (Fiber or Matrix)</summary>
        public ElementPhase Phase { get; init; }
        
        /// <summary>Node coordinates for this element</summary>
        public Point2D[] Nodes { get; init; }
        
        /// <summary>Total number of nodes in this element</summary>
        public int NodeCount => Nodes?.Length ?? 0;

        /// <summary>Total number of nodes in this element</summary>
        public string ElementName { get; init; }

        public Element(int id, ElementPhase phase, string elementName, Point2D[] nodes)
        {
            Id = id;
            Phase = phase;
            Nodes = nodes;
            ElementName = elementName;
        }
    }
}
