
using System.Collections.Generic;
using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing;

namespace FDEMCore.FxTMesh.Meshing
{
    public sealed class TriangulationMesh2D
    {
        public IReadOnlyList<Node> Nodes { get; }
        public IReadOnlyList<int[]> Triangles { get; }

        public TriangulationMesh2D(
            IReadOnlyList<Node> nodes,
            IReadOnlyList<int[]> triangles)
        {
            Nodes = nodes;
            Triangles = triangles;
        }
    }
}
