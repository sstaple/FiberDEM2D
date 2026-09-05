using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DelaunatorSharp;

namespace Fiber2DRVEMetrics
{
    internal class Triangle
    {
        // The unique triangle number.
        public int TriangleNumber { get; set; }
        // An array of vertex coordinates.
        public IPoint[] VertexCoordinates { get; set; }
        // A list of vertex indices in the original triangulation.
        public List<int> VertexIndices { get; set; }
        // The computed or assigned volume fraction.
        public double VolumeFraction { get; set; }
        // A list of triangle numbers (IDs) that share a boundary with this triangle.
        public List<int> SharedBoundaryTriangles { get; set; }
        // Fiber cluster (FC) ID number
        public int FCNumber { get; set; } = -1;
        // Matrix-rich cluster (MRC) ID number
        public int MRCNumber { get; set; } = -1;
        // is assigned as Fiber cluster (FC)
        public bool IsFC { get; set; } = false;
        // is assigned as matrix-rich cluster (MRC)
        public bool IsMRC { get; set; } = false;
        // bool to decide if its a fiber cluster after smoothing
        public bool IsAssignedFC { get; set; } = false;
        // is assigned as matrix-rich cluster (MRC)
        public bool IsAssignedMRC { get; set; } = false;
        // bool to decide if its a fiber cluster after smoothing
        public bool NewIsFCAferSmoothing { get; set; } = false;
        // bool to decide if its a matrix-rich cluster after smoothing
        public bool NewIsMRCAfterSmoothing { get; set; } = false;
        // Matrix rich clusters are permanent if under lower quartile
        public bool PermanentMRC { get; set; } = false;
        public Triangle(
            int triangleNumber,
            IPoint[] vertexCoordinates,
            List<int> vertexIndices,
            double volumeFraction,
            List<int> sharedBoundaryTriangles)
        {
            TriangleNumber = triangleNumber;
            VertexCoordinates = vertexCoordinates;
            VertexIndices = vertexIndices;
            VolumeFraction = volumeFraction;
            SharedBoundaryTriangles = sharedBoundaryTriangles;
        }
        public override string ToString()
        {
            return $"Triangle {TriangleNumber}: Vertices [{string.Join(", ", VertexCoordinates.Select(v => v.ToString()))}], " +
                   $"Volume Fraction: {VolumeFraction}, Shared Boundaries: [{string.Join(", ", SharedBoundaryTriangles)}]";
        }

        // This method creates a list of TriangleObject from the filtered triangles
        public static List<Triangle> CreateTriangleObjects(IPoint[] points, int[] keptTriangleIDs, List<double> tiledR, int[] globalTriangles)     // pass in Delaunay.Triangles
        {
            var list = new List<Triangle>();
            foreach (int t in keptTriangleIDs)
            {
                int idx0 = globalTriangles[3 * t];
                int idx1 = globalTriangles[3 * t + 1];
                int idx2 = globalTriangles[3 * t + 2];

                var verts = new IPoint[] { points[idx0], points[idx1], points[idx2] };
                var fibR = new double[] { tiledR[idx0], tiledR[idx1], tiledR[idx2] };
                double vf = Utilities.CalculateLocalFiberVolumeFraction(verts, fibR);

                var tri = new Triangle(
                    triangleNumber: t,
                    vertexCoordinates: verts,
                    vertexIndices: new List<int> { idx0, idx1, idx2 },
                    volumeFraction: vf,
                    sharedBoundaryTriangles: new List<int>()
                );
                list.Add(tri);
            }
            return list;
        }

