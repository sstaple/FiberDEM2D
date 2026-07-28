using FDEMCore.FxTMesh.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    internal class ElementBuilderType5 : ElementBuilderType4
    {
        public override ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = BuildMatrixQuad16(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            return MatrixQuad("2DQ16.9", nodes);
        }
    }
}
