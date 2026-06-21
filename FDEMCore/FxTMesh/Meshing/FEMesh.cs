using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing.Elements;
using System.Collections.Generic;

namespace FDEMCore.FxTMesh.Meshing
{
    /// <summary>
    /// Finite element mesh with global nodes, elements, and periodic boundary information.
    /// </summary>
    public sealed class FEMesh
    {
        /// <summary>Global node coordinates</summary>
        public IReadOnlyList<Point2D> GlobalNodes { get; init; }
        
        /// <summary>All finite elements in the mesh</summary>
        public IReadOnlyList<Element> Elements { get; init; }
        
        /// <summary>Pairs of nodes that are periodic (node1, node2) where node2 is the projection of node1</summary>
        public IReadOnlyList<(int Node1, int Node2)> PeriodicNodePairs { get; init; }
        public IReadOnlyList<int> X1Nodes { get; init; }
        public IReadOnlyList<int> Y1Nodes { get; init; }
        public int? PinnedNode { get; init; }

        public FEMesh(
    IReadOnlyList<Point2D> globalNodes,
    IReadOnlyList<Element> elements,
    IReadOnlyList<(int, int)> periodicPairs,
    IReadOnlyList<int> x1Nodes,
    IReadOnlyList<int> y1Nodes,
    int? pinnedNode)
        {
            GlobalNodes = globalNodes;
            Elements = elements;
            PeriodicNodePairs = periodicPairs;
            X1Nodes = x1Nodes;
            Y1Nodes = y1Nodes;
            PinnedNode = pinnedNode;
        }
    }
}
