/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 6/10/2013
 * Time: 9:49 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using RandomMath;
using System.IO;

namespace FDEMCore
{
	public enum BoundaryType{Periodic, Solid, LinearDeformations}
    /// <summary>
    /// The Cell Boundary is for periodic boundary conditions, where there are no cell walls per se, but the wall is simply a boundary around which the fibers are projected.
    /// Input is the coordinate of the bottom right corner, the width, and the height.
    /// Output is a little different: position is width, height.  Meanwhile rotation is the shear angle
    /// Always 0,0 is in the bottom-left corner
    /// </summary>
    [SerializableAttribute] //This allows to make a deep copy fast
	public class CellBoundary: SolidObject
	{
		#region Private Members
		private double [] oDimensions; //Dim in x, dim in y, then dim in z (Dim = length)
		private double [] strainStep; //This is the current strain increment
		private CellWall [] walls; //Back W, Front W, Left W, Right W, Bottom W, Top W
		private List<double[]> strain; //This is all of the strains at all of the increments, Exx, Eyy, Ezz, Ezy, Exz, Exy
		public double [] currStrain; //Just the current total strain
		private double [] currStrainRate; //Just the current total strain
		public double [] currDisp; //This is the current total displacement.  It is divided into deltax, deltay, deltaz, theta_z, theta_y, theta_x
		private double [,] currDefGrad;
		private double [,] currTimeDerDefGrad;
		private double [,] currNormTransf; //Matrix needed to rotate the normals of each boundary
		private double oVol = 1; //This is one because I multiply by dimensions during constructor
		private double[] leftBottomBackCorner;
        #endregion

        #region Public Members
        public CellWall []  Walls {
			get { return walls; }
			set { walls = value; }
		}
		public double [] ODimensions {
			get { return oDimensions; }
		}
		/// <summary>
		/// This is to be accessed and changed when needed.  It is only used to update the position of the walls
		/// </summary>
		public double[] StrainStep {
			get { return strainStep; }
			set { strainStep = value; }
		}
		public double[] StrainRate {
			get { return currStrainRate; }
			set { currStrainRate = value;}
		}
		//Output to be saved
		public List<double[]> Strain {
			get { return strain; }
		}
		
		public double OVolume {
			get { return oVol; }
		}
		public double Volume {
			get { double vol = 1.0;
				for (int i = 0; i < oDimensions.Length; i++) {
					vol *= (oDimensions[i]+currDisp[i]);
				}
				return vol; }
		}
		
		public double[,] CurrentDeformationGradient
        {
            get { return currDefGrad; }
        }
        #endregion

