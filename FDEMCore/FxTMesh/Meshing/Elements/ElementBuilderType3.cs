using FDEMCore.FxTMesh.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public sealed class ElementBuilderType3 : ElementBuilderType2
    {
        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad9(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad("2DQ9.4", nodes);
        }
    }
}
