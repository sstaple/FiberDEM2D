using FxTMeshGenerator.Meshing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FxTMeshGenerator.Geometry
{
    public sealed class Node
    {
        public Point2D P { get; }
        public int? FiberId { get; } // the original fiber ID from projection, or null for boundary points or original points.
        public NodeType Type { get; }
        public (int ox, int oy) Offset { get; }

        public Node(Point2D p, int? fiberId, NodeType nodeType, (int ox, int oy) offset)
        {
            P = p;
            FiberId = fiberId;
            Type = nodeType;
            Offset = offset;
        }
    }
}