        #region Constructors
        /*public CellBoundary(double [] inDimensions):base(inDimensions, 0, 0, 0, 0, 0, 0)
		{
			double [] zeros = new double[6];
			Initialize(new double[inDimensions.Length], inDimensions, zeros, zeros);
		}
		public CellBoundary(double[] bottomLeftBackCorner, double[] inDimensions) : base(inDimensions, 0, 0, 0, 0, 0, 0)
		{
			double[] zeros = new double[6];
			Initialize(bottomLeftBackCorner, inDimensions, zeros, zeros);
		}
		public CellBoundary(double [] inDimensions, double [] inStrainIncrement, double [] inStrainRate)
			:base(inDimensions, 0, 0, 0, 0, 0, 0)
		{
			Initialize(new double[inDimensions.Length], inDimensions, inStrainIncrement, inStrainRate);
		}
		public CellBoundary(double[] bottomLeftBackCorner, double[] inDimensions, double[] inStrainIncrement, double[] inStrainRate)
			: base(inDimensions, 0, 0, 0, 0, 0, 0)
		{
			Initialize(bottomLeftBackCorner, inDimensions, inStrainIncrement, inStrainRate);
		}*/
        public CellBoundary( double[] inDimensions, double[] bottomLeftBackCorner = null, double[] inStrainIncrement = null,
			double[] inStrainRate = null, BoundaryType[] boundaryTypes =  null, double minSpacingBetweenBoundary = 0.0) : base(inDimensions, 0, 0, 0, 0, 0, 0)
        {
			boundaryTypes ??= new BoundaryType[3] { BoundaryType.LinearDeformations, BoundaryType.Periodic, BoundaryType.Periodic };
            bottomLeftBackCorner ??= new double[] { 0, 0, 0 };  // C# 8+ (otherwise use: if (bottomLeftBackCorner == null) bottomLeftBackCorner = ...)
            inStrainIncrement ??= new double[6];
            inStrainRate ??= new double[6];

            Initialize(bottomLeftBackCorner, inDimensions, inStrainIncrement, inStrainRate, boundaryTypes, minSpacingBetweenBoundary);
        }
        private void Initialize(double [] bottomLeftBackCorner, double [] inDimensions, double [] inStrainIncrement, double [] inStrainRate, 
			BoundaryType[] boundaryTypes, double minSpacingBetweenBoundary)
        {
            leftBottomBackCorner = bottomLeftBackCorner;
			x[0] = VectorMath.DeepCopy(inDimensions);
			oDimensions = inDimensions;
			strainStep = inStrainIncrement;
			currStrainRate = inStrainRate;
			currStrain = new double[oDimensions.Length * 2];

            //If the boundary is solid, adjust the dimensions and bottom left corner by the minimum spacing
			for (int i = 0; i < boundaryTypes.Length; i++)
            {
				if (boundaryTypes[i] == BoundaryType.Solid)
                {
					oDimensions[i] -= 2.0 * minSpacingBetweenBoundary;
					leftBottomBackCorner[i] += minSpacingBetweenBoundary;
                }
            }

            //Update some stuff
            currDisp = NLStrainToDisplacement(currStrain); //Update the displacements
			currDefGrad = UpdateDefGrad(); //Update the deformation gradient
			currTimeDerDefGrad = UpdateTimeDerDefGrad(); //Update the time derivative of the deformation gradient
			currNormTransf = UpdateNormalTransformation();
			
			//Initiate output lists
			strain = new List<double[]> { new double[oDimensions.Length * 2] };
			
			walls = new CellWall[inDimensions.Length*2];
			double [] center; double [] oppositeCenter;
			double [] normal; double [] projection; //Projection is the amount a fiber will be projected if it intersects the wall
			
			//Assigns the walls: Bottom-left-back corner is 0,0,0 and the wall centers go from there.
			for (int i = 0; i < inDimensions.Length; i++) {
				
				oVol *= oDimensions[i];//Get the volume set

				center = new double[inDimensions.Length];
				bottomLeftBackCorner.CopyTo(center, 0); //Start at the bottom left back corner
				normal = new double[inDimensions.Length]; //Normal starts out as 0
				projection = new double[inDimensions.Length]; //Projection starts out as 0
				
				for (int j = 0; j < inDimensions.Length; j++) {
					
					if (j != i) {
						
						center[j] += oDimensions[j] / 2d;
					}
					else{
						normal[j] = 1;
						projection[j] = oDimensions[j];
					}
				}
				
				walls[2 * i + 0] = new CellWall(center, normal, projection, this, 2 * i + 0, boundaryTypes[i]);
				
				oppositeCenter = VectorMath.DeepCopy(center);
				oppositeCenter[i] += oDimensions[i];
				
				walls[2 * i + 1] = new CellWall(oppositeCenter, VectorMath.ScalarMultiply(-1.0, normal), VectorMath.ScalarMultiply(-1, projection), this, 
					2 * i + 1, boundaryTypes[i]);
			}
		}
		#endregion
		
		#region Public Methods
		
		public override void WriteOutput(int i, StreamWriter dataWrite)
		{
			foreach (CellWall cw in walls) {
				cw.WriteOutput(i, dataWrite);
			}
		}
		
		public override void UpdatePosition(){
			//Increment size (also named position in this context) and update left and lower boundaries
			currStrain = VectorMath.Add(currStrain, strainStep);
			
			currDisp = NLStrainToDisplacement(currStrain); //Update the displacements
			
			currDefGrad = UpdateDefGrad(); //Update the deformation gradient
			currTimeDerDefGrad = UpdateTimeDerDefGrad(); //Update the time derivative of the deformation gradient
			currNormTransf = UpdateNormalTransformation();
			
			x[0] = VectorMath.Add(VectorMath.ExtractVector(currDisp,0,oDimensions.Length-1), oDimensions);//Increment the cell
			
			//updates the wall position
			foreach (CellWall wall in walls) {
				wall.Update();
			}
		}

		public override void UpdateRotPosition(){
			//Iterate rotation
			r[0] = currDisp[2];
		}

		public override void UpdateAcceleration(){
		}
		
		public override void UpdateRotAcceleration(){
		}
		
