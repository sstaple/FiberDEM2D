using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using DelaunatorSharp;

namespace MicroCluster
{
    internal class Microstructure
    {
        #region Public Members
        // Delaunator points 
        public IPoint[]? YZPoints { get; private set; }
        // Trianglaution
        public Delaunator? Triangulation {  get; private set; }
        public List<Triangle>? Triangles { get; private set; }
        // Vf thershold
        public double[]? Threshold { get; private set; }
        // total number of FC
        public static int NumFC { get; set; }
        // total number of MRC
        public static int NumMRC { get; set; }
        // Vf median
        public double VfMdn { get; private set; }
        // Vf IQR
        public double VfIqr { get; private set; }
        // FC Area Density
        public double FCDensity { get; private set; }
        // MRC Area Density
        public double MRCDensity { get; private set; }
        // Number of FC Density
        public double FCNumDensity { get; private set; }
        // Number of MRC Density
        public double MRCNumDensity {  get; private set; }
        public string? FilePath { get; set; }
        // Pack file name
        public string? PackFileName { get; set; }
        // Save Directory
        public string? SaveDirectory { get; set; }
        // Y vals
        public List<double> Y { get; private set; } = new List<double>();
        // Z vals
        public List<double> Z { get; private set; } = new List<double>();
        // R vals
        public List<double> R { get; private set; } = new List<double>();
        // Y bound
        public double YBoundary { get; private set; }
        // Z bound
        public double ZBoundary { get; private set; }
        // basefilename
        public string BaseName { get; set; }
        private OutputOptions _outputOptions;  
        #endregion

        public Microstructure(OutputOptions outputOptions, string filePath,string packFileName,string saveDirectory,List<double> y,List<double> z,List<double> r,double yBoundary, double zBoundary) 
        {
            // Initalize a bunch of variables
            _outputOptions = outputOptions;
            FilePath = filePath;
            PackFileName = packFileName;
            SaveDirectory = saveDirectory;
            Y = y;
            Z = z;
            R = r;
            YBoundary = yBoundary; 
            ZBoundary = zBoundary;
            BaseName = Path.GetFileNameWithoutExtension(PackFileName);

            // Construct triangulation, build triangles objects, and start initial assignments as well as threshold volume fractions
            ConstructTriangulation();

            // Set initial Assignments basde on vf
            InitialAssignments(Triangles,Threshold);

            // Smoothing
            int nSmoothingRounds = 10;
            SmoothClusters(Triangles, nSmoothingRounds);

            // Geometric measurements 
            // For Fiber Clusters (FC)
            ComputeClusterStatistics(Triangles, forFC: true, out double fcdensity, out double fcnumdensity);

            // For Matrix Rich Clusters (MRC)
            ComputeClusterStatistics(Triangles, forFC: false, out double mrcdensity, out double mrcnumdensity);

            // local Vf Calcualtions
            ComputeMedianAndIQR(Triangles, out double vfmdn, out double vfiqr);

            // Assign all vars
            VfMdn = vfmdn;
            VfIqr = vfiqr;
            FCDensity = fcdensity;
            MRCDensity = mrcdensity;
            FCNumDensity = fcnumdensity;
            MRCNumDensity = mrcnumdensity;

        }

        private void ConstructTriangulation()
        {
            // Strat by reflecting points over boudaries
            GenerateTiledLists(Y,Z,R,YBoundary,ZBoundary, 
                                           out List<double> tiledY,
                                           out List<double> tiledZ,
                                           out List<double> tiledR);


            // Triangulate all those points - verified
            IPoint[] tiledPoints = Utilities.ConvertListToColumnArray(tiledY, tiledZ);
            // Triangulate the tiled points using DelaunatorSharp.
            Delaunator triangulation = new Delaunator(tiledPoints);
            Triangulation = triangulation;
            YZPoints = tiledPoints;


            // Find only triangules in original boundary - verified
            int[] keptTriangleIDs = FilterTrianglesWithinBoundary(YZPoints, Triangulation.Triangles,0, YBoundary, 0, ZBoundary);

            // Create list of triangles 
            Triangles = Triangle.CreateTriangleObjects(YZPoints, keptTriangleIDs, tiledR, Triangulation.Triangles);

            // Find and assign neighboring triangles
            Triangle.AssignSharedBoundaryTriangles(Triangles, Triangulation.Halfedges);

            // Gather all volume fractions
            var vols = Triangles.Select(t => t.VolumeFraction).ToArray();

            // Compute 2 thresholds (for 3 categories e.g.)
            var thresh = Utilities.MultiThresholdOtsu(vols, numThresholds: 2);
            Threshold = thresh;

            // debug files
            //string TiledPosPath = PackFile.SaveDirectory + "\\TiledPos.csv";
            //string ConnectivityPath = PackFile.SaveDirectory + "\\TiledCon.csv";
            //string filteredPath = PackFile.SaveDirectory + "\\filteredCon.csv";
            var diskPointIndices = keptTriangleIDs.SelectMany(t => new[]
            {
                Triangulation.Triangles[3*t],
                Triangulation.Triangles[3*t + 1],
                Triangulation.Triangles[3*t + 2]
            })
            .Distinct()
            .ToArray();
            
            // Paraview Writing Options
            if (_outputOptions.SaveParaviewFibers)
            {
                // filename
                string paraFibers = SaveDirectory + "\\" + BaseName + "_ParaviewFibers.vtu";
                // Save ParaView visualization files
                ParaViewWriter.WriteDisks(paraFibers, tiledY, tiledZ, tiledR, Triangles);
            }
        }

