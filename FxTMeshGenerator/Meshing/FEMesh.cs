using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.Meshing.Elements;

namespace FxTMeshGenerator.Meshing
{
    /// <summary>
    /// Finite element mesh with global nodes, elements, and periodic boundary information.
    /// </summary>
    public sealed class FEMesh
    {
        /// <summary>Global node coordinates</summary>
        public IReadOnlyList<Point2D> GlobalNodes { get; init; }
        
        /// <summary>All finite elements in the mesh</summary>
        public IReadOnlyList<BaseElement> Elements { get; init; }
        
        /// <summary>Pairs of nodes that are periodic (node1, node2) where node2 is the projection of node1</summary>
        public IReadOnlyList<(int Node1, int Node2)> PeriodicNodePairs { get; init; }
        
        /// <summary>Nodes on the top edge of the RVE</summary>
        public IReadOnlyList<int> TopEdgeNodes { get; init; }
        
        /// <summary>Nodes on the right edge of the RVE</summary>
        public IReadOnlyList<int> RightEdgeNodes { get; init; }

        public FEMesh(
            IReadOnlyList<Point2D> globalNodes,
            IReadOnlyList<BaseElement> elements,
            IReadOnlyList<(int, int)> periodicPairs,
            IReadOnlyList<int> topEdgeNodes,
            IReadOnlyList<int> rightEdgeNodes)
        {
            GlobalNodes = globalNodes;
            Elements = elements;
            PeriodicNodePairs = periodicPairs;
            TopEdgeNodes = topEdgeNodes;
            RightEdgeNodes = rightEdgeNodes;
        }
    }
}
