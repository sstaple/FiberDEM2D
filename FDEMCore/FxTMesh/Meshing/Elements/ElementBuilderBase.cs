using FDEMCore.FxTMesh.Geometry;
using System;
using System.Linq;

namespace FDEMCore.FxTMesh.Meshing.Elements
{
    public abstract class ElementBuilderBase : IElementBuilder
    {
        public abstract ElementBuildResult BuildInteriorMatrixTriangle(Point2D node0, Point2D node1, Point2D node2);

        public abstract ElementBuildResult BuildFiberTriangle(Point2D fiberCenter, Point2D surfaceNode1,
            Point2D surfaceNode2, double fiberRadius);

        public abstract ElementBuildResult BuildMatrixQuad(Point2D[] fiber1Nodes, Point2D[] fiber2Nodes, bool isEdgeCCW);

        public abstract ElementBuildResult BuildFiberBoundaryMatrixTriangle(Point2D[] fiberNodes, Point2D boundaryPoint);

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
            Point2D node2, double fiberRadius)
        {
            var nodes = new Point2D[6];

            nodes[0] = fiberCenter;
            nodes[2] = node2;
            nodes[4] = node1;

            nodes[1] = Midpoint(nodes[0], nodes[2]);
            nodes[5] = Midpoint(nodes[0], nodes[4]);
            nodes[3] = FiberSurfaceArcPoint(fiberCenter, nodes[2], nodes[4], fiberRadius, 0.5);

            //Check CCW orientation and reverse if necessary
            nodes = EnsureCcwTriangle6(nodes);
            return nodes;
        }

        public static Point2D[] BuildFiberTriangle9(Point2D fiberCenter, Point2D node1,
            Point2D node2, double fiberRadius)
        {
            var nodes = new Point2D[9];

            nodes[0] = fiberCenter;

            Point2D surfaceA;
            Point2D surfaceB;
            surfaceA = node2;
            surfaceB = node1;
            
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

            //Check CCW orientation and reverse if necessary
            nodes = EnsureCcwTriangle9(nodes);
            return nodes;
        }

        #endregion

        #region Element Geometry Builders: Matrix Triangles


        public static Point2D[] BuildMatrixTriangle3(Point2D node0, Point2D node1, Point2D node2)
        {
            Point2D [] nodes = EnsureCcwTriangle3(new[] { node0, node1, node2 });
            return nodes;
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

            nodes = EnsureCcwTriangle6(nodes);
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

            nodes = EnsureCcwTriangle9(nodes);
            return nodes;
        }

        protected static Point2D[] BuildMatrixTriangle3WithFiberEdge(
            Point2D[] fiberNodes, Point2D boundaryPoint)
        {
            Point2D[] nodes = BuildMatrixTriangle3(fiberNodes[2], fiberNodes[4], boundaryPoint);
                    //: BuildMatrixTriangle3(fiberNodes[4], fiberNodes[2], boundaryPoint);
            nodes = EnsureCcwTriangle3(nodes);
            return nodes;
        }

        protected static Point2D[] BuildMatrixTriangle6WithFiberEdge(
            Point2D[] fiberNodes, Point2D boundaryPoint)
        {
            Point2D node0;
            Point2D node1;

                node0 = fiberNodes[2];
                node1 = fiberNodes[4];
            

            Point2D[] nodes = new[]{node0, fiberNodes[3], node1,  Midpoint(node1, boundaryPoint), 
                boundaryPoint, Midpoint(boundaryPoint, node0)};

            return EnsureCcwTriangle6(nodes);
        }

        protected static Point2D[] BuildMatrixTriangle9WithFiberEdge(
            Point2D[] fiberNodes, Point2D boundaryPoint)
        {
            Point2D node0;
            Point2D node1;
            Point2D curved1;
            Point2D curved2;

                node0 = fiberNodes[3];
                curved1 = fiberNodes[4];
                curved2 = fiberNodes[5];
                node1 = fiberNodes[6];
           
            Point2D[] nodes = new[]{node0,curved1,curved2,node1,PointAlong(node1, boundaryPoint, 1.0 / 3.0),PointAlong(node1, boundaryPoint, 2.0 / 3.0),
                boundaryPoint,PointAlong(boundaryPoint, node0, 1.0 / 3.0),PointAlong(boundaryPoint, node0, 2.0 / 3.0)};

            return EnsureCcwTriangle9(nodes);
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

                nodes[3] = fiber1Nodes[2];
                nodes[4] = fiber1Nodes[3];
                nodes[5] = fiber1Nodes[4];
            }
            
            return EnsureCcwQuad6(nodes); 
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

            return EnsureCcwQuad8(nodes); 
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
            return EnsureCcwQuad9(nodes);
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

            return EnsureCcwQuad12(nodes);
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

            return EnsureCcwQuad16(nodes);
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

        protected static Point2D[] EnsureCcw(Point2D[] nodes, int[] cornerMap, int[] reverseMap)
        {
            Point2D[] corners = cornerMap.Select(i => nodes[i]).ToArray();

            if (MeshBuilder.SignedArea2(corners) >= 0.0)
                return nodes;

            Point2D[] fixedNodes = reverseMap.Select(i => nodes[i]).ToArray();

            return fixedNodes;
        }

        public static Point2D[] EnsureCcwTriangle3(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 1, 2 }, new[] { 0, 2, 1 });
            return nodes;
        }
        public static Point2D[] EnsureCcwTriangle6(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 2, 4 }, new[] { 0, 5, 4, 3, 2, 1 });
            return nodes;
        }
        public static Point2D[] EnsureCcwTriangle9(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 3, 6 }, new[] { 0, 8, 7, 6, 5, 4, 3, 2, 1 });
            return nodes;
        }
        public static Point2D[] EnsureCcwQuad6(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 2, 3, 5 }, new[] { 0, 5,4,3,2,1 });
            return nodes;
        }
        public static Point2D[] EnsureCcwQuad8(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 2, 4, 6 }, new[] { 0, 7, 6, 5, 4, 3, 2, 1 });
            return nodes;
        }
        public static Point2D[] EnsureCcwQuad9(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 2, 4, 6 }, new[] { 0, 7, 6, 5, 4, 3, 2, 1, 8 });
            return nodes;
        }
        public static Point2D[] EnsureCcwQuad12(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 3, 6, 9 }, new[] { 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 });
            return nodes;
        }
        public static Point2D[] EnsureCcwQuad16(Point2D[] nodes)
        {
            nodes = EnsureCcw(nodes, new[] { 0, 3, 6, 9 }, new[] { 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 15, 14, 13, 12 });
            return nodes;
        }
        #endregion
    }
}