		public override void SaveTimeStep(int i){
			base.SaveTimeStep(i);
            if (i == 0)
            {
				strain[0] = VectorMath.DeepCopy(currStrain);
			}
            else
            {
				strain.Add(VectorMath.DeepCopy(currStrain));
			}
			
			foreach (CellWall w in walls) {
				w.SaveTimeStep(i);
			}
		}

		public double [] CalculteStrain(){
			return VectorMath.DeepCopy(currStrain);
		}
		
		public double [] UndefXtoDefx(double [] X){
			
			double [] xtemp = MatrixMath.Multiply(currDefGrad,X);
			return xtemp;
		}
		
		public double [] RotateNormals(double [] n){
			
			double [] rotatedN = MatrixMath.Multiply(currNormTransf,n);
			VectorMath.NormalizeVector(ref rotatedN);
			return rotatedN;
		}
		
		public double [] UndefVtoDefv(double [] X){
			
			//Assumes that the initial velocity (in the reference frame) is 0
			double[] tempx = MatrixMath.Multiply(currTimeDerDefGrad,X);
			return tempx;
		}

		/// <summary>
		/// This takes coordinates (y and z) in the deformed state, finds its homogeneous coordinates in the undeformed state, then finds the corresponding X and Vx.  This is
		/// because the deformations in the y and z directions are non-homogeneous, but x is homogeneous.  That way, if a fiber moves to a different location, it will still
		/// get an equivalent x deformation.  This assumes that the initial v is 0.
		/// </summary>
		/// <returns>an array with x and v_x in the deformed configuration</returns>
		/// <param name="x">an array with x, y and z in the deformed configuration</param>
		/// <param name="X0">initial X-position of the point</param>
		public double [] DefyzToDefxAndv(double [] x, double X0){ //This is to output the x-coordinate only (a little faster)
			//Assume that the initial X-coordinate is 0!!!!!!
			//double  tempx = currDefGrad[0,1] / currDefGrad[1,1] * (y - currDefGrad[1,2] / currDefGrad[2,2] * z) + currDefGrad[0,2] / currDefGrad[2,2] * z;
			double[] XHom = MatrixMath.LinSolve (currDefGrad, x);
			XHom[0] = X0;
			double [] xHom = MatrixMath.Multiply(currDefGrad, XHom);
			double [] vHom = MatrixMath.Multiply(currTimeDerDefGrad, XHom);

			double  [] c = new double[2]{xHom[0], vHom[0]};
			return c;
		}

		#region Comments
		/// <summary>
		/// Nonlinear Strains to displacement.
		/// </summary>
		/// <returns>
		/// Returns the displacement modes: [delta_x, delta_y, delta_z, theta_z, theta_y, thety_x]
		/// </returns>
		/// <param name='inStrain'>
		/// Lagrangian Strain, in the form of [E11, E22, E33, E12, E13, E23]
		/// </param>
		#endregion
		public double [] NLStrainToDisplacement(double [] inStrain){
			
			//This is Lagrangian Strain
			double [] dispRot = new double[inStrain.Length];
			
			//TODO This only works for 2 or 3-D, I don't expect anything else, but might want to make this general
			dispRot[0] = oDimensions[0] * (-1.0 + Math.Pow(1.0 + 2.0 * inStrain[0], 0.5));
			
			double dx = dispRot[0] / oDimensions[0] + 1.0;

			if (inStrain.Length == 3) { //2-D, 1-dir, 2-dir, then rotation around z
				
				dispRot[2] = 2.0 * inStrain[2] / dx; //
				dispRot[1] = oDimensions[1] * (-1 + Math.Pow(1.0 + 2.0 * inStrain[1] - Math.Pow(Math.Tan(dispRot[2]), 2.0), 0.5));
			}
			else{ //3-D, 1-dir, 2-dir, 3-dir, then rotation around z, rotation around y, rotation around x
				dispRot[3] = Math.Atan(2.0 * inStrain[5] / dx);
				dispRot[4] = Math.Atan(2.0 * inStrain[4] / dx);
				dispRot[1] = oDimensions[1] * (-1.0 + Math.Pow(1.0 + 2.0 * inStrain[1] - Math.Pow(Math.Tan(dispRot[3]), 2.0), 0.5));
				dispRot[5] = Math.Atan((2.0 * inStrain[3] - Math.Tan(dispRot[3]) * Math.Tan(dispRot[4])) / (dispRot[1] / oDimensions[1] + 1.0));
				dispRot[2] = oDimensions[2] * (-1.0 + Math.Pow(1.0 + 2.0 * inStrain[2] - Math.Pow(Math.Tan(dispRot[5]), 2.0)- Math.Pow(Math.Tan(dispRot[4]), 2.0), 0.5));
			}
			return dispRot;
		}
		
