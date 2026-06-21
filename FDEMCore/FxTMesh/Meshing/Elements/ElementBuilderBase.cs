using FDEMCore.FxTMesh.Geometry;
using System;
using System.Linq;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public abstract class ElementBuilderBase : IElementBuilder
    {
        public abstract ElementBuildResult BuildInteriorMatrixTriangle(Point2D node0, Point2D node1, Point2D node2);

        public abstract ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius, bool isEdgeCCW);

        public abstract ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW);

        public abstract ElementBuildResult BuildFiberBoundaryMatrixTriangle(Point2D[] fiberNodes, Point2D boundaryPoint, bool isEdgeCCW);

        protected static ElementBuildResult MatrixTriangle(string type, Point2D[] nodes)
        {
            return new ElementBuildResult(type, nodes);
        }

        protected static ElementBuildResult FiberTriangle(string type, Point2D[] nodes)
        {
            return new ElementBuildResult(type, nodes);
        }

        protected static ElementBuildResult MatrixQuad(string type, Point2D[] nodes)
        {
            return new ElementBuildResult(type, nodes);
        }

        #region Element Geometry Builders: Fiber Triangles


        public static Point2D[] BuildFiberTriangle6(Point2D fiberCenter, Point2D node1,
            Point2D node2, double fiberRadius, bool isEdgeCCW)
        {
            var nodes = new Point2D[6];

            nodes[0] = fiberCenter;

            if (isEdgeCCW)
            {
                nodes[2] = node2;
                nodes[4] = node1;
            }
            else
            {
                nodes[2] = node1;
                nodes[4] = node2;
            }

            nodes[1] = Midpoint(nodes[0], nodes[2]);
            nodes[5] = Midpoint(nodes[0], nodes[4]);
            nodes[3] = FiberSurfaceArcPoint(fiberCenter, nodes[2], nodes[4], fiberRadius, 0.5);
            return nodes;
        }

        public static Point2D[] BuildFiberTriangle9(Point2D fiberCenter, Point2D node1,
            Point2D node2, double fiberRadius, bool isEdgeCCW)
        {
            var nodes = new Point2D[9];

            nodes[0] = fiberCenter;

            Point2D surfaceA;
            Point2D surfaceB;

            if (isEdgeCCW)
            {
                surfaceA = node2;
                surfaceB = node1;
            }
            else
            {
                surfaceA = node1;
                surfaceB = node2;
            }

            // Edge from center to surfaceA
            nodes[1] = PointAlong(fiberCenter, surfaceA, 1.0 / 3.0);
            nodes[2] = PointAlong(fiberCenter, surfaceA, 2.0 / 3.0);

            nodes[3] = surfaceA;

            // Curved fiber surface edge from surfaceA to surfaceB
            nodes[4] = FiberSurfaceArcPoint(fiberCenter, surfaceA, surfaceB, fiberRadius, 1.0 / 3.0);
            nodes[5] = FiberSurfaceArcPoint(fiberCenter, surfaceA, surfaceB, fiberRadius, 2.0 / 3.0);

            nodes[6] = surfaceB;

            // Two internal nodes for 9-node triangle
            nodes[7] = PointAlong(surfaceB, fiberCenter, 1.0 / 3.0);
            nodes[8] = PointAlong(surfaceB, fiberCenter, 2.0 / 3.0);

            return nodes;
        }

        #endregion

        #region Element Geometry Builders: Matrix Triangles


        public static Point2D[] BuildMatrixTriangle3(Point2D node0, Point2D node1, Point2D node2)
        {
            return new[] { node0, node1, node2 };
        }

        public static Point2D[] BuildMatrixTriangle6(Point2D node0, Point2D node1, Point2D node2)
        {
            var nodes = new Point2D[6];

            nodes[0] = node0;
            nodes[2] = node1;
            nodes[4] = node2;

            nodes[1] = Midpoint(node0, node1);
            nodes[3] = Midpoint(node1, node2);
            nodes[5] = Midpoint(node2, node0);

            return nodes;
        }

        public static Point2D[] BuildMatrixTriangle9(Point2D node0, Point2D node1, Point2D node2)
        {
            var nodes = new Point2D[9];

            nodes[0] = node0;

            // Edge from center to surfaceA
            nodes[1] = PointAlong(node0, node1, 1.0 / 3.0);
            nodes[2] = PointAlong(node0, node1, 2.0 / 3.0);

            nodes[3] = node1;

            // Curved fiber surface edge from surfaceA to surfaceB
            nodes[4] = PointAlong(node1, node2, 1.0 / 3.0);
            nodes[5] = PointAlong(node1, node2, 2.0 / 3.0);

            nodes[6] = node2;

            // Two internal nodes for 9-node triangle
            nodes[7] = PointAlong(node2, node0, 1.0 / 3.0);
            nodes[8] = PointAlong(node2, node0, 2.0 / 3.0);

            return nodes;
        }

        protected static Point2D[] BuildMatrixTriangle3WithFiberEdge(
            Point2D[] fiberNodes, Point2D boundaryPoint, bool isEdgeCCW)
        {
            if (isEdgeCCW)
                return BuildMatrixTriangle3(fiberNodes[2], fiberNodes[4], boundaryPoint);

            return BuildMatrixTriangle3(fiberNodes[4], fiberNodes[2], boundaryPoint);
        }

        protected static Point2D[] BuildMatrixTriangle6WithFiberEdge(
            Point2D[] fiberNodes, Point2D boundaryPoint, bool isEdgeCCW)
        {
            Point2D node0;
            Point2D node1;

            if (isEdgeCCW)
            {
                node0 = fiberNodes[2];
                node1 = fiberNodes[4];
            }
            else
            {
                node0 = fiberNodes[4];
                node1 = fiberNodes[2];
            }

            return new[]
            {
        node0,
        fiberNodes[3],
        node1,
        Midpoint(node1, boundaryPoint),
        boundaryPoint,
        Midpoint(boundaryPoint, node0)
    };
        }

        protected static Point2D[] BuildMatrixTriangle9WithFiberEdge(
            Point2D[] fiberNodes, Point2D boundaryPoint, bool isEdgeCCW)
        {
            Point2D node0;
            Point2D node1;
            Point2D curved1;
            Point2D curved2;

            if (isEdgeCCW)
            {
                node0 = fiberNodes[3];
                curved1 = fiberNodes[4];
                curved2 = fiberNodes[5];
                node1 = fiberNodes[6];
            }
            else
            {
                node0 = fiberNodes[6];
                curved1 = fiberNodes[5];
                curved2 = fiberNodes[4];
                node1 = fiberNodes[3];
            }

            return new[]
            {
        node0,
        curved1,
        curved2,
        node1,
        PointAlong(node1, boundaryPoint, 1.0 / 3.0),
        PointAlong(node1, boundaryPoint, 2.0 / 3.0),
        boundaryPoint,
        PointAlong(boundaryPoint, node0, 1.0 / 3.0),
        PointAlong(boundaryPoint, node0, 2.0 / 3.0)
    };
        }

        #endregion

        #region Element Geometry Builders: Matrix Quad


        public static Point2D[] BuildMatrixQuad6(Point2D[] fiber1Nodes,
            Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = new Point2D[6];

            if (isEdgeCCW)
            {
                nodes[0] = fiber1Nodes[4];
                nodes[1] = fiber1Nodes[3];
                nodes[2] = fiber1Nodes[2];

                nodes[3] = fiber2Nodes[4];
                nodes[4] = fiber2Nodes[3];
                nodes[5] = fiber2Nodes[2];
            }
            else
            {
                nodes[0] = fiber2Nodes[4];
                nodes[1] = fiber2Nodes[3];
                nodes[2] = fiber2Nodes[2];

                nodes[3] = fiber1Nodes[4];
                nodes[4] = fiber1Nodes[3];
                nodes[5] = fiber1Nodes[2];
            }

            return nodes;
        }

        public static Point2D[] BuildMatrixQuad8(Point2D[] fiber1Nodes,
            Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var nodes = new Point2D[8];

            if (isEdgeCCW)
            {
                nodes[0] = fiber1Nodes[4];
                nodes[1] = fiber1Nodes[3];
                nodes[2] = fiber1Nodes[2];

                nodes[4] = fiber2Nodes[4];
                nodes[5] = fiber2Nodes[3];
                nodes[6] = fiber2Nodes[2];
            }
            else
            {
                nodes[0] = fiber2Nodes[4];
                nodes[1] = fiber2Nodes[3];
                nodes[2] = fiber2Nodes[2];

                nodes[4] = fiber1Nodes[4];
                nodes[5] = fiber1Nodes[3];
                nodes[6] = fiber1Nodes[2];
            }

            nodes[3] = Midpoint(nodes[2], nodes[4]);
            nodes[7] = Midpoint(nodes[0], nodes[6]);

            return nodes;
        }

        public static Point2D[] BuildMatrixQuad9(Point2D[] fiber1Nodes,
            Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var q8 = BuildMatrixQuad8(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            var nodes = new Point2D[9];

            for (int i = 0; i < 8; i++)
                nodes[i] = q8[i];

            //nodes[8] = Centroid(q8[0], q8[2], q8[4], q8[6]);
            nodes[8] = PointAlong(q8[1], q8[5], 0.5);
            return nodes;
        }

        public static Point2D[] BuildMatrixQuad12(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var q8 = BuildMatrixQuad8(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            var nodes = new Point2D[12];

            nodes[0] = q8[0];
            nodes[1] = PointAlong(q8[0], q8[2], 1.0 / 3.0);
            nodes[2] = PointAlong(q8[0], q8[2], 2.0 / 3.0);
            nodes[3] = q8[2];

            nodes[4] = PointAlong(q8[2], q8[4], 1.0 / 3.0);
            nodes[5] = PointAlong(q8[2], q8[4], 2.0 / 3.0);
            nodes[6] = q8[4];

            nodes[7] = PointAlong(q8[4], q8[6], 1.0 / 3.0);
            nodes[8] = PointAlong(q8[4], q8[6], 2.0 / 3.0);
            nodes[9] = q8[6];

            nodes[10] = PointAlong(q8[6], q8[0], 1.0 / 3.0);
            nodes[11] = PointAlong(q8[6], q8[0], 2.0 / 3.0);

            return nodes;
        }

        public static Point2D[] BuildMatrixQuad16(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW)
        {
            var q12 = BuildMatrixQuad12(fiber1Nodes, fiber2Nodes, isEdgeCCW);
            var nodes = new Point2D[16];

            for (int i = 0; i < 12; i++)
                nodes[i] = q12[i];

            var c0 = q12[0];
            var c1 = q12[3];
            var c2 = q12[6];
            var c3 = q12[9];

            nodes[12] = BilinearPoint(c0, c1, c2, c3, 1.0 / 3.0, 1.0 / 3.0);
            nodes[13] = BilinearPoint(c0, c1, c2, c3, 2.0 / 3.0, 1.0 / 3.0);
            nodes[14] = BilinearPoint(c0, c1, c2, c3, 2.0 / 3.0, 2.0 / 3.0);
            nodes[15] = BilinearPoint(c0, c1, c2, c3, 1.0 / 3.0, 2.0 / 3.0);

            return nodes;
        }
        #endregion


        #region Element Geometry Helpers


        public static Point2D FiberSurfaceArcPoint(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius, double t)
        {
            double theta1 = Math.Atan2(surfaceNode1.Y - fiberCenter.Y,
                surfaceNode1.X - fiberCenter.X);

            double theta2 = Math.Atan2(surfaceNode2.Y - fiberCenter.Y,
                surfaceNode2.X - fiberCenter.X);

            double dTheta = theta2 - theta1;

            if (dTheta > Math.PI)
                dTheta -= 2.0 * Math.PI;
            else if (dTheta < -Math.PI)
                dTheta += 2.0 * Math.PI;

            double theta = theta1 + t * dTheta;

            return new Point2D(
                fiberCenter.X + fiberRadius * Math.Cos(theta),
                fiberCenter.Y + fiberRadius * Math.Sin(theta));
        }

        public static Point2D PointAlong(Point2D a, Point2D b, double t)
        {
            return new Point2D(
                a.X + t * (b.X - a.X),
                a.Y + t * (b.Y - a.Y));
        }

        public static Point2D BilinearPoint(Point2D c0, Point2D c1, Point2D c2, Point2D c3, double xi, double eta)
        {
            double x =
                (1.0 - xi) * (1.0 - eta) * c0.X +
                xi * (1.0 - eta) * c1.X +
                xi * eta * c2.X +
                (1.0 - xi) * eta * c3.X;

            double y =
                (1.0 - xi) * (1.0 - eta) * c0.Y +
                xi * (1.0 - eta) * c1.Y +
                xi * eta * c2.Y +
                (1.0 - xi) * eta * c3.Y;

            return new Point2D(x, y);
        }

        public static Point2D Midpoint(Point2D a, Point2D b)
        {
            return new Point2D(0.5 * (a.X + b.X), 0.5 * (a.Y + b.Y));
        }

        public static Point2D Centroid(params Point2D[] points)
        {
            return new Point2D(points.Average(p => p.X), points.Average(p => p.Y));
        }

        public static Point2D FiberSurfaceMidpoint(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius)
        {
            var midpoint = Midpoint(surfaceNode1, surfaceNode2);
            var vector = MathHelper.MakeVector2D(fiberCenter, midpoint);
            double angle = Math.Atan2(vector.Y, vector.X);

            return new Point2D(
                fiberCenter.X + fiberRadius * Math.Cos(angle),
                fiberCenter.Y + fiberRadius * Math.Sin(angle));
        }

        public static double DistanceSquared(Point2D a, Point2D b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Determines the node ordering for a curved fiber element (6 nodes).
        /// Based on MATLAB FE_Mesh_2D.DetermineFiberNodeOrder (lines 286-305).
        /// </summary>
        private static Point2D[] DetermineFiberNodeOrder(Point2D fiberCenter, Point2D node1, Point2D node2, double fiberRadius,
            bool isEdgeCCW)
        {
            var nodes = new Point2D[6];
            nodes[0] = fiberCenter;

            if (isEdgeCCW)
            {
                nodes[2] = node2;
                nodes[4] = node1;
            }
            else
            {
                nodes[2] = node1;
                nodes[4] = node2;
            }

            // Midpoint nodes
            nodes[1] = new Point2D((nodes[0].X + nodes[2].X) / 2.0, (nodes[0].Y + nodes[2].Y) / 2.0);
            nodes[5] = new Point2D((nodes[0].X + nodes[4].X) / 2.0, (nodes[0].Y + nodes[4].Y) / 2.0);

            // Middle node on fiber surface
            Point2D midPointBetweenNodes2And4 = new Point2D(
                (nodes[2].X + nodes[4].X) / 2.0,
                (nodes[2].Y + nodes[4].Y) / 2.0);

            var midPointVector = MathHelper.MakeVector2D(fiberCenter, midPointBetweenNodes2And4);
            double midPointAngle = Math.Atan2(midPointVector.Y, midPointVector.X);

            nodes[3] = new Point2D(
                fiberCenter.X + fiberRadius * Math.Cos(midPointAngle),
                fiberCenter.Y + fiberRadius * Math.Sin(midPointAngle));

            return nodes;
        }

        #endregion
    }
}