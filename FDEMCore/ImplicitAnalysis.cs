/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/7/2013
 * Time: 8:41 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using RandomMath;
using FDEMCore.Contact;
using RandomMath.NewtonRaphson;

namespace FDEMCore
{
	
	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public abstract class ImplicitAnalysis : Analysis, IMatrixFunction
	{
		#region Members
        public int nLoadSteps;
		public int maxNRIter;
		public double maxNRError;
		protected double[] totalStrain;
		public List<int> lNRIterationsPerLoadStep = new List<int>();
		public List<double> lNRErrorsPerLoadStep;
		public List<int> lCrackIterationsPerLoadStep;
		protected int nDOFPerNode = 4;
		protected int nrItCounter; //This just needs to keep moving so that it doesn't think that the contacts have been duplicated.
		protected double BCStiffness = 0.0;
		protected bool calculateBCStiffness;
		protected int maxCrackIter;
		protected int currentLoadStep; //This is for debugging: get rid of this later....

		public double[] AppliedForce; //This is just for unit testing....
		public NewtonRaphsonBase NR;


		#endregion

		#region Constructor
		public ImplicitAnalysis(int inNLoadSteps, int inNMaxNRIter, double inMaxNRError, int maxCrackIter, double[] totStrain,
								   ContactParameters inContactParameters) : base(inContactParameters)
        {		}
		protected void Initiate(int inNLoadSteps, int inNMaxNRIter, double inMaxNRError, int maxCrackIter, double[] totStrain)
		{
			nLoadSteps = inNLoadSteps;
			maxNRIter = inNMaxNRIter;
			maxNRError = inMaxNRError;
			totalStrain = totStrain;
			this.maxCrackIter = maxCrackIter;
			nrItCounter = 1;


						string convertedstring = String.Join(", ", totStrain);
			sAnalysisDetails = sType + ", Load Steps = " + nLoadSteps + ", Max NewtonRaphson Iterations = "
				+ maxNRIter + ", Max NewtonRaphson Error = " + maxNRError + ", Strain = " + convertedstring;
		}

		#endregion

		#region Analysis Methods
		override public void Analyze(List<Fiber> inlFibers, CellBoundary inCBoundary, Grid myGrid)
		{
			
			base.CreateNewAnalysis(inlFibers, inCBoundary, myGrid);

			//Get the "corrected volume" for the homogenized stress
			/*double matrixVolume = 0;
            foreach (FToFRelation matrix in interactions.LSprings)
            {
                if (matrix.breakableSpring is Contact.FToFWithMatrix ffm)
                {
					matrixVolume += ffm.matrixFiberAssembly.matrixVolume;
                }
            }
			double fiberVolume = 0;
            foreach (Fiber fiber in lFibers)
            {
				fiberVolume += Math.PI * fiber.Radius * fiber.Radius * fiber.OLength;
			}
			initialVolume = matrixVolume + fiberVolume;
			*/

			//Just keep the sizing from breaking except when I check purposefully
			interactions.CanSizingBreak = false;

			//Initialize some things
			lNRErrorsPerLoadStep = new List<double>();
			lNRIterationsPerLoadStep = new List<int>();
			lCrackIterationsPerLoadStep = new List<int>();
			lNRErrorsPerLoadStep.Add(0.0);
			lNRIterationsPerLoadStep.Add(0);
			lCrackIterationsPerLoadStep.Add(0);

			double[] strainIncrement = VectorMath.ScalarMultiply(1d / (nLoadSteps), totalStrain);
			cBoundary.StrainStep = strainIncrement;

			double[] QGlobal = new double[nDOFPerNode * lFibers.Count];
			double[] FGloabl = new double[nDOFPerNode * lFibers.Count];

			calculateBCStiffness = true;
			double[,] KGlobal = InitialSlope(FGloabl, QGlobal);
			calculateBCStiffness = false;


			for (int i = 1; i < nLoadSteps + 1; i++)
            {
                //Update the Load
                cBoundary.UpdatePosition();
				currentLoadStep = i; //This is for debugging, get rid of this later

                //Find equillibrium, with no global load.

                //Initiate a few values for the count
                bool isThereFailure = true;
                int crackIterationCount = 0;
                int totalNRIterationCounter = 0;

                //Solve the equations, then allow cracks to grow.  If there is failure, re-solve
                while (crackIterationCount < maxCrackIter && isThereFailure)
				{
					//Initial counts
					isThereFailure = false;
					crackIterationCount++;

					//Solution and storing results
                    Solve(QGlobal, FGloabl, KGlobal);
					QGlobal = NR.X;
					totalNRIterationCounter += NR.Iterations;


                    //Just For Debugging
                    //SaveTimeStepImplicit(QGlobal, NR.Iterations, crackIterationCount);

					//For now, let it break even if it hasn't reached EQBM.  Otherwise, it gives up too soon.
                    //Now check for failure, but only if the solution was converged.  Otherwise don't let it crack
                    //if (NR.Iterations < maxNRIter)
                    //{
                        foreach (FToFRelation ftof in interactions.LSprings)
                        {
                            bool didThisOneFail = ftof.BreakNonContactSpring();
                            isThereFailure = didThisOneFail || isThereFailure;
                        }
					lSprings = interactions.LSprings;
					// }
				}

                SaveTimeStepImplicit(QGlobal, totalNRIterationCounter, crackIterationCount);

				//Write out progress in the console window if it is a console application
				Console.Write($"Finished load step {i}, took {crackIterationCount} crack iterations and {totalNRIterationCounter} NR iterations.");
				//Write out the strains and stresses
				double[] tempStrain = MatrixMath.TensorToVoigtVector(homogenizedStrainForOutput[i]);
				double[] tempStress = MatrixMath.TensorToVoigtVector(homogenizedStressForOutput[i]);
				Console.WriteLine("Strain(6) / Stress(6)# " + tempStrain[0] + "," + tempStrain[1] + "," + tempStrain[2]
									+ "," + tempStrain[3] + "," + tempStrain[4] + "," + tempStrain[5]
									+ "/" + tempStress[0] + "," + tempStress[1] + "," + tempStress[2]
									+ "," + tempStress[3] + "," + tempStress[4] + "," + tempStress[5]);
			}

            lSprings = interactions.LSprings;
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}

        private void SaveTimeStepImplicit(double[] QGlobal, int totalNRIterationCounter, int crackIterationCount)
        {
            //Save the results of the NR Analysis
            lNRIterationsPerLoadStep.Add(totalNRIterationCounter);
            lNRErrorsPerLoadStep.Add(NR.FinalError);
			lCrackIterationsPerLoadStep.Add(crackIterationCount);

            ApplyDOFToFibers(QGlobal, true);
            //Record the results everywhere
            base.SaveTimeStep(ref counter);

            //Now reset the fibers
            ApplyDOFToFibers(QGlobal, false);
        }

        override public void SetTimeSteps(int nTimeS)
		{
			nLoadSteps = nTimeS;
		}

		#endregion

		#region iMatrixFunction Methods
		public virtual double[] Eval(double[] X)
        {
            
			//Rename X to Q for readability
			double[] Q = X;
			double[] F = new double[X.Length];

			//Apply displacement to the fibers
            for (int i = 0; i < lFibers.Count; i++)
			{
				lFibers[i].UpdateTimeStep(0.0);
			}
			ApplyDOFToFibers(Q, true);

			//Update the grid and all of the interactions
			interactions.UpdateGrid(nrItCounter);
			interactions.UpdateContacts(nrItCounter, 0);

			//assemble the global force vector.  multiply it by -1 because we are not using the reaction force, but the internal force.  
			for (int i = 0; i < lFibers.Count; i++)
			{
				lFibers[i].SumAndClearForces();
				lFibers[i].SumAndClearMoment();

				F[i * nDOFPerNode] -= lFibers[i].CurrentNetForce[0];
				F[i * nDOFPerNode + 1] -= lFibers[i].CurrentNetForce[1];
				F[i * nDOFPerNode + 2] -= lFibers[i].CurrentNetForce[2];
				F[i * nDOFPerNode + 3] -= lFibers[i].CurrentNetMoment;
			}

            //Calculate the BCStiffness if this is still the run to get the initial stiffness so that I get the largest of all of the slope finding methods
            if (calculateBCStiffness)
            {
				double tempStiffness = FindMaxK(F, Q) * 1.0E4;
				BCStiffness = (tempStiffness > BCStiffness) ? tempStiffness : BCStiffness;
            }
            else
            {
                //this is just for debugin... apply some external force if there is one and it isn't the BC stiffness rounds....
                if (AppliedForce != null)
                {
					F = VectorMath.Subtract(F, AppliedForce);
                }
            }

			//Apply the boundary conditions to prevent rigid body motion and make K invertible:
			//Pin the bottom-left-most fiber
			double minDist = lFibers[0].CurrentPosition[1] + lFibers[0].CurrentPosition[2];
			int minFiberIndex = 0;

            for (int i = 1; i < lFibers.Count; i++)
            {
				double dist = lFibers[i].CurrentPosition[1] + lFibers[i].CurrentPosition[2];
				if (dist < minDist)
                {
					minDist = dist;
					minFiberIndex = i;
                }
            }

			F[minFiberIndex * nDOFPerNode] += Q[minFiberIndex * nDOFPerNode] * BCStiffness;
			F[minFiberIndex * nDOFPerNode + 1] += Q[minFiberIndex * nDOFPerNode + 1] * BCStiffness;
			F[minFiberIndex * nDOFPerNode + 2] += Q[minFiberIndex * nDOFPerNode + 2] * BCStiffness;
			F[minFiberIndex * nDOFPerNode + 3] += Q[minFiberIndex * nDOFPerNode + 3] * BCStiffness;

			//move the fibers back: this is because NR is finding q from the last Q_loadstep, so we don't want that to be summed.
			ApplyDOFToFibers(Q, false);

			
			nrItCounter++;
			return F;
        }

		public override void WriteOutput(StreamWriter dataWrite, string sComment, string sCommand)
		{

			#region Now write out Iteration Information and StressStrain Data
			if (bOutputAll)
			{
				//Write the general output (Homogenized Stress/Strain)

				OutputFile.WriteHeader("GeneralOutput", sComment, sCommand, dataWrite);
				dataWrite.WriteLine(sComment + sAnalysisDetails);
				dataWrite.WriteLine(sComment + duration.ToString());
				dataWrite.WriteLine(sComment);
				dataWrite.WriteLine(sComment + "Iteration, E11, E22, E33, E23, E13, E12, S11, S22, S33, S23, S13, S12, NRIterations, NRError, CrackIterations");

				for (int i = 0; i < homogenizedStrainForOutput.Count; i++)
				{
					double[] tempStrain = MatrixMath.TensorToVoigtVector( homogenizedStrainForOutput[i]);
					double[] tempStress = MatrixMath.TensorToVoigtVector(homogenizedStressForOutput[i]);
					dataWrite.WriteLine(i + "," + tempStrain[0] + "," + tempStrain[1] + "," + tempStrain[2]
										+ "," + tempStrain[3] + "," + tempStrain[4] + "," + tempStrain[5]
										+ "," + tempStress[0] + "," + tempStress[1] + "," + tempStress[2]
										+ "," + tempStress[3] + "," + tempStress[4] + "," + tempStress[5]
										+ "," + lNRIterationsPerLoadStep[i] + "," + lNRErrorsPerLoadStep[i] + "," + lCrackIterationsPerLoadStep[i]);

				}

				for (int i = 0; i < homogenizedStrainForOutput.Count; i++)
				{
					OutputFile.WriteHeader("Iteration", sComment, sCommand, dataWrite);
					dataWrite.WriteLine(i);

					#region Now write out Fiber Information
					//TODO: but the headers into the types themselves.....
					dataWrite.WriteLine(sCommand + "Fibers");
					dataWrite.WriteLine(sComment + "Fiber Index, isProjected?, CenterX, CenterY, CenterZ, ZRot, Radius, Length");
					int j = 0;

					foreach (Fiber f in lFibers)
					{
						dataWrite.Write(j);
						f.WriteOutput(i, dataWrite);
						j++;
					}
					#endregion

					#region Now write out Contact Information
					dataWrite.WriteLine(sCommand + "Contacts");
					dataWrite.WriteLine(sComment + "nF1, nProjF1, nF2, nProjF2, Type, theta_1, u_2, v_2, theta_2, u_g, State Variables");
					foreach (FToFRelation s in lSprings)
					{
						s.WriteOutput(i, dataWrite);
					}


					#endregion

					#region Boundary Information
					dataWrite.WriteLine(sCommand + "Boundaries");
					dataWrite.WriteLine(sComment + "CenterX, CenterY, CenterZ, NormX, NormY, NormZ");
					cBoundary.WriteOutput(i, dataWrite);
					#endregion
				}
			}

			#endregion
		}
		#endregion

		#region private methods
		protected abstract double[,] InitialSlope(double[] FGloabl, double[] QGlobal);

		protected abstract void Solve(double[] FGloabl, double[] QGlobal, double[,] KGlobal);

		
		protected void ApplyDOFToFibers(double [] Q, bool addDOF)
        {
			for (int i = 0; i < lFibers.Count; i++)
			{
				double[] qlocal = VectorMath.ExtractVector(Q, i * nDOFPerNode, (i + 1) * nDOFPerNode - 1);

				if (addDOF)
				{
					lFibers[i].CurrentPosition = VectorMath.Add(lFibers[i].CurrentPosition, new double[] { qlocal[0], qlocal[1], qlocal[2] });
					lFibers[i].CurrentRotation += qlocal[3];
				}
				else
				{
					lFibers[i].CurrentPosition = VectorMath.Subtract(lFibers[i].CurrentPosition, new double[] { qlocal[0], qlocal[1], qlocal[2] });
					lFibers[i].CurrentRotation -= qlocal[3];
				}
			}
		}
		
		protected double FindMaxK(double [] F, double [] Q)
        {
			double maxK = 0;
            for (int i = 0; i < F.Length; i++)
            {
                for (int j = 0; j < Q.Length; j++)
                {
                    if (!Q[j].Equals(0.0))
                    {
						double K = Math.Abs( F[i] / Q[j]);
						maxK = (K > maxK) ? K : maxK;
					}
				}
            }
			return maxK;
        }
		#endregion
	}

	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small.  Uses the numerical tangent by calculatng the slope by going
	/// one small step in the direction of each dof at each iteration
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ImplicitAnalysis_NumericalTangent : ImplicitAnalysis
	{
		#region Members
		public double stepSizePercent;
		protected double[] QSteps;

		#endregion

		#region Constructor
		public ImplicitAnalysis_NumericalTangent(int inNLoadSteps, int inNMaxNRIter, double inMaxNRError, int maxCrackIter, double[] totStrain,
								   ContactParameters inContactParameters, double numTangentStepSizePercent)
			: base(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain, inContactParameters)
		{
			stepSizePercent = numTangentStepSizePercent;
			sType = "ImplicitNumericalTangentAnalysis";

			Initiate(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain);
		}
		#endregion

		#region Override Methods

		protected override double[,] InitialSlope(double[] FGloabl, double[] QGlobal)
		{
			QSteps = new double[nDOFPerNode * lFibers.Count];
			//Create step size based on the angles and radius....
			for (int i = 0; i < lFibers.Count; i++)
			{
				//Scale displacements with the radius
				QSteps[nDOFPerNode * i + 0] = lFibers[0].Radius * stepSizePercent;
				QSteps[nDOFPerNode * i + 1] = lFibers[0].Radius * stepSizePercent;
				QSteps[nDOFPerNode * i + 2] = lFibers[0].Radius * stepSizePercent;
				//Scale the rotation just with PI
				QSteps[nDOFPerNode * i + 3] = Math.PI * stepSizePercent;
			}

			//Create the initial slope.  This is important because it sets the penalty stiffness to a consistent value
			NR = new NewtonRaphsonNumericalTangent(FGloabl, QGlobal, this, maxNRError, QSteps, maxNRIter);
			double[,] tempKGlobal = NR.DEval(QGlobal);
			return tempKGlobal;
		}

		protected override void Solve(double[] FGloabl, double[] QGlobal, double[,] KGlobal)
		{
			NR = new NewtonRaphsonNumericalTangent(FGloabl, QGlobal, this, maxNRError, QSteps, maxNRIter);
			NR.Solve();
		}

        #endregion
    }

	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small.  Uses the numerical tangent by calculatng the numerical tangent
	/// at the beginning of each load step, then using that for the rest of the time
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ImplicitAnalysis_NumericalTangentAtLoadStep : ImplicitAnalysis
	{
		#region Members
		public double stepSizePercent;
		protected double[] QSteps;

		#endregion

		#region Constructor
		public ImplicitAnalysis_NumericalTangentAtLoadStep(int inNLoadSteps, int inNMaxNRIter, double inMaxNRError, int maxCrackIter, double[] totStrain,
								   ContactParameters inContactParameters, double numTangentStepSizePercent)
			: base(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain, inContactParameters)
		{
			stepSizePercent = numTangentStepSizePercent;
			sType = "ImplicitNumericalTangent1stLSAnalysis";

			Initiate(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain);
		}
		#endregion

		#region Override Methods

		protected override double[,] InitialSlope(double[] FGloabl, double[] QGlobal)
		{
			QSteps = new double[nDOFPerNode * lFibers.Count];

			//Create step size based on the angles and radius....
			for (int i = 0; i < lFibers.Count; i++)
			{
				//Scale displacements with the radius
				QSteps[nDOFPerNode * i + 0] = lFibers[0].Radius * stepSizePercent;
				QSteps[nDOFPerNode * i + 1] = lFibers[0].Radius * stepSizePercent;
				QSteps[nDOFPerNode * i + 2] = lFibers[0].Radius * stepSizePercent;
				//Scale the rotation just with PI
				QSteps[nDOFPerNode * i + 3] = Math.PI * stepSizePercent;
			}

			//Create the initial slope.  This is important because it sets the penalty stiffness to a consistent value
			NR = new NewtonRaphsonNumericalTangent(FGloabl, QGlobal, this, maxNRError, QSteps, maxNRIter);
			double[,] tempKGlobal = NR.DEval(QGlobal);
			return tempKGlobal;
		}

		protected override void Solve(double[] FGloabl, double[] QGlobal, double[,] KGlobal)
		{
			//Create the initial slope.  This is important because it sets the penalty stiffness to a consistent value
			
			NR = new NewtonRaphsonNumericalTangent(FGloabl, QGlobal, this, maxNRError, QSteps, maxNRIter);
			double[,] tempKGlobal = NR.DEval(QGlobal);

			NR = new NewtonRaphsonInitialSlope(FGloabl, QGlobal, this, maxNRError, maxNRIter, tempKGlobal);
			NR.Solve();
		}

		#endregion
	}

	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small.  Uses the numerical tangent by calculatng the numerical tangent
	/// at the beginning of each load step, then using that for the rest of the time
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ImplicitAnalysis_InitialNumericalTangent : ImplicitAnalysis
	{
		#region Members
		public double stepSizePercent;
		protected double[] QSteps;

		#endregion

		#region Constructor
		public ImplicitAnalysis_InitialNumericalTangent(int inNLoadSteps, int inNMaxNRIter, double inMaxNRError, int maxCrackIter, double[] totStrain,
								   ContactParameters inContactParameters, double numTangentStepSizePercent)
			: base(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain, inContactParameters)
		{
			stepSizePercent = numTangentStepSizePercent;
			sType = "ImplicitNumericalTangent1stLS";

			Initiate(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain);
		}
		#endregion

		#region Override Methods

		protected override double[,] InitialSlope(double[] FGloabl, double[] QGlobal)
		{
			QSteps = new double[nDOFPerNode * lFibers.Count];

			//Create step size based on the angles and radius....
			for (int i = 0; i < lFibers.Count; i++)
			{
				//Scale displacements with the radius
				QSteps[nDOFPerNode * i + 0] = lFibers[0].Radius * stepSizePercent;
				QSteps[nDOFPerNode * i + 1] = lFibers[0].Radius * stepSizePercent;
				QSteps[nDOFPerNode * i + 2] = lFibers[0].Radius * stepSizePercent;
				//Scale the rotation just with PI
				QSteps[nDOFPerNode * i + 3] = Math.PI * stepSizePercent;
			}

			//Create the initial slope.  This is important because it sets the penalty stiffness to a consistent value
			NR = new NewtonRaphsonNumericalTangent(FGloabl, QGlobal, this, maxNRError, QSteps, maxNRIter);
			double[,] tempKGlobal = NR.DEval(QGlobal);
			return tempKGlobal;
		}

		protected override void Solve(double[] FGloabl, double[] QGlobal, double[,] KGlobal)
		{
			//Create the initial slope.  This is important because it sets the penalty stiffness to a consistent value


			NR = new NewtonRaphsonInitialSlope(FGloabl, QGlobal, this, maxNRError, maxNRIter, KGlobal);
			NR.Solve();
		}

		#endregion
	}

	/*
	/// <summary>
	/// Performs an implicit analysis, based on the jacobian
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ImplicitAnalysis_Jacobian : ImplicitAnalysis, IMatrixFunctionAndDerivative
	{
		#region Members
		
		#endregion

		#region Constructor
		public ImplicitAnalysis_Jacobian(int inNLoadSteps, int inNMaxNRIter, double inMaxNRError, int maxCrackIter, double[] totStrain,
								   ContactParameters inContactParameters)
			: base(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain, inContactParameters)
		{
			sType = "ImplicitJacobianAnalysis";

			Initiate(inNLoadSteps, inNMaxNRIter, inMaxNRError, maxCrackIter, totStrain);
		}
		#endregion

		#region Override Methods
		public override double[,] DEval(double[] x)
		{


		}


		protected override void Solve(double[] FGloabl, double[] QGlobal, double[,] KGlobal)
		{
			NR = new NewtonRaphsonJacobian(FGloabl, QGlobal, this, maxNRError, maxNRIter);
			NR.Solve();
		}
	
		#endregion
	}
	*/
}