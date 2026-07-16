using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing.Elements;
using FDEMCore.FxTMesh.Meshing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh.Meshing.Elements
{

    public interface IElementBuilder
    {
        ElementBuildResult BuildInteriorMatrixTriangle( Point2D node0, Point2D node1, Point2D node2);

        ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius);

        ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW);

        ElementBuildResult BuildFiberBoundaryMatrixTriangle(Point2D[] fiberNodes,
            Point2D boundaryPoint);
    }

    public sealed class ElementBuildResult
    {
        public string ElementName { get; }
        public Point2D[] Nodes { get; }

        public ElementBuildResult(string elementName, Point2D[] nodes)
        {
            ElementName = elementName;
            Nodes = nodes;
        }
    }

    public static class ElementBuilderProvider
    {
        public static IElementBuilder Create(ElementConfig config)
        {
            return config.Family switch
            {
                FxTElementFamily.Type1 => new ElementBuilderType1(),
                FxTElementFamily.Type2 => new ElementBuilderType2(),
                FxTElementFamily.Type3 => new ElementBuilderType3(),
                FxTElementFamily.Type4 => new ElementBuilderType4(),
                FxTElementFamily.Type5 => new ElementBuilderType5(),
                _ => throw new NotSupportedException()
            };
        }
    }
}