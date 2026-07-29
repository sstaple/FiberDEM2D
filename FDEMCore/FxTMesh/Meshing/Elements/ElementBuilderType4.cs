using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public class ElementBuilderType4_2p5D : ElementBuilderType4
    {
        public override string GetQuadName() { return "2P5DQ13.3"; }
        public override string GetTriangleName() { return "2P5DT10.6"; }
    }
    public class ElementBuilderType4 : ElementBuilderBase
    {
        public override int GetNNodesPerSideOfElement() { return 4; }
        public override string GetQuadName() { return "2DQ12.3" ; }
        public override string GetTriangleName() { return "2DT9.6"; }
        public override ElementBuildResult BuildInteriorMatrixTriangle(Point2D node0, Point2D node1, Point2D node2)
        {
            var nodes = BuildMatrixTriangle9(node0, node1, node2);
            return MatrixTriangle(GetTriangleName(), nodes);
        }

        public override ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius)
        {
            var nodes = BuildFiberTriangle9(fiberCenter, surfaceNode1,
                surfaceNode2, fiberRadius);
            return FiberTriangle(GetTriangleName(), nodes);
        }

        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad12(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad(GetQuadName() , nodes);
        }

        public override ElementBuildResult BuildFiberBoundaryMatrixTriangle(Point2D[] fiberNodes, Point2D boundaryPoint)
        {
            var nodes = BuildMatrixTriangle9WithFiberEdge(fiberNodes, boundaryPoint);
            return MatrixTriangle(GetTriangleName(), nodes);
        }
    }
}
