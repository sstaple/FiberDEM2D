/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 9/9/2019
 * Time: 10:09 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using DelaunatorSharp;
using myMath;
using System.IO;
using FDEMCore.Contact.FailureTheories;
using System.Linq;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of FToFMatrixContinuumSpring.
	/// </summary>
	public class FToFMatrixContinuumSpring : FToFBreakableSpring, myMath.RootFinding.iFunction
	{
		#region Private Members
		//Initial values when matrix was created
		protected double r;
		protected double b; //length in the x-direction

        //Initial values for when the matrix was created
        protected double d_initial; //Initial yz distance between the fibers
        protected double theta0_f1;
		protected double theta0_f2;
		protected double theta0_12;
        protected double lx_f1_initial; //Initial length of f1
        protected double dx_initial; //Initial x distance between the fibers
        
        //Degree of freedom vector: 
        //Order is: {u_f2, v_f2, w_f2, u_both, sin(rot_f1), 1-cos(rot_f1), sin(rot_f2), 1-cos(rot_f2)}
        protected double[] q;
		protected double[] q_dot; //dof velocity
		protected double[] q_prev; //pref dof value

        //stiffness matrix
        protected double[,] k;

        //damping matrix
        protected double[,] d;

        //force vector {Fx, Fy, Fz, Mx}
        public double[] force;
        		
		//Matrix material properties
		protected double E;
		protected double Ep;
		protected double nu;
		protected double G;
		
		//Integration Bounds (_t means top half of the matrix, 1 is on top, 2 is on bottom)
		protected double z_t1;
		protected double z_t2;
		protected double z_b1;
		protected double z_b2;
		protected double charDist;

		protected IFailureCriteria failureCriteria;
		protected Matrix_RigidFibers matrixModel;

		//Geometry
		protected double [] norm_vT; //Tangential to the contact plane
		protected double [] e21T; //Tangential to the contact plane
		protected double [] vrelT; //relative tangential velocities of the 2 bodies (3-d)
		protected double vrelN; //relative normal velocities of the 2 bodies (3-d)
        protected double[] e21N;

        //Need additional moment for fiber 2 because the vertical displacement results in non-symmetric moments
        protected double momentMag_f2;

		//Outputs to Save
		protected List <double[]> lq;
        protected List<double[]> lzRange;
        protected List <double> lTheta_f2;
		protected List <double> lTheta_f1;
		protected List <double> lCrackHalfLength;

		#endregion

		#region Public Members
		public static string Name = "MatrixContinuum";
		#endregion

		#region Constructors
		/// <summary>
		/// This class represents the matrix between two fibers, assuming displacements vary linearly with along x.  This assumes that fibers are the same radius
		/// </summary>
		/// <param name="initialCenterlineDistance">initial distance between the two fibers when spring is created</param>
		/// <param name="x12">vector between fiber 1 and fiber 2</param>
		/// <param name="matParams">matrix parameters object</param>
		/// <param name="fiber1">fiber 1</param>
		/// <param name="fiber2">fiber 2</param>
		/// <param name="nfiber1">index of fiber 1</param>
		/// <param name="nfiber2">index of fiber 2</param>
		public FToFMatrixContinuumSpring(double initialCenterlineDistance, double [] x12, MatrixContinuumParameters matParams,
		                                 Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
			:base(fiber1, fiber2, nfiber1, nfiber2)
		{
			//Set Constants for the initial geometry
			failureCriteria = matParams.FailureTheory;

			r = f1.Radius;
			b = f1.OLength;
			E = matParams.E;
			nu = matParams.Nu;
			Ep = matParams.Ep;
			G = matParams.G;
			charDist = matParams.CharDist;
			dCoefficient = matParams.DampCoeff;
			isBroken = false;
			currentlyActive = true;
			
			//Set the lists of data to be saved
			lq = new List<double[]>();
            lzRange = new List<double[]>();
            lTheta_f1 = new List<double>();
			lTheta_f2 = new List<double>();

            //Set the initial conditions
            q_prev = new double[8];
            q = new double[8];
            q_dot = new double[8];
            theta0_f1 = f1.CurrentRotation;
			theta0_f2 = f2.CurrentRotation;
			x12_YZ = x12;
			theta0_12 = Math.Atan(x12_YZ[2]/x12_YZ[1]);
			d_initial = initialCenterlineDistance;
            lx_f1_initial = f1.CurrentLength;
            dx_initial = f2.CurrentPosition[0] - f1.CurrentPosition[0];
			
			//Set the integration limits
			z_t1 = f1.Radius - charDist * f1.Radius;
			z_b2 = -1.0 * z_t1;
			z_t2 = 0.0;
			z_b1 = 0.0;

            //If the fibers are touching, then start it out with an initial "crack"
            if (initialCenterlineDistance <= (2 * f1.Radius)) {
                double overlapRegionHalfLength = 1 / d_initial * Math.Sqrt((-d_initial + r - r) * (-d_initial - r + r) * (-d_initial + r + r) * (d_initial + r + r)) / 2;
                z_t2 = overlapRegionHalfLength + charDist * f1.Radius;
                z_b1 = -1.0 * z_t2;
            }
            else if(initialCenterlineDistance <= (2 * f1.Radius + charDist * f1.Radius)) { 
				
				z_t2 = charDist * f1.Radius;
				z_b1 = -1.0 * z_t2;
			}

			matrixModel = new Matrix_RigidFibers(r, d_initial, b, Ep, nu, f1.Mass, f2.Mass, f1.Inertia, f2.Inertia, dCoefficient);

			//Set the stiffness
			this.CalculateStiffnessesAndDamping();
			
		}
		#endregion
		
        #region Public Methods

        public override bool IsSpringActive(int currentTimeStep){
			
			if (!isBroken) {
				
				double [] xl12 = new double[3];
				double [] vl12 = new double[3];
				double [] pt1 = new double[3];
				double [] pt2 = new double[3];
                int nList1 = 0;
                int nList2 = 0;
                centerpointDistance_YZ = GetMinYZDistanceBetweenFibersIncludingProjections(f1, f2, ref xl12, ref vl12, ref pt1, ref pt2, ref nList1, ref nList2);

                //Save the number of projected fiber
                npf1 = nList1 - 1;
                npf2 = nList2 - 1;
                double [] e12Ntemp = Spring.NormalizeVector(xl12);
				IsFoundToBeActive(centerpointDistance_YZ, pt1, pt2, xl12, vl12, e12Ntemp, currentTimeStep);
				
			}
			return !isBroken;
		}
		
		protected override void InitiateInternalValues(){
			
			//Initial geometry is already set in the constructor, so I don't necessarily need this.
		}
		
		protected override void IncrementInternalValues(){

            q_prev = (double[])(q.Clone());
		}
		
		public override void WriteOutput(int nTimeStep, StreamWriter dataWrite){

			if (!notYetActive && lTimeSteps.Contains(nTimeStep)) {

                int index = lTimeSteps.IndexOf(nTimeStep);

                //Write this just the first time
                if (index == 0)
                {
                    dataWrite.WriteLine(nf1 + "," + lNProjectedFiber1[index] + "," + nf2 + "," + lNProjectedFiber2[index]
                                             + "," + Name + "," + (d_initial) + "," + E + "," + nu + ",");
                }
				
                //This is the info that we include otherwise (1st time too)
				dataWrite.WriteLine(nf1 + "," + lNProjectedFiber1[index] + "," + nf2 + "," + lNProjectedFiber2[index]
											 + "," + "m" +  ", " + lq[index][0] + "," + lq[index][1] 
                    + "," + lq[index][2] + "," + lq[index][3] + "," + lTheta_f1[index] + "," + lTheta_f2[index]
                    + "," + lzRange[index][0] + "," + lzRange[index][1] + "," + lzRange[index][2] + "," + lzRange[index][3]);
				
			}
		}
		
		public override void SaveTimeStep(int iSaved, int iCurrent){
			
			if (!isBroken && (iCurrent == base.tIndex)) {  //calling bse.tIndex is the same as checking current contact
                //Save these values first for the spring update method

                base.SaveTimeStep (iSaved, iCurrent);
                lq.Add(q_prev);
				lTheta_f1.Add(Math.Asin(q_prev[4]));
				lTheta_f2.Add(Math.Asin(q_prev[6]));
                lzRange.Add(new double[4] { z_t1, z_t2, z_b1, z_b2 });
            }
		}

		public override bool BreakSpring(){

			bool isTopFailed = CheckSectionForFfailure(ref z_t1, ref z_b1);
			bool isBottomFailed = CheckSectionForFfailure(ref z_t2, ref z_b2);
            if (isTopFailed)
            {
				return true;
            }
            else
            {
				return isBottomFailed;
            }
		}

		#endregion

		#region Private Methods

		#region Failure Methods
		protected bool CheckSectionForFfailure(ref double ztop, ref double zbottom)
		{
			bool isFailed = false;

			//This just checks the failure down the centerline (y=d/2)
			//double ftop = failureCriteria.FailureFunction(0.0, d_initial / 2.0, ztop, q, matrixModel);
			//double fbottom = failureCriteria.FailureFunction(0.0, d_initial / 2.0, zbottom, q, matrixModel);
			//This checks the failure on both sides
			double ftop = FindMaximumFailureFunctionOnBothSides(0.0, ztop, q, matrixModel);
			double fbottom = FindMaximumFailureFunctionOnBothSides(0.0, zbottom, q, matrixModel);


			//If both sides are broken (>0)
			if (ftop >= 0.0 && fbottom >= 0.0)
			{
				isFailed = true;

				double zmiddle = (ztop - zbottom) / 2.0;
				//This just checks the failure down the centerline (y=d/2)
				//double fmiddle = failureCriteria.FailureFunction(0.0, d_initial / 2.0, zmiddle, q, matrixModel);
				//This checks the faliure on both sides (y), but in the middle (z)
				double fmiddle = FindMaximumFailureFunctionOnBothSides(0.0, zmiddle, q, matrixModel);

				//If the middle is broken too, then just break the whole thing
				if (fmiddle >= 0.0)
				{
					ztop = zbottom;
					isBroken = true;
				}
				//If the middle isn't broken, find the intersect in both halves
				else
				{
					//Move both edges
					//This just checks the failure down the centerline (y=d/2)
					FindNewZwhenOneSieIsFailed(ftop, fmiddle, ref ztop, ref zmiddle);
					FindNewZwhenOneSieIsFailed(fmiddle, fbottom, ref zmiddle, ref zbottom);
				}
			}
			//If just one side is broken
			else if (ftop > 0.0 || fbottom > 0.0)
			{
				isFailed = true;
				FindNewZwhenOneSieIsFailed(ftop, fbottom, ref ztop, ref zbottom);
			}

			return isFailed;
		}

		protected void FindNewZwhenOneSieIsFailed(double ft, double fb, ref double zt, ref double zb)
		{
			//This finds the 0 in between z1 and z2
			double zZero = myMath.RootFinding.FalsiMethod(this, zt, zb, ft, fb, 0.01, 5);

			if (ft >= 0)
			{
				//z1 = zZero;
				double diff = zt - zZero;
				//overshoot by the crack growth to speed up convergence
				zt = zZero - 2.0 * diff;
				//If we overshot past zb
				if (zt <= zb)
				{
					zt = zb + 0.5 * (zZero - zb);
				}
			}
			else
			{
				//zb = zZero;
				double diff = zZero - zb;
				zb = zZero + 2.0 * diff;
				//if we overshot
				if (zb >= zt)
				{
					zb = zt - 0.5 * (zt - zZero);
				}
			}
		}
		public double Eval(double x)
		{
			//Just checks on both sides
			return FindMaximumFailureFunctionOnBothSides(0.0, x, q, matrixModel);
			//This just checks the failure down the centerline (y=d/2)
			//return failureCriteria.FailureFunction(0.0, dx_initial / 2.0, x, q, matrixModel);
		}
		protected double FindMaximumFailureFunctionOnBothSides(double x, double z, double[] qm, IMatrixModel matrixModel)
        {
			//Get the point at either side
			double yLeft = matrixModel.CalculateYAtFiber1(z);
			double yRight = matrixModel.CalculateYAtFiber2(z);

			//Calculate the failure function at either side
			double fLeft = failureCriteria.FailureFunction(0.0, yLeft, x, qm, matrixModel);
			double fRight = failureCriteria.FailureFunction(0.0, yRight, x, qm, matrixModel);

			//return the maximum failure function from the two sides
			return Math.Max(fLeft, fRight);
		}
		#endregion

		protected override void UpdateInternalValues(){
            double[] x2m1 = VectorMath.ScalarMultiply(-1.0, x12_YZ);
            e21N = VectorMath.ScalarMultiply(-1.0, e12N_YZ);
            double[] v2m1 = VectorMath.ScalarMultiply(-1.0, v12);

            //This was the old math.  The new math is at an absolute coordinate system, so I want to use the z-axis that is rotated
            //vrelT = VectorMath.Subtract(v2m1, VectorMath.ScalarMultiply(VectorMath.Dot(e21N, v2m1), e21N));
			e21T = MatrixMath.Multiply(new double[,] { { 1, 0, 0 }, { 0, 0, -1 }, { 0, 1, 0 } }, e21N);
			vrelN = VectorMath.Dot(v2m1, e21N);
            vrelT = VectorMath.ScalarMultiply( VectorMath.Dot(v2m1, e21T), e21T);
            norm_vT = VectorMath.DeepCopy(vrelT);
			VectorMath.NormalizeVector(ref norm_vT);

            ///Calculate speed of the vertical (y-v) movement to calculate the movement
            double dW_f2_dot = VectorMath.Norm(vrelT);

            //Calculate Angles
            double theta_12 = Math.Atan(x2m1[2] / x2m1[1]);
            double dTheta_f2 = (f2.CurrentRotation - theta0_f2) - (theta_12 - theta0_12);
            double dTheta_f1 = (f1.CurrentRotation - theta0_f1) - (theta_12 - theta0_12);

            //set the degrees of freedom
            q[0] = (f2.CurrentPosition[0] - f1.CurrentPosition[0]) - dx_initial;
            q[1] = centerpointDistance_YZ - d_initial;
            q[2] = dW_f2_dot * currDT;
            q[3] = f1.CurrentLength - lx_f1_initial;
            q[4] = Math.Sin(dTheta_f1);
            q[5] = 1.0 - Math.Cos(dTheta_f1);
            q[6] = Math.Sin(dTheta_f2);
            q[7] = 1.0 - Math.Cos(dTheta_f2);

            //Set the velocity of the degrees of freedom based on the previous and current values
            q_dot = VectorMath.ScalarMultiply(1 / currDT, myMath.VectorMath.Subtract(q, q_prev));

            //set a few speeds that are based on fiber velocities, not the previous iteration.
            //q_dot[1] = VectorMath.Dot(v2m1, e21N); //Positive is moving away from each other, negative together
            q_dot[2] = dW_f2_dot;

        }
		
		protected override void CalculateForcesAndMoments(){

			if (!currDT.Equals(0.0))
			{
				force = VectorMath.Add(MatrixMath.Multiply(k, q), MatrixMath.Multiply(d, q_dot));
			}
			else
			{
				force = MatrixMath.Multiply(k, q);
			}

		}

        protected override void ApplyForcesAndMoments()
        {
            //First, the global to local transformation is the collection of local direction vectors
            double [,] rotationMatrix =  new double[,] { { 1, e21N[0], e21T[0] }, { 0, e21N[1], e21T[1] }, { 0, e21N[2], e21T[2] } };
            //Now transpose it (tranpose = inverse for a orthonormal matrix) to get the transformation

            double[] globalForce = MatrixMath.Multiply(rotationMatrix, new double[3] { force[0], force[1], force[2] });

            //Save some of the results
            normForceMag = force[1];
            tanForceMag = force[2];
            momentMag = force[3];

            normForceVect = VectorMath.ScalarMultiply(normForceMag, e21N);
            tanForceVect = VectorMath.ScalarMultiply(tanForceMag, e21T);

            //Normal Forces
            f1.CurrentForces.Add(globalForce);
            f2.CurrentForces.Add(VectorMath.ScalarMultiply(-1.0, globalForce));
            //f1.currentForces.Add(normForceVect);
            //f2.currentForces.Add(VectorMath.ScalarMultiply(-1.0, normForceVect)); 

            //Moment (had to override because I have different moments to add)
            f1.CurrentMoments.Add(force[3]);

            f2.CurrentMoments.Add(force[3]);
        }

        #endregion

		#region Private Methods for getting stiffness
		/// <summary>
		/// Stiffness in the x-direction.
		/// </summary>
		/// <param name="zt1">z-coordinate at the very top (usually radius - some characteristic distance)</param>
		/// <param name="zt2">z-coordinate at the bottom of the top half (usually 0, but when there is some crack it is 1/2 of the crack length)</param>
		/// <param name="zb1">z-coordinate at the top of the bottom half</param>
		/// <param name="zb2">z-coordinate at the bottom of the bottom half</param>
		public static double K_x(double zt1, double zt2, double zb1, double zb2, double r, double d, double b,  double Ep, double nu, double G){

            Matrix_RigidFibers mykx = new Matrix_RigidFibers( r,  d,  b,  Ep,  nu,  1, 1, 1, 1, 1);

            double kx = mykx.Calculate_knorm(zt1, zt2, zb1, zb2);

            return kx;

        }
		
		protected void CalculateStiffnessesAndDamping(){
			
			matrixModel.CalculateStiffnesses(ref k, ref d, z_t1, z_t2, z_b1, z_b2);

		}
		
		#endregion

		#region Static Methods for creation
		
		static public void CreateMatrixPairs(ref List<Fiber> lFibers, ref List <FToFRelation> lSprings,
		                                     ref List<MatrixProjectedFiber> lMatrixProjFibers,
		                                     CellWall [] cw, ContactParameters conParams, MatrixContinuumParameters matrixParams){
			

			//Erase any old springs, so that only matrix is active.
			lSprings = new List<FToFRelation>();
			int n = lFibers.Count;

			MyPoint[] myPts = AddAllFiberProjectionsToPoints(lFibers, cw);

			//Now do the triangulation, and extract all of the pairs
			var triangulation = new Delaunator(myPts);
			List<int[]> pairs = ExtractIndicesOfPairs(triangulation);

			//Debugging: draw triangulation
			//OutputAllTriangulation(points, pairs, "E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\FDEMTests\\InputFiles\\Connections.csv");
			//Debugging:
			//OutputFiberPositionsInPackFile(lFibers, "E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\FDEMTests\\InputFiles\\Position2.csv");

            //Sort them all: make the smaller index first
            foreach (int[] iArray in pairs)
            {
				Array.Sort(iArray);
            }

			//now find the original connections
			for (int i = 0; i < pairs.Count; i++) {
				
				if (pairs[i][0] < n) {
					
					//Original RVE: make a spring!
					if(pairs[i][1] < n) {
						lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[pairs[i][1]], pairs[i][0], pairs[i][1]));
						
						lSprings[lSprings.Count-1].AddNonContactSpring(matrixParams);
					}
					//Directly to the right (proj of cw[2]), index 1 for projections)
					else if(pairs[i][1] >= n && pairs[i][1] < 2*n){
						int nFiber = pairs[i][1]-n;
						lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 0));
						lFibers[nFiber].AddProjectedFiber(cw[2].PeriodicProjection, false, new int[] { 2 });
						lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));
						
						lSprings[lSprings.Count-1].AddNonContactSpring(matrixParams);
					}
					//Directly above (proj of cw[4], index 3 for projections)
					else if(pairs[i][1] >= 3*n && pairs[i][1] < 4*n){
						int nFiber = pairs[i][1]-3*n;
						lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 1));
						lFibers[nFiber].AddProjectedFiber(cw[4].PeriodicProjection, false, new int[] { 4 });
						lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));
						
						lSprings[lSprings.Count-1].AddNonContactSpring(matrixParams);
					}
					//above and to the right (proj of cw[2] + cw[4], index 5 for projections)
					else if(pairs[i][1] >= 5*n && pairs[i][1] < 6*n){
						int nFiber = pairs[i][1]-5*n;
						lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 2));
						double [] tempProj = myMath.VectorMath.Add(cw[2].PeriodicProjection, cw[4].PeriodicProjection);
						lFibers[nFiber].AddProjectedFiber(tempProj, false, new int[] { 2, 4 });
						lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));
						
						lSprings[lSprings.Count-1].AddNonContactSpring(matrixParams);
					}
					//above and to the left (proj of cw[3] + cw[4], index 7 for projections), ends up as index 3
					else if(pairs[i][1] >= 7*n && pairs[i][1] < 8*n){
						int nFiber = pairs[i][1]-7*n;
						lMatrixProjFibers.Add(new MatrixProjectedFiber(nFiber, 3));
						double [] tempProj = myMath.VectorMath.Add(cw[3].PeriodicProjection, cw[4].PeriodicProjection);
						lFibers[nFiber].AddProjectedFiber(tempProj, false, new int[] { 3, 4 });
						lSprings.Add(new FToFRelation(conParams, lFibers[pairs[i][0]], lFibers[nFiber], pairs[i][0], nFiber));
						
						lSprings[lSprings.Count-1].AddNonContactSpring(matrixParams);
					}
				}
			}

			//Debugging:
			//OutputFiberPositionsInPackFile(lFibers, "E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\FDEMTests\\InputFiles\\Position3.csv");

			//remove duplicates from lMatrixProjFibers
			for (int i = 0; i < lMatrixProjFibers.Count-1; i++) {
				for (int j = i+1; j < lMatrixProjFibers.Count; j++) {
					if (lMatrixProjFibers[i].FiberIndex == lMatrixProjFibers[j].FiberIndex && lMatrixProjFibers[i].ProjectionIndex == lMatrixProjFibers[j].ProjectionIndex) {
						lMatrixProjFibers.RemoveAt(j);
						j-=1;
					}
				}
			}
			//Debugging
			//OutputFiberPositionsInPackFile(lFibers, "E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\FDEMTests\\InputFiles\\Position4.csv");
		}
		/// <summary>
		/// This is just for debugging: it doesn't make any projections, but just keeps the original connections to make smaller RVEs for unit tests.
		/// </summary>
		static public void CreateMatrixPairs(ref List<Fiber> lFibers, ref List<FToFRelation> lSprings,
											 ref List<MatrixProjectedFiber> lMatrixProjFibers,
											 CellWall[] cw, ContactParameters conParams, MatrixContinuumParameters matrixParams, bool dontMakeProjections)
		{

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
		static public void UpdateMatrix(ref List <FToFRelation> lSprings, int currentTimeStep, double dT){
			
			foreach (FToFRelation s in lSprings) {
				
				s.Update(currentTimeStep, dT);
			}
			
		}
		
		static public void UpdateProjectedFibers(ref List<Fiber> lFibers, CellWall [] cw, List<MatrixProjectedFiber> lMatrixProjFibers){
			
			//Make this horrible projections array that coordinates with the numbers assigned in CreateMatrixPairs()
			//TODO: this is sloppy coding!!!  Change this someday.  It has too many built in indices (1 is already too many!)
			double [][] projections = new double[4][]{cw[2].PeriodicProjection, cw[4].PeriodicProjection,
				myMath.VectorMath.Add(cw[2].PeriodicProjection, cw[4].PeriodicProjection),
				myMath.VectorMath.Add(cw[3].PeriodicProjection, cw[4].PeriodicProjection)};
			
			//Add all of the projected fibers (assumes the fibers don't cross the boundaries)
			foreach (MatrixProjectedFiber mpf in lMatrixProjFibers) {
				
				lFibers[mpf.FiberIndex].AddProjectedFiber(projections[mpf.ProjectionIndex], mpf.cellWallIndices);
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
		                                               double projx, double projy){
			
			foreach (Fiber f in lFibers) {
				vertices.Add( new double[2] { f.CurrentPosition[1] + projx, f.CurrentPosition[2] + projy });
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
				dataWrite.WriteLine(vertices[con[0]][0] + "," + vertices[con[0]][1] + "," + vertices[con[1]][0] + "," + vertices[con[1]][1] );
			}

			dataWrite.Close();
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
	public class MatrixProjectedFiber{
		public int FiberIndex;
		public int ProjectionIndex;
		public int[] cellWallIndices;
		public MatrixProjectedFiber(int fiberIndex, int projIndex){
			FiberIndex = fiberIndex;
			ProjectionIndex = projIndex;

			//this tells you which cell walls they are being projected by (2 is on the right, projected 
			//from cw[2] on the left, and 4 is on top, projected from cw[4] on the bottom
            switch (projIndex)
            {
				case 1:
					cellWallIndices = new int[] { 2 };
					break;
				case 3:
					cellWallIndices = new int[] { 4 };
					break;
				case 5:
					cellWallIndices = new int[] { 4, 2};
					break;
				case 6:
					cellWallIndices = new int[] { 3, 2 };
					break;
				case 7:
					cellWallIndices = new int[] { 1, 4 };
					break;
				default:
					cellWallIndices = new int[0];
					break;
            }
        }
	}
}