		public PointF [] FindCorners(double [] stepStrain){
			//First update the defGradient
			currDisp = NLStrainToDisplacement(stepStrain); //Update the displacements
			currDefGrad = UpdateDefGrad(); //Update the deformation gradient
			
			//TODO This is just 2-D, make it 3-D later!!!
			PointF LL = new PointF(0f, 0f);
			PointF UL = ToPoint(UndefXtoDefx(new double [3]{0,oDimensions[1],0}));
			PointF UR = ToPoint(UndefXtoDefx(new double [3]{0,oDimensions[1],oDimensions[2]}));
			PointF LR = ToPoint(UndefXtoDefx(new double [3]{0,0,oDimensions[2]}));
			
			return new PointF [4]{LL, UL, UR, LR};
		}
		public PointF[] Find2DCornersAtCurrentStrain()
		{
            //TODO This is just 2-D, make it 3-D later!!!
            PointF LL = new PointF(0f, 0f);
            PointF UL = ToPoint(UndefXtoDefx(new double[3] { 0, oDimensions[1], 0 }));
            PointF UR = ToPoint(UndefXtoDefx(new double[3] { 0, oDimensions[1], oDimensions[2] }));
            PointF LR = ToPoint(UndefXtoDefx(new double[3] { 0, 0, oDimensions[2] }));

            return new PointF[4] { LL, UL, UR, LR };
        } 
		public void WriteBackLeftCornerAndDimensions(StreamWriter dataWrite)
		{
			double[] corner = leftBottomBackCorner;
			dataWrite.WriteLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}",corner[0], corner[1], corner[2], oDimensions[0], oDimensions[1], oDimensions[2]) );
		}

        /// <summary>
        /// Generates a list of points along the boundary of a specified wall with given spacing.
        /// </summary>
        /// <param name="wallIndex">Index of the wall (0-5: Back, Front, Left, Right, Bottom, Top)</param>
        /// <param name="pointSpacing">Desired spacing between points</param>
        /// <param name="includeCorners">Whether to include corner points in the output</param>
        /// <returns>List of points in the deformed configuration</returns>
        public List<double[]> GetBoundaryPoints(int wallIndex, double pointSpacing, bool includeCorners = true, int dim = 2)
        {
            if (wallIndex < 0 || wallIndex >= walls.Length)
                throw new ArgumentException($"Wall index {wallIndex} is out of range [0, {walls.Length - 1}]");

            List<double[]> points = new List<double[]>();
            CellWall wall = walls[wallIndex];

            // Determine which dimension this wall is perpendicular to
            int perpDim = wallIndex / 2; // 0=x, 1=y, 2=z

            if (dim == 2) // 2D case
            {
                GenerateBoundaryPoints2D(wallIndex, pointSpacing, includeCorners, points);
            }
            else // 3D case
            {
                GenerateBoundaryPoints3D(wallIndex, pointSpacing, includeCorners, points, perpDim);
            }

            return points;
        }

        #endregion

        #region Private Methods
        private double [,] UpdateDefGrad(){
			double [,] F = new double[oDimensions.Length, oDimensions.Length];
			int shearCount = oDimensions.Length;
			
			for (int i = 0; i < oDimensions.Length; i++) {
				
				F[i,i] = 1 + currDisp[i] / oDimensions[i];
				
				for (int j = i+1; j < oDimensions.Length; j++) {
					
					F[i,j] = Math.Tan(currDisp[shearCount]);
					shearCount++;
				}
			}
			return F;
		}
		
		private double [,] UpdateNormalTransformation(){
			
			/*double Cx = Math.Cos(currDisp[5]);
			double Sx = Math.Sin(currDisp[5]);
			double Cy = Math.Cos(currDisp[4]);
			double Sy = Math.Sin(currDisp[4]);
			double Cz = Math.Cos(currDisp[3]);
			double Sz = Math.Sin(currDisp[3]);
			double [,] T1 = new double[3,3]{{Cy * Cz, 0d, 0d}, {-1.0 * Sz, Cx, 0d}, {-1d * Sy, -1.0 * Sx, 1d}};
			*/
			double [,] T2 = MatrixMath.InvertTransposed3b3(currDefGrad);
			return T2;
		}
		
