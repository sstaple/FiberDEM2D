using FDEMCore.FxTMesh.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public sealed class ElementBuilderType3_2p5D : ElementBuilderType2_2p5D
    {
        public override string GetQuadName() { return "2P5DQ10.2"; }
        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad9(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad(GetQuadName(), nodes);
        }
    }
    public sealed class ElementBuilderType3 : ElementBuilderType2
    {
        public override string GetQuadName() {return "2DQ9.2"; }
        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad9(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad(GetQuadName() , nodes);
        }
    }
}