        public static void AssignSharedBoundaryTriangles(List<Triangle> filteredTris,int[] halfedges)
        {
            // Build a map: original triangle ID → index in filteredTris
            var idToFilteredIndex = filteredTris
                .Select((tri, idx) => (tri.TriangleNumber, idx))
                .ToDictionary(tuple => tuple.TriangleNumber, tuple => tuple.idx);

            foreach (var tri in filteredTris)
            {
                var newNeighbors = new List<int>();
                int baseEdge = tri.TriangleNumber * 3;

                for (int e = 0; e < 3; e++)
                {
                    int oppEdge = halfedges[baseEdge + e];
                    if (oppEdge >= 0)
                    {
                        int origNbrTriID = oppEdge / 3;
                        // Only keep neighbors that are in our filtered list
                        if (idToFilteredIndex.TryGetValue(origNbrTriID, out int filteredIdx))
                        {
                            newNeighbors.Add(filteredIdx);
                        }
                    }
                }

                tri.SharedBoundaryTriangles = newNeighbors;
            }
        }
        public void ResetFCAssignment()
        {
            IsAssignedFC = false;
            FCNumber = -1;
            IsFC = NewIsFCAferSmoothing;
        }
        public void ResetMRCAssignment()
        {
            IsAssignedMRC = false;
            MRCNumber = -1;
            if (PermanentMRC)
            {
                IsMRC = true; // Always retain IsMRC if it's permanent
            }
            else
            {
                IsMRC = NewIsMRCAfterSmoothing;
            }
        }
        public bool AssignFC(int fcNum, List<Triangle> triangles)
        {
            // if this tri qualifies and hasn’t yet been assigned…
            if (IsFC && !IsAssignedFC)
            {
                IsAssignedFC = true;           // ← mark it!
                FCNumber = fcNum;

                // flood to neighbors
                foreach (int nbr in SharedBoundaryTriangles)
                    triangles[nbr].AssignFC(fcNum, triangles);

                return true;
            }
            return false;
        }
        public bool AssignMRC(int mrcNum, List<Triangle> triangles)
        {
            if (IsMRC && !IsAssignedMRC)
            {
                IsAssignedMRC = true;          // ← mark it!
                MRCNumber = mrcNum;
                foreach (int nbr in SharedBoundaryTriangles)
                    triangles[nbr].AssignMRC(mrcNum, triangles);
                return true;
            }
            return false;
        }
        public void SmoothBySubtractingAndAddingFC(List<Triangle> triangles)
        {
            NewIsFCAferSmoothing = IsFC;

            // Count how many neighbors are FC
            int fcNeighborCount = SharedBoundaryTriangles.Count(nbrIdx => triangles[nbrIdx].IsFC);

            if (IsFC)
            {
                if (fcNeighborCount < 2)
                    NewIsFCAferSmoothing = false;
            }
            else
            {
                if (fcNeighborCount >= 2)
                    NewIsFCAferSmoothing = true;
            }
        }
        public void SmoothBySubtractingAndAddingMRC(List<Triangle> triangles)
        {
            NewIsMRCAfterSmoothing = IsMRC;

            // Count how many neighbors are MRC
            int mrcNeighborCount = SharedBoundaryTriangles.Count(nbrIdx => triangles[nbrIdx].IsMRC);

            if (IsMRC)
            {
                if (mrcNeighborCount < 2 && !PermanentMRC)
                    NewIsMRCAfterSmoothing = false;
            }
            else
            {
                if (mrcNeighborCount >= 2)
                    NewIsMRCAfterSmoothing = true;
            }
        }
        public void SmoothBySubtractingFC(List<Triangle> triangles)
        {
            NewIsFCAferSmoothing = IsFC;

            if (IsFC)
            {
                int fcNeighborCount = SharedBoundaryTriangles.Count(nbrIdx => triangles[nbrIdx].IsFC);

                if (fcNeighborCount < 2)
                    NewIsFCAferSmoothing = false;
            }
        }
        public void SmoothBySubtractingMRC(List<Triangle> triangles)
        {
            NewIsMRCAfterSmoothing = IsMRC;

            if (IsMRC)
            {
                int mrcNeighborCount = SharedBoundaryTriangles.Count(nbrIdx => triangles[nbrIdx].IsMRC);

                if (mrcNeighborCount < 2 && !PermanentMRC)
                    NewIsMRCAfterSmoothing = false;
            }
        }
        public void SmoothUnconnectedMRC(List<Triangle> triangles)
        {
            if (IsMRC)
            {
                int mrcNeighborCount = SharedBoundaryTriangles.Count(nbrIdx => triangles[nbrIdx].IsMRC);

                if (mrcNeighborCount == 0)
                {
                    NewIsMRCAfterSmoothing = false;
                    PermanentMRC = false;  // ✅ Unmark if it's unconnected
                }
                else
                {
                    NewIsMRCAfterSmoothing = true;
                }
            }
            else
            {
                NewIsMRCAfterSmoothing = false;
            }
        }
        public void ResetOverlapRegions(List<Triangle> triangles)
        {
            if (IsFC && IsMRC)
            {
                ResetMRCAssignment();
                IsMRC = false;
                NewIsMRCAfterSmoothing = false;
                MRCNumber = -1;

            }
        }

    }
}