		private double [,] UpdateTimeDerDefGrad(){
			
			//This is just inherently 3-D.  Sorry
			/*double d1 = currDisp[0] / oDimensions[0]; //delta x / lx
			double d2 = currDisp[1] / oDimensions[1]; //delta y / ly
			double d3 = currDisp[2] / oDimensions[2]; //delta z / lz
			double T3 = currDisp[3]; //Theta z
			double T2 = currDisp[4]; //Theta y
			double T1 = currDisp[5]; //Theta x
			double tT3 = Math.Tan(T3);
			double tT2 = Math.Tan(T2);
			double tT1 = Math.Tan(T1);
			double sT12 = Math.Pow( 1.0 / Math.Cos(T1), 2.0);
			double sT22 = Math.Pow( 1.0 / Math.Cos(T2), 2.0);
			double sT32 = Math.Pow( 1.0 / Math.Cos(T3), 2.0);
			
			double dd1 = currStrainRate[0] / (d1 + 1.0); //d(delta x / lx)/dt
			double dT3 = (2.0 * currStrain[3] - dd1 * tT3) / ((d1 + 1.0) * sT32); //d(Theta Z)/dt
			double dT2 = (2.0 * currStrain[4] - dd1 * tT2) / ((d1 + 1.0) * sT22);  //d(Theta y)/dt
			double dd2 = (currStrainRate[1] - dT3 * sT32 * tT3) / (d2 + 1.0); //d(delta y / ly)/dt
			double dT1=(2.0 * currStrain[5] - dT2 * sT22 * tT3 - dT3 * sT32 * tT2 - dd2 * tT1) / ((d2 + 1.0) * sT12);  //d(Theta x)/dt
			double dd3 = (currStrainRate[2] - dT2 * sT22 * tT2 - dT1 * sT12 * tT1) / (d3 + 1.0);  //d(delta z / lz)/dt
			return new double[3,3]{{dd1, sT32 * dT3, sT22 * dT2}, {0.0, dd2, sT12 * dT1}, {0.0, 0.0, dd3}};
			 */
			double d1 = currDisp[0] / oDimensions[0] + 1.0; //delta x / l+1
			double d2 = currDisp[1] / oDimensions[1] + 1.0; //delta y / ly+1
			double d3 = currDisp[2] / oDimensions[2] + 1.0; //delta z / lz+1
			double d12 = Math.Pow(d1, 2.0); //delta x / lx+1 ^2
			double d22 = Math.Pow(d2, 2.0); //delta y / ly+1 ^2
			double T3 = currDisp[3]; //Theta z
			double T2 = currDisp[4]; //Theta y
			double T1 = currDisp[5]; //Theta x
			double tT3 = Math.Tan(T3);
			double tT2 = Math.Tan(T2);
			double tT1 = Math.Tan(T1);
			double sT12 = Math.Pow( 1.0 / Math.Cos(T1), 2.0);
			double sT22 = Math.Pow( 1.0 / Math.Cos(T2), 2.0);
			double sT32 = Math.Pow( 1.0 / Math.Cos(T3), 2.0);
			
			double dd1 = currStrainRate[0] / d1; //d(delta x / lx)/dt
			double dT3 = 2.0 * (d1 * currStrainRate[5] - currStrain[5] * dd1) / (d12 * sT32); //d(Theta Z)/dt
			double dT2 = 2.0 * (d1 * currStrainRate[4] - currStrain[4] * dd1) / (d12 * sT22);  //d(Theta y)/dt
			double dd2 = (currStrainRate[1] - dT3 * sT32 * tT3) / (d2); //d(delta y / ly)/dt
			
			double dT1 = ((2.0 * d2 * currStrainRate[3] - 2.0 * currStrain[3] * dd2 - tT3 * (d2 * dT2 * sT22 - dd2 * tT2) - d2 * dT3 * tT2 * sT32))/(d22 * sT12);
			
			double dd3 = (currStrainRate[2] - dT1 * tT1 * sT12 - dT2 * tT2 * sT22) / d3;  //d(delta z / lz)/dt
			
			return new double[3,3]{{dd1, sT32 * dT3, sT22 * dT2}, {0.0, dd2, sT12 * dT1}, {0.0, 0.0, dd3}};
			
		}
		
