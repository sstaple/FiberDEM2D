using FxTMeshGenerator.Geometry;

namespace FxTMeshGenerator.Meshing.Elements
{
    /// <summary>
    /// Quadrilateral finite element (4, 6, 8, 9, 10, 12, or 16 nodes).
    /// </summary>
    public sealed class QuadElement : BaseElement
    {
        public QuadElement(int id, ElementPhase phase, Point2D[] nodes) 
            : base(id, phase, nodes)
        {
            if (nodes.Length != 4 && nodes.Length != 6 && nodes.Length != 8 && nodes.Length != 9 && nodes.Length != 10 && nodes.Length != 12 && nodes.Length != 16)
                throw new ArgumentException($"Quad must have 4, 6, 8, 9, 10, 12, or 16 nodes, got {nodes.Length}");
        }
    }
}
