using FDEMCore.FxTMesh.Geometry;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public sealed class ElementBuilderType1 : ElementBuilderBase
    {
        public override ElementBuildResult BuildInteriorMatrixTriangle(Point2D node0, Point2D node1, Point2D node2)
        {
            var nodes = BuildMatrixTriangle3(node0, node1, node2);
            return MatrixTriangle("2P5DT4", nodes);
        }

        public override ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius)
        {
            var nodes = BuildFiberTriangle6(fiberCenter, surfaceNode1, surfaceNode2,
                fiberRadius);

            return FiberTriangle("2P5DT7", nodes);
        }

        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad6(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad("2P5DQ7", nodes);
        }

        public override ElementBuildResult BuildFiberBoundaryMatrixTriangle(
            Point2D[] fiberNodes, Point2D boundaryPoint)
        {
            var nodes = BuildMatrixTriangle3WithFiberEdge(fiberNodes, boundaryPoint);
            return MatrixTriangle("2DT3", nodes);
        }
    }
}