		private PointF ToPoint(double [] dArray){
			PointF p = new PointF((float)dArray[1], (float)dArray[2]); //This is one and 2 because I am dropping the z-coordinate for the plot
			return p;
		}

        private void GenerateBoundaryPoints2D(int wallIndex, double pointSpacing, bool includeCorners, List<double[]> points)
        {
            int perpDim = wallIndex / 2; // 0=x, 1=y, 2=z
            int parallelDim = 3 - perpDim; // The other dimension

            double perpPos = (wallIndex % 2 == 0) ? leftBottomBackCorner[perpDim] : leftBottomBackCorner[perpDim] + oDimensions[perpDim];

            int numPoints = (int)Math.Ceiling(oDimensions[parallelDim] / pointSpacing) + 1;
            double actualSpacing = oDimensions[parallelDim] / (numPoints - 1);

            for (int i = 0; i < numPoints; i++)
            {
                if (!includeCorners && (i == 0 || i == numPoints - 1))
                    continue;

                double[] undefPoint = new double[oDimensions.Length];
                undefPoint[perpDim] = perpPos;
                undefPoint[parallelDim] = leftBottomBackCorner[parallelDim] + i * actualSpacing;

                // Transform to deformed configuration
                double[] defPoint = UndefXtoDefx(undefPoint);
                points.Add(defPoint);
            }
        }

        private void GenerateBoundaryPoints3D(int wallIndex, double pointSpacing, bool includeCorners, List<double[]> points, int perpDim)
        {
            // Get the two dimensions parallel to this wall
            int[] parallelDims = new int[2];
            int idx = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i != perpDim)
                    parallelDims[idx++] = i;
            }

            double perpPos = (wallIndex % 2 == 0) ? leftBottomBackCorner[perpDim] : leftBottomBackCorner[perpDim] + oDimensions[perpDim];

            int numPoints1 = (int)Math.Ceiling(oDimensions[parallelDims[0]] / pointSpacing) + 1;
            int numPoints2 = (int)Math.Ceiling(oDimensions[parallelDims[1]] / pointSpacing) + 1;

            double spacing1 = oDimensions[parallelDims[0]] / (numPoints1 - 1);
            double spacing2 = oDimensions[parallelDims[1]] / (numPoints2 - 1);

            // Generate points along all four edges of the rectangular face
            for (int i = 0; i < numPoints1; i++)
            {
                if (!includeCorners && (i == 0 || i == numPoints1 - 1))
                    continue;

                // Bottom edge
                double[] undefPoint1 = new double[3];
                undefPoint1[perpDim] = perpPos;
                undefPoint1[parallelDims[0]] = leftBottomBackCorner[parallelDims[0]] + i * spacing1;
                undefPoint1[parallelDims[1]] = leftBottomBackCorner[parallelDims[1]];
                points.Add(UndefXtoDefx(undefPoint1));

                // Top edge
                if (i > 0 || includeCorners) // Avoid duplicate corner
                {
                    double[] undefPoint2 = new double[3];
                    undefPoint2[perpDim] = perpPos;
                    undefPoint2[parallelDims[0]] = leftBottomBackCorner[parallelDims[0]] + i * spacing1;
                    undefPoint2[parallelDims[1]] = leftBottomBackCorner[parallelDims[1]] + oDimensions[parallelDims[1]];
                    points.Add(UndefXtoDefx(undefPoint2));
                }
            }

            // Generate points along left and right edges (excluding corners already added)
            for (int j = 1; j < numPoints2 - 1; j++)
            {
                // Left edge
                double[] undefPoint3 = new double[3];
                undefPoint3[perpDim] = perpPos;
                undefPoint3[parallelDims[0]] = leftBottomBackCorner[parallelDims[0]];
                undefPoint3[parallelDims[1]] = leftBottomBackCorner[parallelDims[1]] + j * spacing2;
                points.Add(UndefXtoDefx(undefPoint3));

                // Right edge
                double[] undefPoint4 = new double[3];
                undefPoint4[perpDim] = perpPos;
                undefPoint4[parallelDims[0]] = leftBottomBackCorner[parallelDims[0]] + oDimensions[parallelDims[0]];
                undefPoint4[parallelDims[1]] = leftBottomBackCorner[parallelDims[1]] + j * spacing2;
                points.Add(UndefXtoDefx(undefPoint4));
            }
        }
        #endregion
    }
	
}