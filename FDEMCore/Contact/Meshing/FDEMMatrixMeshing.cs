using DelaunatorSharp;
using RandomMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDEMCore.Contact.Meshing
{
    internal static class FDEMMatrixMeshing
    {
        #region Static Methods for creation

        static public void CreateMatrixPairs(ref List<Fiber> lFibers, ref List<FToFRelation> lSprings,
                                             ref List<MatrixProjectedFiber> lMatrixProjFibers,
                                             CellBoundary cb, ContactParameters conParams, MatrixAssemblyParameters matrixParams)
        {

            CellWall[] cw = cb.Walls;

            //Erase any old springs, so that only matrix is active.
            lSprings = new List<FToFRelation>();
            int n = lFibers.Count;

            MyPoint[] myPts = AddAllFiberProjectionsToPoints(lFibers, cw);

            //Now do the triangulation, and extract all of the pairs
            var triangulation = new Delaunator(myPts);
            List<int[]> pairs = ExtractIndicesOfPairs(triangulation);

            //Debugging: draw triangulation
            //OutputAllTriangulation(myPts, pairs, "E:\\GoogleDrive\\IFAM\\Projects\\FDEM\\CompositeModel\\Validation_LinearElastic\\Test_ToDeleteAfter\\Connections.csv");
            //Debugging:this should have all of the many fibers!!!
            //OutputFiberPositionsInPackFile(lFibers, "E:\\GoogleDrive\\IFAM\\Projects\\FDEM\\CompositeModel\\Validation_LinearElastic\\Test_ToDeleteAfter\\Position2.csv");
            //RandomPack.WriteFinalFiberPositions("E:\\GoogleDrive\\IFAM\\Projects\\FDEM\\CompositeModel\\Validation_LinearElastic\\Test_ToDeleteAfter\\Position2.csv", lFibers, cb, true, false, false, TimeSpan.Zero);


            //Sort them all: make the smaller index first
            foreach (int[] iArray in pairs)
            {
                Array.Sort(iArray);
            }
            //Get rid of duplicate entries just in case
            pairs = pairs.Distinct().ToList();

            //now find the original connections
            for (int i = 0; i < pairs.Count; i++)
            {

                if (pairs[i][0] < n)
                {

                    //Original RVE: make a spring!
                    if (pairs[i][1] < n)
                    {
                        lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[pairs[i][1]], pairs[i][0], pairs[i][1]));

                        lSprings[lSprings.Count - 1].AddNonContactSpring(matrixParams);
                    }
                    //Directly to the right (proj of cw[2]), index 1 for projections)
                    else if (pairs[i][1] >= n && pairs[i][1] < 2 * n)
                    {
                        int nFiber = pairs[i][1] - n;
                        lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 0));
                        //lFibers[nFiber].AddProjectedFiber(cw[2].PeriodicProjection, false, new int[] { 2 });
                        lFibers[nFiber].AddProjectedFiber(cw[2].PeriodicProjection, new int[] { 2 });
                        lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));

                        lSprings[lSprings.Count - 1].AddNonContactSpring(matrixParams);
                    }
                    //Directly above (proj of cw[4], index 3 for projections)
                    else if (pairs[i][1] >= 3 * n && pairs[i][1] < 4 * n)
                    {
                        int nFiber = pairs[i][1] - 3 * n;
                        lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 1));
                        lFibers[nFiber].AddProjectedFiber(cw[4].PeriodicProjection, new int[] { 4 });
                        lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));

                        lSprings[lSprings.Count - 1].AddNonContactSpring(matrixParams);
                    }
                    //above and to the right (proj of cw[2] + cw[4], index 5 for projections)
                    else if (pairs[i][1] >= 5 * n && pairs[i][1] < 6 * n)
                    {
                        int nFiber = pairs[i][1] - 5 * n;
                        lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 2));
                        double[] tempProj = VectorMath.Add(cw[2].PeriodicProjection, cw[4].PeriodicProjection);
                        lFibers[nFiber].AddProjectedFiber(tempProj, new int[] { 2, 4 });
                        lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));

                        lSprings[lSprings.Count - 1].AddNonContactSpring(matrixParams);
                    }
                    //above and to the left (proj of cw[3] + cw[4], index 7 for projections), ends up as index 3
                    else if (pairs[i][1] >= 7 * n && pairs[i][1] < 8 * n)
                    {
                        int nFiber = pairs[i][1] - 7 * n;
                        lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 3));
                        double[] tempProj = VectorMath.Add(cw[3].PeriodicProjection, cw[4].PeriodicProjection);
                        lFibers[nFiber].AddProjectedFiber(tempProj, new int[] { 3, 4 });
                        lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));

                        lSprings[lSprings.Count - 1].AddNonContactSpring(matrixParams);
                    }
                }
            }

            //Debugging: should be just the RVE though may have doubles
            //RandomPack.WriteFinalFiberPositions("E:\\GoogleDrive\\IFAM\\Projects\\FDEM\\CompositeModel\\Validation_LinearElastic\\Test_ToDeleteAfter\\Position3.csv", lFibers, cb, true, false, false, TimeSpan.Zero);

            //remove duplicates from lMatrixProjFibers, since multiple contacts can go from the same projected fiber
            for (int i = 0; i < lMatrixProjFibers.Count - 1; i++)
            {
                for (int j = i + 1; j < lMatrixProjFibers.Count; j++)
                {
                    if (lMatrixProjFibers[i].FiberIndex == lMatrixProjFibers[j].FiberIndex && lMatrixProjFibers[i].ProjectionIndex == lMatrixProjFibers[j].ProjectionIndex)
                    {
                        lMatrixProjFibers.RemoveAt(j);
                        j -= 1;
                    }
                }
            }
            //Debugging: should be perfect!!
            //RandomPack.WriteFinalFiberPositions("E:\\GoogleDrive\\IFAM\\Projects\\FDEM\\CompositeModel\\Validation_LinearElastic\\Test_ToDeleteAfter\\Position4.csv", lFibers, cb, true, false, false, TimeSpan.Zero);
        }
        /// <summary>
        /// This is just for debugging: it doesn't make any projections, but just keeps the original connections to make smaller RVEs for unit tests.
        /// </summary>
        static public void CreateMatrixPairs(ref List<Fiber> lFibers, ref List<FToFRelation> lSprings,
                                             ref List<MatrixProjectedFiber> lMatrixProjFibers,
                                             CellBoundary cb, ContactParameters conParams, MatrixAssemblyParameters matrixParams, bool dontMakeProjections)
        {
            CellWall[] cw = cb.Walls;

            //Erase any old springs, so that only matrix is active.
            lSprings = new List<FToFRelation>();
            int n = lFibers.Count;

            //put fiber positions into list
            List<double[]> points = new List<double[]>();
            AddAllFibersWithProjection(lFibers, ref points, 0.0, 0.0);

            //Now add the projected points
            //0=original, 1=right, 2=left, 3=top, 4=bottom, 5=top/right,
            //6=bottom/right, 7=top/left, 8=bottom/left

            //Normal projections
            for (int i = 2; i < cw.Length; i++)
            {

                AddAllFibersWithProjection(lFibers, ref points, cw[i].PeriodicProjection[1],
                                           cw[i].PeriodicProjection[2]);
            }
            //This adds the diagonals
            for (int i = 2; i < cw.Length - 2; i++)
            {
                for (int j = 4; j < cw.Length; j++)
                {
                    AddAllFibersWithProjection(lFibers, ref points, cw[i].PeriodicProjection[1] + cw[j].PeriodicProjection[1],
                                               cw[i].PeriodicProjection[2] + cw[j].PeriodicProjection[2]);
                }
            }

            //Do the triangulation:

            //Remove duplicates.  Just in case.
            points = points.Distinct().ToList();

            //Convert the points from a list to an array of myPoints
            MyPoint[] myPts = new MyPoint[points.Count];
            for (int l = 0; l < points.Count; l++)
            {
                myPts[l] = new MyPoint(points[l][0], points[l][1]);
            }

            //Now do the triangulation, and extract all of the pairs
            var triangulation = new Delaunator(myPts);
            List<int[]> pair = ExtractIndicesOfPairs(triangulation);


            //now find the original connections

            for (int i = 0; i < pair.Count; i++)
            {

                if (pair[i][0] < n)
                {

                    //Original RVE: make a spring!
                    if (pair[i][1] < n)
                    {
                        lSprings.Add(new FToFRelation(conParams, lFibers[pair[i][0]], lFibers[pair[i][1]], pair[i][0], pair[i][1]));

                        lSprings[lSprings.Count - 1].AddNonContactSpring(matrixParams);
                    }
                }
            }

            //remove duplicates from lMatrixProjFibers
            for (int i = 0; i < lMatrixProjFibers.Count - 1; i++)
            {
                for (int j = i + 1; j < lMatrixProjFibers.Count; j++)
                {
                    if (lMatrixProjFibers[i].FiberIndex == lMatrixProjFibers[j].FiberIndex && lMatrixProjFibers[i].ProjectionIndex == lMatrixProjFibers[j].ProjectionIndex)
                    {
                        lMatrixProjFibers.RemoveAt(j);
                        j -= 1;
                    }
                }
            }

        }
       
        static public MyPoint[] AddAllFiberProjectionsToPoints(List<Fiber> lFibers, CellWall[] cw)
        {
            //put fiber positions into list
            List<double[]> points = new List<double[]>();
            AddAllFibersWithProjection(lFibers, ref points, 0.0, 0.0);


            //Now add the projected points
            //0=original, 1=right, 2=left, 3=top, 4=bottom, 5=top/right,
            //6=bottom/right, 7=top/left, 8=bottom/left

            //Normal projections
            for (int i = 2; i < cw.Length; i++)
            {

                AddAllFibersWithProjection(lFibers, ref points, cw[i].PeriodicProjection[1],
                                           cw[i].PeriodicProjection[2]);
            }
            //This adds the diagonals
            for (int i = 2; i < cw.Length - 2; i++)
            {
                for (int j = 4; j < cw.Length; j++)
                {
                    AddAllFibersWithProjection(lFibers, ref points, cw[i].PeriodicProjection[1] + cw[j].PeriodicProjection[1],
                                               cw[i].PeriodicProjection[2] + cw[j].PeriodicProjection[2]);
                }
            }

            //Convert the points from a list to an array of myPoints
            MyPoint[] myPts = new MyPoint[points.Count];
            for (int l = 0; l < points.Count; l++)
            {
                myPts[l] = new MyPoint(points[l][0], points[l][1]);
            }

            return myPts;
        }

        static private void AddAllFibersWithProjection(List<Fiber> lFibers, ref List<double[]> vertices,
                                                       double projx, double projy)
        {

            foreach (Fiber f in lFibers)
            {
                vertices.Add(new double[2] { f.CurrentPosition[1] + projx, f.CurrentPosition[2] + projy });
            }
        }
        /// <summary>
        /// This is just for debugging: you can throw this out to get intermediate data
        /// </summary>
        static public void OutputFiberPositionsInPackFile(List<Fiber> lFibers, string fileName)
        {
            StreamWriter dataWrite = new StreamWriter(fileName);
            dataWrite.WriteLine("Y, Z, Radius");
            foreach (Fiber f in lFibers)
            {
                dataWrite.WriteLine(f.CurrentPosition[1] + "," + f.CurrentPosition[2] + "," + f.Radius);
            }
            foreach (Fiber f in lFibers)
            {
                if (f.HasProjectedFibers)
                {
                    foreach (ProjectedFiber projectedFiber in f.ProjectedFibers)
                    {
                        dataWrite.WriteLine(projectedFiber.CurrentPosition[1] + "," + projectedFiber.CurrentPosition[2] + "," + f.Radius);
                    }
                }
            }

            dataWrite.Close();
        }

        static public void OutputFiberPositionsInPackFile(List<double[]> vertices, double radius, string fileName)
        {
            StreamWriter dataWrite = new StreamWriter(fileName);
            dataWrite.WriteLine("Y, Z, Radius");
            foreach (double[] v in vertices)
            {
                dataWrite.WriteLine(v[0] + "," + v[1] + "," + radius);
            }

            dataWrite.Close();
        }

        static public void OutputAllTriangulation(List<double[]> vertices, List<int[]> connections, string fileName)
        {
            StreamWriter dataWrite = new StreamWriter(fileName);
            dataWrite.WriteLine("Y1, Z1, Y2, Z2");
            foreach (int[] con in connections)
            {
                dataWrite.WriteLine(vertices[con[0]][0] + "," + vertices[con[0]][1] + "," + vertices[con[1]][0] + "," + vertices[con[1]][1]);
            }

            dataWrite.Close();
        }
        static public void OutputAllTriangulation(MyPoint[] pts, List<int[]> connections, string fileName)
        {
            List<double[]> points = new List<double[]>();
            foreach (MyPoint point in pts)
            {
                points.Add(new double[] { point.X, point.Y });
            }
            OutputAllTriangulation(points, connections, fileName);
        }
        //This gets the "other point" in an edge from the triangulation.  See https://mapbox.github.io/delaunator/ for explanation
        public static int NextHalfEdge(int i)
        {
            int iNext = (i % 3 == 2) ? (i - 2) : i + 1;
            return iNext;
        }

        public static List<int[]> ExtractIndicesOfPairs(Delaunator triangulation)
        {
            List<int[]> pairs = new List<int[]>();

            for (int i = 0; i < triangulation.Triangles.Length; i++)
            {
                if (i > triangulation.Halfedges[i])
                {
                    int ip1 = triangulation.Triangles[i];
                    int ip2 = triangulation.Triangles[NextHalfEdge(i)];
                    pairs.Add(new int[2] { ip1, ip2 });
                }
            }
            return pairs;
        }
        #endregion
    }

    /// <summary>
	/// This is a little class needed for the Delaunator.  Kind of dumb actually, but whatever
	/// </summary>
	public class MyPoint : DelaunatorSharp.IPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public MyPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    
}
