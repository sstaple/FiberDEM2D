using FxTMeshGenerator.Geometry;

namespace FxTMeshGenerator.Meshing.Elements
{
    /// <summary>
    /// Quadrilateral finite element (6, 8, 12, or 16 nodes).
    /// </summary>
    public sealed class QuadrilateralElement : BaseElement
    {
        public QuadrilateralElement(int id, ElementPhase phase, Point2D[] nodes) 
            : base(id, phase, nodes)
        {
            if (nodes.Length != 6 && nodes.Length != 8 && nodes.Length != 12 && nodes.Length != 16)
                throw new ArgumentException($"Quad must have 6, 8, 12, or 16 nodes, got {nodes.Length}");
        }
    }
}
