using FDEMCore.FxTMesh.Geometry;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public class ElementBuilderType2 : ElementBuilderBase
    {
        public override ElementBuildResult BuildInteriorMatrixTriangle(Point2D node0, Point2D node1, Point2D node2)
        {
            var nodes = BuildMatrixTriangle6(node0, node1, node2);
            return MatrixTriangle("2DT6.3", nodes);
        }

        public override ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius, bool isEdgeCCW)
        {
            var nodes = BuildFiberTriangle6(fiberCenter, surfaceNode1, surfaceNode2,
                fiberRadius, isEdgeCCW);

            return FiberTriangle("2DT6.3", nodes);
        }

        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad8(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad("2DQ8.4", nodes);
        }

        public override ElementBuildResult BuildFiberBoundaryMatrixTriangle( Point2D[] fiberNodes, Point2D boundaryPoint, bool isEdgeCCW)
        {
            var nodes = BuildMatrixTriangle6WithFiberEdge(fiberNodes, boundaryPoint, isEdgeCCW);
            return MatrixTriangle("2DT6.3", nodes);
        }
    }
}