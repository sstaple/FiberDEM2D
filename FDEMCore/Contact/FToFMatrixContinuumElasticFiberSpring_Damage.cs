using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FDEMCore.Contact.FailureTheories;
using myMath;

namespace FDEMCore.Contact
{
    public class FToFMatrixContinuumElasticFiberSpring_Damage : FToFMatrixContinuumElasticFiberSpring
	{
		#region Private Members

		//stiffness matrix
		protected Matrix_ElasticFiber_Damage matrixModel_Damage;

		//Outputs to Save
		protected List<double[]> lDamage;

		#endregion

		#region Public Members

		public new static string Name = "MatrixContinuumElasticFibers_Damage";
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
		public FToFMatrixContinuumElasticFiberSpring_Damage(double initialCenterlineDistance, double[] x12, MatrixContinuumParameters matParams,
										 Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
			: base(initialCenterlineDistance, x12,  matParams, fiber1, fiber2, nfiber1, nfiber2)
		{
			
			//Make sure the failre theory was the correct one....
			double strength, fractureEnergy, damageAccelerationCoefficient;
			int nIntPts;
            try
            {
				DamageFracturEnergyAndStrength ft = (DamageFracturEnergyAndStrength)(matParams.FailureTheory);
				strength = ft.Strength;
				fractureEnergy = ft.CriticalFractureEnergy;
				nIntPts = ft.NumberOfIntegrationPoints;
				damageAccelerationCoefficient = ft.DamageAccelerationCoefficient;
            }
            catch (Exception)
            {

                throw new Exception("The failure Theory must be DamageFracturEnergyAndStrength for the damage model");
            }

			//Set the lists of data to be saved
			lDamage = new List<double[]>();

			//Set the stiffness
			matrixModel_Damage = new Matrix_ElasticFiber_Damage(nIntPts, r, d_initial, b, E, nu, f1, f2, dCoefficient, z_t1, z_t2, z_b1, z_b2, strength, fractureEnergy, damageAccelerationCoefficient);

			matrixModel = matrixModel_Damage;
		}
		#endregion

		#region Public Methods

		public override void WriteOutput(int nTimeStep, StreamWriter dataWrite)
		{

			if (!notYetActive && lTimeSteps.Contains(nTimeStep))
			{

				int index = lTimeSteps.IndexOf(nTimeStep);

				//Write this just the first time
				if (index == 0)
				{
					dataWrite.WriteLine(nf1 + "," + npf1 + "," + nf2 + "," + npf2
											 + "," + Name + "," + (d_initial) + "," + E + "," + G + "," + z_t1 + "," + z_t2 + "," + z_b1 + "," + z_b2);
				}

				//This is the info that we include otherwise (1st time too) //Notice we add the ug from the original u
				dataWrite.Write(nf1 + "," + lNProjectedFiber1[index] + "," + nf2 + "," + lNProjectedFiber2[index]
											 + "," + "m" + ", " + lqm[index][0] + "," + lqm[index][1]
					+ "," + lqm[index][2] + "," + lqm[index][3] + "," + lqm[index][4] + "," + lqm[index][5]
					+ "," + lqm[index][6] + "," + lqm[index][7]
					+ "," + lq[index][4]);
                foreach (double dValue in lDamage[index])
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
			   isQmUpdated = true;
				lDamage.Add(matrixModel_Damage.Damage);
				base.SaveTimeStep(iSaved, iCurrent);
			}
		}

		public override bool BreakSpring()
		{

			bool isThereFailure = true;

			if (!isQmUpdated)
			{
				qm = matrixModel.CalculateMatrixDOF(q);
				isQmUpdated = true;
			}

			//Think I need this to have it skipped in further calculations
			isThereFailure = matrixModel_Damage.UpdateDamage(qm);

            if (isThereFailure)
            {
				CalculateStiffnessesAndDamping();
			}

			//Put in stuff here to check if the damage is all 1
			double[] Dn = matrixModel_Damage.Damage;
			isBroken = true;
            foreach (double dn in Dn)
            {
                if (!Double.Equals(dn, 1.0))
                {
					isBroken = false;
                }
            }

			return isThereFailure;
		}

		#endregion

		#region Private Methods

		#endregion

		#region Private Methods for getting stiffness

		#endregion
	}
}