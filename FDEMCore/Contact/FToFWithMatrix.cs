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
using RandomMath;
using System.IO;
using FDEMCore.Contact.FailureTheories;
using System.Linq;
using FDEMCore.Contact.MatrixModels;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of FToFMatrixContinuumSpring.
	/// </summary>
	public class FToFWithMatrix : FToFBreakableSpring
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
		//Order is: {Theta1, u_f2, v_f2, w_f2, u_both, sin(rot_f1), 1-cos(rot_f1), sin(rot_f2), 1-cos(rot_f2)}
		protected double centerpointDistance_X; //current z distance of two fibers.  This is a seperate variable because the pts can be projections
		protected double[] q;
		protected double[] q_dot; //dof velocity
		protected double[] q_prev; //pref dof value

		//Save the state variables
		protected double[] stateVariables;
		protected double[] stateVariables_prev;

		//stiffness matrix
		protected double[,] k;

		//damping matrix
		protected double[,] d;

		//force vector {Fx, Fy, Fz, Mx}
		public double[] force;

		public MatrixFiberAssembly matrixFiberAssembly;

		//Geometry
		protected double[] norm_vT; //Tangential to the contact plane
		protected double[] e21T; //Tangential to the contact plane
		protected double[] vrelT; //relative tangential velocities of the 2 bodies (3-d)
		protected double vrelN; //relative normal velocities of the 2 bodies (3-d)
		protected double[] e21N;
		protected double[,] rotationMatrix;

		//Need additional moment for fiber 2 because the vertical displacement results in non-symmetric moments
		protected double momentMag_f2;

		//Outputs to Save
		protected List<double[]> lq;
		protected List<double[]> lStateVariables;

		#endregion

		#region Public Members
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
		public FToFWithMatrix(double initialCenterlineDistance, double[] x12, MatrixAssemblyParameters matParams,
										 Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
			: base(fiber1, fiber2, nfiber1, nfiber2)
		{
			//Set Constants for the initial geometry

			r = f1.Radius;
			b = f1.OLength;
			dCoefficient = matParams.DampCoeff;
			isBroken = false;
			currentlyActive = true;

			//Set the lists of data to be saved
			lq = new List<double[]>();
			lStateVariables = new List<double[]>();

			//Set the initial conditions
			q_prev = new double[5];
			q = new double[5];
			q_dot = new double[5];
			theta0_f1 = f1.CurrentRotation;
			theta0_f2 = f2.CurrentRotation;
			x12_YZ = x12;
			theta0_12 = Math.Atan2(-1.0 * x12_YZ[2], -1.0 * x12_YZ[1]);
			d_initial = initialCenterlineDistance;
			lx_f1_initial = f1.CurrentLength;
			dx_initial = f2.CurrentPosition[0] - f1.CurrentPosition[0];

			//TODO: Change this!!!
			matrixFiberAssembly = new MatrixFiberAssembly(initialCenterlineDistance, b, matParams, fiber1, fiber2, out stateVariables);

			//Set the stiffness
			this.CalculateStiffnessesAndDamping();

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
				centerpointDistance_X = pt2[0] - pt1[0];

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
			stateVariables_prev = (double[])(stateVariables.Clone());
		}

		protected override void UpdateInternalValues()
		{
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

			/*  This is when the angle is not part of it...
			
			//Calculate Angles
			//This always puts the angle in -pi to pi.
			double theta_12 = Math.Atan2(x2m1[2], x2m1[1]);
			double theta_12t = ConvertAngleToRangePI_to_negPI(theta_12 - theta0_12);

			//Convert the other angles to -pi to pi.  WARNING: this does not allow twists greater than 180 degrees!!!  For something really 
			//General, this formulation needs to changed to an incremental one.
			double thetaf1t = ConvertAngleToRangePI_to_negPI(f1.CurrentRotation - theta0_f1);
			double thetaf2t = ConvertAngleToRangePI_to_negPI(f2.CurrentRotation - theta0_f2);

			//The coordinate system is aligned with thetaf1t!!!

			//The theta2 rotation is relative to the first fiber
			double dTheta_f2 = thetaf2t - thetaf1t;

			//Now this is the angle between the fiber 1 coordinate system and the 12 coordinate system (between fiber 1 and 2)
			double dTheta_12 = theta_12t - thetaf1t;

			double globalTheta12 = theta_12 + thetaf1t;
			rotationMatrix = new double[3,3]{ { 1, 0, 0}, { 0, Math.Cos(globalTheta12), -1.0*Math.Sin(globalTheta12)},
				{ 0, Math.Sin(globalTheta12), Math.Cos(globalTheta12)} };
			 

			//set the degrees of freedom
			q[0] = (f2.CurrentPosition[0] - f1.CurrentPosition[0]) - dx_initial;
			q[1] = centerpointDistance_YZ - d_initial * Math.Cos(dTheta_12);
			q[2] = d_initial * Math.Sin(dTheta_12);
			q[3] = dTheta_f2;
			q[4] = f1.CurrentLength - lx_f1_initial;
			* */

			//Calculate Angles
			//Convert the other angles to -pi to pi.  WARNING: this does not allow twists greater than 180 degrees!!!  For something really 
			//General, this formulation needs to changed to an incremental one.
			double theta_12 = Math.Atan2(x2m1[2], x2m1[1]);
			//Do this bit of math because fibers that are on the boundary between pi and -pi get arbitrarily large values
			//under the "right" circumstances.  This way, two fibers can not fully circle each other though.
			double dTheta_12a = (theta_12 - theta0_12);
			double dTheta_12b = dTheta_12a >= 0 ? dTheta_12a - 2.0 * Math.PI : dTheta_12a + 2.0 * Math.PI;
			//Take the smallest of the two
			double dTheta_12 = Math.Abs(dTheta_12a) > Math.Abs(dTheta_12b) ? dTheta_12b : dTheta_12a;

			double dTheta_f2 = (f2.CurrentRotation - theta0_f2) - dTheta_12;
			double dTheta_f1 = (f1.CurrentRotation - theta0_f1) - dTheta_12;

			//set the degrees of freedom
			q[0] = dTheta_f1;
			q[1] = centerpointDistance_X - dx_initial;
			q[2] = centerpointDistance_YZ - d_initial;
			q[3] = dTheta_f2;
			q[4] = f1.CurrentLength - lx_f1_initial;

			//Set the velocity of the degrees of freedom based on the previous and current values
			q_dot = VectorMath.ScalarMultiply(1 / currDT, VectorMath.Subtract(q, q_prev));


			this.CalculateStiffnessesAndDamping();
		}

		protected override void CalculateForcesAndMoments()
		{
			//Calculate the stiffness if it is null.  This was added to get the calculation out of the constructor
			/*if ((k == null || k.Length == 0))
			{
				CalculateStiffnessesAndDamping();
			}*/

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

			//Force vector is: M1, Fx2, Fy2, M2, 
			double[] globalForce = MatrixMath.Multiply(rotationMatrix, new double[3] { force[1], force[2], 0 });
			//double[] globalForce = MatrixMath.Multiply(rotationMatrix, new double[3] { force[0], force[1], force[2] });

			//Save some of the results
			normForceMag = force[2];
			tanForceMag = 0;
			momentMag = force[3];

			if (force[3] > 0)
			{
				bool stophere = true;
			}

			normForceVect = VectorMath.ScalarMultiply(normForceMag, e21N);
			tanForceVect = VectorMath.ScalarMultiply(tanForceMag, e21T);
			//Maybe someday replace this with a Moment/force conversion

			//Normal Forces: multiply by -1 because this the force reaction gets applied
			//to the fibers, while the FE equations produce the load applied...
			f1.CurrentForces.Add(globalForce);
			f2.CurrentForces.Add(VectorMath.ScalarMultiply(-1.0, globalForce));
			//f1.currentForces.Add(normForceVect);
			//f2.currentForces.Add(VectorMath.ScalarMultiply(-1.0, normForceVect)); 

			//Moment: again, multiply by -1 because we are applying the moment reaction,
			//while FE produces the needed moment to cause the roation


			f1.CurrentMoments.Add(-1 * force[0]);

			//f2.CurrentMoments.Add(-1 * force[3]);
			f2.CurrentMoments.Add(-1 * force[3]);
		}

		public override void WriteOutput(int nTimeStep, StreamWriter dataWrite)
		{

			if (!notYetActive && lTimeSteps.Contains(nTimeStep))
			{

				int index = lTimeSteps.IndexOf(nTimeStep);

				//Write this just the first time
				if (index == 0)
				{
					dataWrite.Write(nf1 + "," + npf1 + "," + nf2 + "," + npf2);
					matrixFiberAssembly.WriteFirstIterationOutput(dataWrite);
				}

				//This is the info that we include otherwise (1st time too) //Notice we keep the ug from the original u
				dataWrite.Write(nf1 + "," + lNProjectedFiber1[index] + "," + nf2 + "," + lNProjectedFiber2[index] + ", m");

				foreach (double qVal in lq[index])
				{
					dataWrite.Write("," + qVal);
				}
				foreach (double dValue in lStateVariables[index])
				{
					dataWrite.Write("," + dValue);
				}
				dataWrite.WriteLine();

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
				lStateVariables.Add(stateVariables_prev);
			}
		}

		public override bool BreakSpring()
		{

			bool isBroken = matrixFiberAssembly.IsItBroken(ref stateVariables);

			return isBroken;
		}

		public override double[,] CalculateCurrentSpringStress(int iCurrent)
		{
			double[,] cs = new double[3, 3];

			if (!notYetActive && currentlyActive && (iCurrent == tIndex))
			{
				//Now calculate the contributions of matrix/fibers
				//11, 22, 33, 23, 13, 12
				matrixFiberAssembly.IntegralOfOutOfPlaneStressOverVolume(out double[] SdV_fiber1, out double[] SdV_fiber2, out double[] SdV_mattrix);

				//This is the contributions from the axial stresses and if the force homogenization is used.  This gets rid of the axial stuff....
				//cs = base.CalculateCurrentSpringStress(iCurrent);
				//cs = base.CalculateCurrentSpringStress(iCurrent);
				//SdV_mattrix = new double[6] {1.0*SdV_mattrix[0], 0, 1.0 * SdV_mattrix[2], 0, SdV_mattrix[4], SdV_mattrix[5] };
				//SdV_fiber1 = new double[6] { SdV_fiber1[0], SdV_fiber1[1], 0.0, SdV_fiber1[3], SdV_fiber1[4], SdV_fiber1[5] }; //1.750
				//SdV_fiber2 = new double[6] { SdV_fiber2[0], SdV_fiber2[1], 0.0, SdV_fiber2[3], SdV_fiber2[4], SdV_fiber2[5] };
				//Multiply by 2 because it's only half of the area.  Assume that it is the same stress over the whole thing??? (Do this in Fiber)
				//SdV_fiber1 = VectorMath.ScalarMultiply(2.0, SdV_fiber1);
				//SdV_fiber2 = VectorMath.ScalarMultiply(2.0, SdV_fiber2);

				double[,] STensorMatrix = MatrixMath.VoigtVectorToTensor(SdV_mattrix);
				double[,] STensorF1 = MatrixMath.VoigtVectorToTensor(SdV_fiber1);
				double[,] STensorF2 = MatrixMath.VoigtVectorToTensor(SdV_fiber2);

				//Rotate the vectors into the global coordinate system
				double[,] rotMatrixTranspose = MatrixMath.Transpose(rotationMatrix);

				double[,] rotatedTransverseStressMatrix = MatrixMath.Multiply(rotationMatrix, MatrixMath.Multiply(STensorMatrix, rotMatrixTranspose));
				double[,] rotatedTransverseStressF1 = MatrixMath.Multiply(rotationMatrix, MatrixMath.Multiply(STensorF1, rotMatrixTranspose));
				double[,] rotatedTransverseStressF2 = MatrixMath.Multiply(rotationMatrix, MatrixMath.Multiply(STensorF2, rotMatrixTranspose));

				//TODO: Get rid of the current calculations
				//Fiber components need to be dealt with at the fiber level, because it needs to be averaged over all connections that the fiber has
				f1.HomogenizedOutOfPlaneStress.Add(rotatedTransverseStressF1);
				f2.HomogenizedOutOfPlaneStress.Add(rotatedTransverseStressF2);

				//Now add the matrix component only (fiber component added later)
				//cs = MatrixMath.Add(cs, rotatedTransverseStressMatrix);
				cs = rotatedTransverseStressMatrix;

			}
			return cs;
		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Stiffness in the x-direction.
		/// </summary>
		public static double K_x(double r, double b, MatrixAssemblyParameters myMatrix, Fiber f1, Fiber f2)
		{
			//Create a temporary object???
			//Then get the stiffness, and calculate the highest stiffness component
			MatrixFiberAssembly tempAssembly = new MatrixFiberAssembly(2.0000001 * r, b, myMatrix, f1, f2, out double[] initialStateVariables);
			double kx = tempAssembly.Calculate_knorm();

			return kx;
		}

		protected void CalculateStiffnessesAndDamping()
		{
			try
			{
				matrixFiberAssembly.SetIteration(ref k, ref d, q, stateVariables_prev);
			}
			catch (Exception ex)
			{
				throw new System.Exception(ex.Message + $"  Fiber {nf1} and fiber {nf2}, iteration {tIndex}");
			}
		}

		protected static double ConvertAngleToRangePI_to_negPI(double initialAngle)
		{
			double convertedAngle = initialAngle % (2 * Math.PI);

			if (convertedAngle <= (2 * Math.PI) && convertedAngle > (Math.PI))
			{
				convertedAngle -= (2 * Math.PI);
			}
			else if (convertedAngle >= (-2 * Math.PI) && convertedAngle < (-1 * Math.PI))
			{
				convertedAngle += (2 * Math.PI);
			}

			return convertedAngle;
		}


		#endregion

		#region Static Methods
		static public void UpdateMatrix(ref List<FToFRelation> lSprings, int currentTimeStep, double dT)
		{

			foreach (FToFRelation s in lSprings)
			{

				s.Update(currentTimeStep, dT);
			}

		}

		static public void UpdateProjectedFibers(ref List<Fiber> lFibers, CellBoundary cb, List<MatrixProjectedFiber> lMatrixProjFibers)
		{

			//Make this horrible projections array that coordinates with the numbers assigned in CreateMatrixPairs()
			//TODO: this is sloppy coding!!!  Change this someday.  It has too many built in indices (1 is already too many!)
			/*double[] projec2 = cb.UndefXtoDefx(cb.Walls[2].PeriodicProjection);
			double[] projec3 = cb.UndefXtoDefx(cb.Walls[3].PeriodicProjection);
			double[] projec4 = cb.UndefXtoDefx(cb.Walls[4].PeriodicProjection);*/
			double[] projec2 = cb.Walls[2].PeriodicProjection;
			double[] projec3 = cb.Walls[3].PeriodicProjection;
			double[] projec4 = cb.Walls[4].PeriodicProjection;

			double[][] projections = new double[4][]{projec2, projec4,
				VectorMath.Add(projec2, projec4),
				VectorMath.Add(projec3, projec4)};

			//Clear the current projected fibers
			foreach (Fiber fiber in lFibers)
			{
				fiber.ClearProjectedFibers();
			}

			//Add all of the projected fibers (assumes the fibers don't cross the boundaries)
			foreach (MatrixProjectedFiber mpf in lMatrixProjFibers)
			{

				lFibers[mpf.FiberIndex].AddProjectedFiber(projections[mpf.ProjectionIndex], mpf.cellWallIndices);
			}

		}

		#endregion

	}

	public class MatrixProjectedFiber
	{
		public int FiberIndex;
		public int ProjectionIndex;
		public int[] cellWallIndices;
		public MatrixProjectedFiber(int fiberIndex, int projIndex)
		{
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
					cellWallIndices = new int[] { 4, 2 };
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
    