        private void InitialAssignments(List<Triangle>? triangles, double[]? Threshold)
        {
            // Gather all volume fractions
            var vols = triangles.Select(t => t.VolumeFraction).ToArray();

            // Compute Lower Quartile
            double q1 = Utilities.ComputeLowerQuartile(vols);
            //Console.WriteLine($"Lower Quartile (Q1): {q1:F4}");
            //Console.WriteLine($"Lower Otsu Threshold: {Threshold[0]:F4}");
            // determine which triangles are below and assign as permanent MRC
            bool[] lowMask = Utilities.IsBelowLowerQuartile(vols);

            // Assign boolean to permanent MRC
            for (int i = 0; i < triangles.Count; i++)
            {
                triangles[i].PermanentMRC = lowMask[i];
                triangles[i].IsMRC = lowMask[i];
            }

            // Now need assignments for all FC and MRC
            double lowerThresh = Threshold[0];
            double upperThresh = Threshold[1];

            // Loop through and initially assign
            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                tri.IsMRC = tri.VolumeFraction < lowerThresh;
                tri.IsFC = tri.VolumeFraction > upperThresh;

                // set other variables
                tri.NewIsFCAferSmoothing = tri.IsFC;
                tri.NewIsMRCAfterSmoothing = tri.IsMRC;
            }

            // initial cluster counting 
            AddFC(triangles);
            AddMRC(triangles);

            if (_outputOptions.SaveParaviewClustersAtEveryStep)
            {
                // filename
                string clusterspath = SaveDirectory + "\\" + BaseName + "_Clusters_0.vtu";
                // Save ParaView visualization files
                ParaViewWriter.WriteTrianglesWithClusters(clusterspath, triangles); 
            }

        }

        private void GenerateTiledLists(List<double> origY, List<double> origZ, List<double> origR, double yBoundary, double zBoundary, out List<double> newY, out List<double> newZ, out List<double> newR)
        {
            // Create new lists to store the translated points.
            newY = new List<double>();
            newZ = new List<double>();
            newR = new List<double>();

            // Define the offsets for the grid.
            double[] yOffsets = new double[] { -yBoundary-10, 0, yBoundary+10};
            double[] zOffsets = new double[] { -zBoundary-10, 0, zBoundary+10};

            // Loop through every combination of y and z offsets.
            foreach (double dy in yOffsets)
            {
                foreach (double dz in zOffsets)
                {
                    // For each original point, add a translated copy.
                    for (int i = 0; i < origY.Count; i++)
                    {
                        newY.Add(origY[i] + dy);
                        newZ.Add(origZ[i] + dz);
                        newR.Add(origR[i]); // Radius remains unchanged.
                    }
                }
            }
        }

        public int[] FilterTrianglesWithinBoundary( IPoint[] points, int[] delaunayTriangles, double minY, double maxY, double minZ, double maxZ)
        {
            var kept = new List<int>();
            int triCount = delaunayTriangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                int i0 = delaunayTriangles[3 * t];
                int i1 = delaunayTriangles[3 * t + 1];
                int i2 = delaunayTriangles[3 * t + 2];

                var p0 = points[i0];
                var p1 = points[i1];
                var p2 = points[i2];

                bool inside =
                    p0.X >= minY && p0.X <= maxY && p0.Y >= minZ && p0.Y <= maxZ &&
                    p1.X >= minY && p1.X <= maxY && p1.Y >= minZ && p1.Y <= maxZ &&
                    p2.X >= minY && p2.X <= maxY && p2.Y >= minZ && p2.Y <= maxZ;

                if (inside)
                    kept.Add(t);
            }

