using System;
using System.Collections.Generic;
using myMath;
using System.IO;
using FDEMCore.Contact.FailureTheories;

namespace FDEMCore.Contact
{
	public class FToFMatrixContinuumElasticFiberSpring : FToFBreakableSpring, myMath.RootFinding.iFunction
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

		//Degree of freedom vector:  (subscript is for fiber)
		//Order is: {rot_1, u_2, v_2, rot_2, u_both, 
		protected double[] q;
		protected double[] q_dot; //dof velocity
		protected double[] q_prev; //pref dof value

		//Degree of freedom vector of matrix: (subscript is fiber boundary)
		//Order is: {u_f1, v_f1, w_f1, rot_f1, u_f2, v_f2, w_f2, rot_f2, u_both}
		protected double[] qm;
		protected bool isQmUpdated;

		//stiffness matrix
		protected double[,] k;
		protected Matrix_ElasticFiber matrixModel;

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
		protected bool checkFiber1Surface;

		//Geometry
		protected double[] norm_vT; //Tangential to the contact plane
		protected double[] e21T; //Tangential to the contact plane
		protected double[] vrelT; //relative tangential velocities of the 2 bodies (3-d)
		protected double vrelN; //relative normal velocities of the 2 bodies (3-d)
		protected double[] e21N;
		protected double[,] rotationMatrix;

		//Outputs to Save
		protected List<double[]> lq;
		protected List<double[]> lqm;
		protected List<double[]> lzRange;

		#endregion

