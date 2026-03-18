using FxTMeshGenerator.Geometry;

namespace FxTMeshGenerator.Meshing.Elements
{
    /// <summary>
    /// Triangular finite element (3, 6, 9, or 12 nodes).
    /// </summary>
    public sealed class TriangleElement : BaseElement
    {
        public TriangleElement(int id, ElementPhase phase, Point2D[] nodes) 
            : base(id, phase, nodes)
        {
            if (nodes.Length != 3 && nodes.Length != 6 && nodes.Length != 9 && nodes.Length != 12)
                throw new ArgumentException($"Triangle must have 3, 6, 9, or 12 nodes, got {nodes.Length}");
        }
    }
}
