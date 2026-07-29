using FDEMCore.FxTMesh.Geometry;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public class ElementBuilderType2_2p5D : ElementBuilderType2
    {
        public override string GetQuadName() { return "2P5DQ9.2"; }
        public override string GetTriangleName() { return "2P5DT7.3"; }
    }
    public class ElementBuilderType2 : ElementBuilderBase
    {
        public override int GetNNodesPerSideOfElement() { return 3; }
        public override string GetQuadName() {return "2DQ8.2"; }
        public override string GetTriangleName() {return "2DT6.3"; }
        public override ElementBuildResult BuildInteriorMatrixTriangle(Point2D node0, Point2D node1, Point2D node2)
        {
            var nodes = BuildMatrixTriangle6(node0, node1, node2);
            return MatrixTriangle(GetTriangleName(), nodes);
        }

        public override ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius)
        {
            var nodes = BuildFiberTriangle6(fiberCenter, surfaceNode1, surfaceNode2,
                fiberRadius);

            return FiberTriangle(GetTriangleName(), nodes);
        }

        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad8(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad(GetQuadName(), nodes);
        }

        public override ElementBuildResult BuildFiberBoundaryMatrixTriangle(Point2D[] fiberNodes, Point2D boundaryPoint)
        {
            var nodes = BuildMatrixTriangle6WithFiberEdge(fiberNodes, boundaryPoint);
            return MatrixTriangle(GetTriangleName(), nodes);
        }

    }

}