		#region Public Members
		public static string Name = "MatrixContinuumElasticFibers";
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
		public FToFMatrixContinuumElasticFiberSpring(double initialCenterlineDistance, double[] x12, MatrixContinuumParameters matParams,
										 Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
			:base(fiber1, fiber2, nfiber1, nfiber2)
		{
			//Set Constants for the initial geometry
			failureCriteria = matParams.FailureTheory                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          ;
			
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
			lqm = new List<double[]>();
			lzRange = new List<double[]>();

			//Set the initial conditions
			q_prev = new double[5];
			q = new double[5];
			qm = new double[7];
			q_dot = new double[5];
			theta0_f1 = f1.CurrentRotation; 
			theta0_f2 = f2.CurrentRotation;
			x12_YZ = x12;
			theta0_12 = Math.Atan2(-1.0*x12_YZ[2], -1.0 * x12_YZ[1]);
			d_initial = initialCenterlineDistance;
			lx_f1_initial = f1.CurrentLength;
			dx_initial = f2.CurrentPosition[0] - f1.CurrentPosition[0];

			//Set the integration limits
			// <param name="zt1">top of top half</param>
			// <param name="zt2">bottom of top half</param>
			// <param name="zb1">top of bottom half</param>
			// <param name="zb2">bottom of bottom half</param>
			z_t1 = f1.Radius - charDist * f1.Radius;
			z_t2 = 0.0;
			z_b1 = 0.0;
			z_b2 = -1.0 * z_t1;

			//If the fibers are touching, then start it out with an initial "crack"
			if (initialCenterlineDistance <= (2 * f1.Radius))
			{
				double r1 = fiber1.Radius;
				double r2 = fiber2.Radius;
				double overlapRegionHalfLength = 1 / d_initial * Math.Sqrt((-d_initial + r2 - r1) * (-d_initial - r2 + r1) * (-d_initial + r2 + r1) * (d_initial + r2 + r1)) / 2.0;
				z_t2 = overlapRegionHalfLength + charDist * f1.Radius;
				z_b1 = -1.0 * z_t2;
                if (z_t2 > z_t1)
                {
					throw new ArgumentException($"Fibers are overlapping too much: fibers {nf1} and {nf2}");
				}
			}
			//This code was to take out the space between if they were nearly touching, but I took this out.  Didn't seem like I neded it....
			/*else if (initialCenterlineDistance <= (2 * f1.Radius + charDist  * f1.Radius))
			{

				z_t2 = charDist * f1.Radius;
				z_b1 = -1.0 * z_t2;
			}*/

			//Set the stiffness
			matrixModel = new Matrix_ElasticFiber(r, d_initial, b, Ep, nu, f1, f2, dCoefficient);
			//this.CalculateStiffnessesAndDamping();

		}
		#endregion

		#region Public Methods

		public override bool IsSpringActive(int currentTimeStep)
		{

			if (!isBroken)
			{

				double[] xl12 = new double[3];
				double[] vl12 = new double[3];
				double[] pt1 = new double[3];
				double[] pt2 = new double[3];
				int nList1 = 0;
				int nList2 = 0;
				centerpointDistance_YZ = GetMinYZDistanceBetweenFibersIncludingProjections(f1, f2, ref xl12, ref vl12, ref pt1, ref pt2, ref nList1, ref nList2);

				//Save the number of projected fiber
				npf1 = nList1 - 1;
				npf2 = nList2 - 1;
				double[] e12Ntemp = Spring.NormalizeVector(xl12);
				IsFoundToBeActive(centerpointDistance_YZ, pt1, pt2, xl12, vl12, e12Ntemp, currentTimeStep);

			}
			return !isBroken;
		}

		protected override void InitiateInternalValues()
		{

			//Initial geometry is already set in the constructor, so I don't necessarily need this.
		}

		protected override void IncrementInternalValues()
		{
			q_prev = (double[])(q.Clone());
		}

		public override void WriteOutput(int nTimeStep, StreamWriter dataWrite)
		{

			if (!notYetActive && lTimeSteps.Contains(nTimeStep))
			{

				int index = lTimeSteps.IndexOf(nTimeStep);

				//Write this just the first time
				if (index == 0)
				{
					dataWrite.WriteLine(nf1 + "," + npf1 + "," + nf2 + "," + npf2
											 + "," + Name + "," + (d_initial) + "," + E + "," + nu + ",");
				}

				//This is the info that we include otherwise (1st time too) //Notice we add the ug from the original u
				dataWrite.WriteLine(nf1 + "," + lNProjectedFiber1[index] + "," + nf2 + "," + lNProjectedFiber2[index]
											 + "," + "m" + ", " + lqm[index][0] + "," + lqm[index][1]
					+ "," + lqm[index][2] + "," + lqm[index][3] + "," + lqm[index][4] + "," + lqm[index][5]
					+ "," + lqm[index][6] + "," + lqm[index][7]
					+ "," + lq[index][4]
					+ "," + lzRange[index][0] + "," + lzRange[index][1] + "," + lzRange[index][2] + "," + lzRange[index][3]);
					//For Debugging: + "," + lNormForceMag[index] + "," + lTanForceMag[index]);

			}
		}

		public override void SaveTimeStep(int iSaved, int iCurrent)
		{

			if (!isBroken && (iCurrent == base.tIndex))
			{  //calling bse.tIndex is the same as checking current contact
			   //Save these values first for the spring update method

				base.SaveTimeStep(iSaved, iCurrent);
				lq.Add(q_prev);
				lzRange.Add(new double[4] { z_t1, z_t2, z_b1, z_b2 });

				if (!isQmUpdated)
				{
					qm = matrixModel.CalculateMatrixDOF(q);
					isQmUpdated = true;
				}

				lqm.Add(qm);
			}
		}

		public override bool BreakSpring()
		{

			if (failureCriteria is FailureTheories.NoFailure)
			{
				return false;
			}

			bool isThereFailure = false;

            if (!isQmUpdated)
            {
				qm = matrixModel.CalculateMatrixDOF(q);
				isQmUpdated = true;
			}
			//Here is where I will eventually check the stress and change z_t and z_b
			
			//Debug code
			//double tempzt1 = z_t1;
			//double tempzt2 = z_t2;
			//double tempzb1 = z_b1;
			//double tempzb2 = z_b2;


			//Check F1 Surface
			checkFiber1Surface = true;
			bool topFailureF1 = IsThereFailureInTheSection(ref z_t1, ref z_t2);
			bool bottomFailureF1 = IsThereFailureInTheSection(ref z_b1, ref z_b2);

			//check F2 surface
			checkFiber1Surface = false;
			bool topFailureF2 = IsThereFailureInTheSection(ref z_t1, ref z_t2);
			bool bottomFailureF2 = IsThereFailureInTheSection(ref z_b1, ref z_b2);

			//Only Recalculate stiffness if there was failure
			if (topFailureF1 || bottomFailureF1 || topFailureF2 || bottomFailureF2)
            {
				CalculateStiffnessesAndDamping();
				isThereFailure = true;
            }

            //DEBUG
           // if (tempzt1 < z_t1 || tempzt2 > z_t2 || tempzb1 < z_b1 || tempzb2 > z_b2)
           // {
			//	bool breakhere = true;
            //}

			return isThereFailure;
		}

		public override double[,] CalculateCurrentSpringStress(int iCurrent)
		{

			double[,] cs = new double[3, 3];

			if (!notYetActive && currentlyActive && (iCurrent == tIndex))
			{
				//This is the contributions from the axial stresses

				cs = base.CalculateCurrentSpringStress(iCurrent);
				/**/
				//Now calculate the contributions of transverse stresses
				matrixModel.CalculateIntegralOfStressDV(out double[,] kS11, out double[,] kS33, z_t1, z_t2, z_b1, z_b2);
				double[] S11 = MatrixMath.Multiply(kS11, qm);
				double[] S33 = MatrixMath.Multiply(kS33, qm);
				double[,] STensor = new double[3, 3] { { S11[0], 0, 0 }, { 0, 0, 0 }, { 0, 0, S33[0] } };

				double[,] rotatedTransverseStress = MatrixMath.Multiply(MatrixMath.Transpose(rotationMatrix), MatrixMath.Multiply(STensor, rotationMatrix));
				
				cs = MatrixMath.Add(cs, rotatedTransverseStress);
				
			}

			return cs;
		}

		protected override void UpdateInternalValues()
		{
			isQmUpdated = false;
			double[] x2m1 = VectorMath.ScalarMultiply(-1.0, x12_YZ);
			e21N = VectorMath.ScalarMultiply(-1.0, e12N_YZ);
			double[] v2m1 = VectorMath.ScalarMultiply(-1.0, v12);

			//This was the old math.  The new math is at an absolute coordinate system, so I want to use the z-axis that is rotated
			//vrelT = VectorMath.Subtract(v2m1, VectorMath.ScalarMultiply(VectorMath.Dot(e21N, v2m1), e21N));
			e21T = MatrixMath.Multiply(new double[,] { { 1, 0, 0 }, { 0, 0, -1 }, { 0, 1, 0 } }, e21N);
			vrelN = VectorMath.Dot(v2m1, e21N);
			vrelT = VectorMath.ScalarMultiply(VectorMath.Dot(v2m1, e21T), e21T);
			norm_vT = VectorMath.DeepCopy(vrelT);
			VectorMath.NormalizeVector(ref norm_vT);

			//Calculate Angles
			//This always puts the angle in -pi to pi.
			double theta_12 = Math.Atan2(x2m1[2], x2m1[1]);
			double theta_12t = ConvertAngleToRangePI_to_negPI(theta_12 - theta0_12);

			//Convert the other angles to -pi to pi.  WARNING: this does not allow twists greater than 180 degrees!!!  For something really 
			//General, this formulation needs to changed to an incremental one.
			double thetaf1t = ConvertAngleToRangePI_to_negPI(f1.CurrentRotation - theta0_f2);
			double thetaf2t = ConvertAngleToRangePI_to_negPI(f2.CurrentRotation - theta0_f1);

			double dTheta_f2 = thetaf2t - theta_12t;
			double dTheta_f1 = thetaf1t - theta_12t;

			//set the degrees of freedom
			q[0] = dTheta_f1;
			q[1] = (f2.CurrentPosition[0] - f1.CurrentPosition[0]) - dx_initial;
			q[2] = centerpointDistance_YZ - d_initial;
			q[3] = dTheta_f2;
			q[4] = f1.CurrentLength - lx_f1_initial;

			//Set the velocity of the degrees of freedom based on the previous and current values

			q_dot = VectorMath.ScalarMultiply(1 / currDT, myMath.VectorMath.Subtract(q, q_prev));
		}

		protected override void CalculateForcesAndMoments()
		{
			//Calculate the stiffness if it is null.  This was added to get the calculation out of the constructor
			if ((k == null || k.Length == 0))
			{
				CalculateStiffnessesAndDamping();
			}

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
			rotationMatrix = new double[,] { { 1, e21N[0], e21T[0] }, { 0, e21N[1], e21T[1] }, { 0, e21N[2], e21T[2] } };
			//Now transpose it (tranpose = inverse for a orthonormal matrix) to get the transformation

			//Why is force[0] not included here?
			//TODO: look at what's going on here, and change if needed
			double[] globalForce1 = MatrixMath.Multiply(rotationMatrix, new double[3] { force[0], force[1], force[2] });
			double[] globalForce2 = MatrixMath.Multiply(rotationMatrix, new double[3] { force[4], force[5], force[6] });

			//Save some of the results
			normForceMag = force[1];
			tanForceMag = force[2];
			momentMag = force[3];

			normForceVect = VectorMath.ScalarMultiply(-1 * normForceMag, e21N);
			tanForceVect = VectorMath.ScalarMultiply(-1 * tanForceMag, e21T);
			//Maybe someday replace this with a Moment/force conversion

			//Normal Forces: multiply by -1 because this the force reaction gets applied
			//to the fibers, while the FE equations produce the load applied...
			f1.CurrentForces.Add(VectorMath.ScalarMultiply(-1.0, globalForce1));
			f2.CurrentForces.Add(VectorMath.ScalarMultiply(-1.0, globalForce2));
			//f1.currentForces.Add(normForceVect);
			//f2.currentForces.Add(VectorMath.ScalarMultiply(-1.0, normForceVect)); 

			//Moment: again, multiply by -1 because we are applying the moment reaction,
			//while FE produces the needed moment to cause the roation
			f1.CurrentMoments.Add(-1 * force[3]);

			f2.CurrentMoments.Add(-1 * force[7]);
		}

		#endregion

		#region Private Methods

		protected bool IsThereFailureInTheSection(ref double ztop, ref double zbottom)
        {
			bool isThereFailure = false;
			double ftop = Eval(ztop);
			double fbottom = Eval(zbottom);

			//If both sides are broken (>0)
			if (ftop >= 0.0 && fbottom >= 0.0)
			{
				double zmiddle = (ztop + zbottom) / 2.0;
				double fmiddle = Eval(zmiddle);

				//If the middle is broken too, then just break the whole thing
				if (fmiddle >= 0.0)
				{
					ztop = zbottom;
					isBroken = true;
					isThereFailure = true;
				}
				//If the middle isn't broken, find the intersect in both halves
				else
				{
					//Move both edges
					FindNewZwhenOneSideIsFailed(ftop, fmiddle, ref ztop, ref zmiddle);
					FindNewZwhenOneSideIsFailed(fmiddle, fbottom, ref zmiddle, ref zbottom);

					isThereFailure = true;
				}
			}
			else if (ftop > 0.0 || fbottom > 0.0)
			{
				FindNewZwhenOneSideIsFailed(ftop, fbottom, ref ztop, ref zbottom);

				isThereFailure = true;
			}

			return isThereFailure;
		}

		protected void FindNewZwhenOneSideIsFailed(double ft, double fb, ref double zt, ref double zb)
        {
			//Debug code
			//double tempzt1 = z1;
			//double tempzt2 = z2;

			//This finds the 0 in between z1 and z2
			double zZero = myMath.RootFinding.FalsiMethod(this, zt, zb, ft, fb, 0.01, 3);

			//This moves the z of the 0 to the one that was failed
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
			double y = (checkFiber1Surface) ? matrixModel.CalculateYAtFiber1(x) : matrixModel.CalculateYAtFiber2(x);

			return failureCriteria.FailureFunction(0.0, y, x, qm, matrixModel);
        }

		/// <summary>
		/// Updates the q, which is theta_1, u2, v2, theta_2, dL
		/// </summary>
		
		protected static double ConvertAngleToRangePI_to_negPI(double initialAngle)
        {
			double convertedAngle = initialAngle % (2 * Math.PI);

            if (convertedAngle <= (2*Math.PI) && convertedAngle > (Math.PI))
			{
				convertedAngle -= (2 * Math.PI);
			}
			else if(convertedAngle >= (-2 * Math.PI) && convertedAngle < (-1*Math.PI))
			{
				convertedAngle += (2 * Math.PI);
			}

			return convertedAngle;
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
		public static double K_x(double zt1, double zt2, double zb1, double zb2, double r, double d, double b, double Ep, double nu, Fiber f1, Fiber f2)
		{

			Matrix_ElasticFiber mykx = new Matrix_ElasticFiber(r, d, b, Ep, nu, f1, f2, 1);

			double kx = mykx.Calculate_knorm(zt1, zt2, zb1, zb2);

			return kx;

		}

		protected virtual void CalculateStiffnessesAndDamping()
		{
			try
			{
				matrixModel.CalculateStiffnesses(ref k, ref d, z_t1, z_t2, z_b1, z_b2);
			}
			catch (Exception ex)
			{
				throw new System.Exception(ex.Message + $"  Fiber {nf1} and fiber {nf2}, iteration {tIndex}");
			}
			

		}

		#endregion
	} 
}