            kept.Sort();       // ensure ascending order of triangle IDs
            return kept.ToArray();
        }

        private void AddFC(List<Triangle> tris)
        {
            int fcNum = 1;
            // Reset all FC assignments
            foreach (var tri in tris)
                tri.ResetFCAssignment();

            // Flood‐fill each unassigned FC triangle
            foreach (var tri in tris)
            {
                if (tri.IsFC && !tri.IsAssignedFC)
                {
                    tri.AssignFC(fcNum, tris);
                    fcNum++;
                }
            }
            NumFC = fcNum - 1;
        }

        private void AddMRC(List<Triangle> tris)
        {
            int mrcNum = 1;
            foreach (var tri in tris)
                tri.ResetMRCAssignment();

            foreach (var tri in tris)
            {
                if (tri.IsMRC && !tri.IsAssignedMRC)
                {
                    tri.AssignMRC(mrcNum, tris);
                    mrcNum++;
                }
            }
            NumMRC = mrcNum - 1;
        }

        private void SmoothClusters(List<Triangle> tris,int nSmoothingRounds)
        {
            for (int i = 0; i < nSmoothingRounds; i++)
            {
                // Smooth by subtracting and adding 
                foreach (var tri in tris)
                {
                    tri.SmoothBySubtractingAndAddingFC(tris);
                    tri.SmoothBySubtractingAndAddingMRC(tris);
                }
                // reset cluster assignments
                foreach (var tri in tris)
                {
                    tri.ResetFCAssignment();
                    tri.ResetMRCAssignment();
                }
                // Smooth by subtracting 
                foreach (var tri in tris)
                {
                    tri.SmoothBySubtractingFC(tris);
                    tri.SmoothBySubtractingMRC(tris);
                }
               
                // Renumber and reassign
                AddFC(tris);
                AddMRC(tris);

                // paraview writing
                if (_outputOptions.SaveParaviewClustersAtEveryStep)
                {
                    // filename
                    string clusterspath = SaveDirectory + "\\" + BaseName + $"_Clusters_{i + 1}.vtu";
                    // Save ParaView visualization files
                    ParaViewWriter.WriteTrianglesWithClusters(clusterspath, tris);
                }
                
            }

            // Smooth unconnected matrix triangles
            foreach (var tri in tris)
                tri.SmoothUnconnectedMRC(tris);

            // Just in case, make sure that a triangle cannot be assigned both FC and MRC
            foreach (var tri in tris)
                tri.ResetOverlapRegions(tris);

            foreach (var tri in tris)
            {
                tri.ResetFCAssignment();
                tri.ResetMRCAssignment();
            }
            // Renumber and reassign
            AddFC(tris);
            AddMRC(tris);


            if (_outputOptions.SaveParaviewClusters)
            {
                // filename
                string finalclusterspath = SaveDirectory + "\\" + BaseName + $"_Final_Clusters.vtu";
                // Save ParaView visualization files
                ParaViewWriter.WriteTrianglesWithClusters(finalclusterspath, tris);
                
            }
            

        }

        public void ComputeClusterStatistics(List<Triangle> triangles, bool forFC,
                                             out double areaFraction, out double clusterDensity)
        {
            double clusterArea = 0.0;
            double totalArea = 0.0;
            var clusterNumbers = new HashSet<int>();

            foreach (var tri in triangles)
            {
                // Compute total area
                totalArea += Utilities.ComputeTriangleArea(tri.VertexCoordinates);

                // FC or MRC specific logic
                bool isClusterMember = forFC ? tri.IsFC : tri.IsMRC;
                int clusterID = forFC ? tri.FCNumber : tri.MRCNumber;

                if (isClusterMember)
                {
                    clusterArea += Utilities.ComputeTriangleArea(tri.VertexCoordinates);
                    if (clusterID > 0)
                        clusterNumbers.Add(clusterID);
                }
            }

            // Calculate area fraction
            areaFraction = (totalArea > 0.0) ? (clusterArea / totalArea) : 0.0;

            // Number of fibers = number of points in original microstructure
            int numFibers = Y.Count;

            // Calculate cluster density (clusters per fiber)
            clusterDensity = (numFibers > 0) ? ((double)clusterNumbers.Count / numFibers) : 0.0;
        }

        public void ComputeMedianAndIQR(List<Triangle> triangles, out double median, out double iqr)
        {
            // Gather all volume fractions
            var volFracs = triangles.Select(t => t.VolumeFraction).OrderBy(v => v).ToArray();

            if (volFracs.Length == 0)
            {
                median = 0;
                iqr = 0;
                return;
            }

            // Median
            median = ComputePercentile(volFracs, 50);

            // First Quartile (Q1) - 25th percentile
            double q1 = ComputePercentile(volFracs, 25);

            // Third Quartile (Q3) - 75th percentile
            double q3 = ComputePercentile(volFracs, 75);

            // IQR = Q3 - Q1
            iqr = q3 - q1;
        }

        private double ComputePercentile(double[] sortedArray, double percentile)
        {
            if (sortedArray == null || sortedArray.Length == 0)
                return 0.0;

            double pos = (percentile / 100.0) * (sortedArray.Length - 1);
            int lowerIndex = (int)Math.Floor(pos);
            int upperIndex = (int)Math.Ceiling(pos);

            if (lowerIndex == upperIndex)
                return sortedArray[lowerIndex];

            double fraction = pos - lowerIndex;
            return sortedArray[lowerIndex] + fraction * (sortedArray[upperIndex] - sortedArray[lowerIndex]);
        }
    }
}
