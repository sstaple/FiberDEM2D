/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 3/16/2015
 * Time: 8:14 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections.Generic;
using RandomMath;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of FiberToFiberRelation.
	/// </summary>
	[SerializableAttribute] //This allows us to make a deep copy fast
	public class FToFRelation
	{
		
		#region Private Members
		FToFSpring contactSpring;
		public FToFBreakableSpring breakableSpring; 
		private Fiber f1;
		private Fiber f2;
		private int nf1;
		private int nf2;
		#endregion
		
		#region Public Members

		public List<int> LTimeSteps {
			get {
				return contactSpring.LTimeSteps;
			}
		}

		public double ContactScaleFactor {
			get { return contactSpring.ContactScaleFactor; }
			set { contactSpring.ContactScaleFactor = value; }
		}

		public List<double> LNormForce {
			get {
				return contactSpring.LNormForce;
			}
		}

		public List<double> LTanForce {
			get {
				return contactSpring.LTanForce;
			}
		}
		
		public int Nf1 {
			get {return contactSpring.Nf1;}
		}
		public int Nf2 {
			get {return contactSpring.Nf2;}
		}

		#endregion
		
		#region Constructors
		/// <summary>
		/// This is fiber contact only, and then sizing can be added
		/// </summary>
		public FToFRelation(ContactParameters inContPar, Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
		{
			contactSpring = new FToFContactSpring(inContPar, fiber1, fiber2, nfiber1, nfiber2);
			f1 = fiber1;
			f2 = fiber2;
			nf1 = nfiber1;
			nf2 = nfiber2;
		}
		
		#endregion
		
		#region Private Methods
		
		#endregion
		
		#region Public Methods
		public bool CurrentlyInContact(double minSpacing = 0)
        {
			bool isCurrentlyInContact = false;
			if (contactSpring.CurrentlyActive)
            {
				double spacingAtContact = f1.Radius + f2.Radius;
				if(contactSpring.CenterpointDistance_YZ < (spacingAtContact - minSpacing))
                {
					isCurrentlyInContact = true;
                }
            }
			return isCurrentlyInContact;
        }

		/// <summary>
		/// This checks the distance between the two, and makes siizing if they are close enough
		/// </summary>
		/// //TODO: Put this into the sizing parameters part
		public void AddNonContactSpring(SizingParameters inSizParams, double ranNum){
			
			//First, get the distance between the two
			double distanceBetweenCenters = FToFSpring.GetMinYZDistanceBetweenFibersIncludingProjections(f1, f2);
			double dBetweenFibers = distanceBetweenCenters - f1.Radius - f2.Radius;
			//Create sizing spring if in range and if the probability is favorable
			if(dBetweenFibers < inSizParams.MaxDist && inSizParams.Probability > ranNum){
				//a little code to get rid of "residual stresses in the sizing
				if (dBetweenFibers <= 0.0) {
					distanceBetweenCenters = f1.Radius + f2.Radius;
				}
				/* reinstate if sizing ever gets fixed....
				breakableSpring = new FToFSizingSpring_EqArea(distanceBetweenCenters, inSizParams, f1, f2, nf1, nf2);
				*/
				//TODO Make a new instantiation here
			}
		}
		//Add sizing for the matrix
		public void AddNonContactSpring(MatrixAssemblyParameters inMatrixParams){
			double [] xl12 = new double[3];
			double [] vl12 = new double[3];
			double [] pt1 = new double[3];
			double [] pt2 = new double[3];
            int nInList1 = 0;
            int nInList2 = 0;
            double distanceBetweenCenters = FToFSpring.GetMinYZDistanceBetweenFibersIncludingProjections(f1, f2, ref xl12, ref vl12, ref pt1, ref pt2, ref nInList1, ref nInList2);
			
			breakableSpring = new FToFWithMatrix(distanceBetweenCenters, xl12, inMatrixParams, f1, f2, nf1, nf2);
			/* Old code: get rid of it!
			//Decide which model to use...
			if (String.Equals(inMatrixParams.ModelName, FToFMatrixContinuumSpring.Name, StringComparison.OrdinalIgnoreCase))
			{
				breakableSpring = new FToFMatrixContinuumElasticFiberSpring(distanceBetweenCenters, xl12, inMatrixParams, f1, f2, nf1, nf2);
			}
			if (String.Equals(inMatrixParams.ModelName, FToFMatrixContinuumElasticFiberSpring_Damage.Name, StringComparison.OrdinalIgnoreCase))
			{
				breakableSpring = new FToFMatrixContinuumElasticFiberSpring_Damage(distanceBetweenCenters, xl12, inMatrixParams, f1, f2, nf1, nf2);
			}
			else if (String.Equals(inMatrixParams.ModelName, FToFMatrixContinuumElasticFiberSpring.Name, StringComparison.OrdinalIgnoreCase))
			{
				breakableSpring = new FToFMatrixContinuumElasticFiberSpring(distanceBetweenCenters, xl12, inMatrixParams, f1, f2, nf1, nf2);
			}
			*/
		}
		
		/// <summary>
		/// Breaks the sizing if it has surpassed the critical length
		/// </summary>
		public bool BreakNonContactSpring(){
			bool didSpringBreak = false;
			if (breakableSpring != null && !breakableSpring.IsBroken) {
				didSpringBreak = breakableSpring.BreakSpring();
			}
			return didSpringBreak;
		}
		
		/// <summary>
		/// Update will check for contact at the current time step.  If no contact is found, the tangent spring is reset
		/// </summary>
		/// <param name="currentTimeStep">Current time step index: needed because the spring is not always active, so it tracks the time steps when it is</param>
		public void Update(int currentTimeStep, double dT){
			
			//This solves the double contact problem: when projections are touching each other, 
			//then contact is checked for projected and original, and if a spring was already updated, this prevents duplication
			if (breakableSpring == null) { 
				
				contactSpring.Update(currentTimeStep, dT); //Although this means contact won't be checked at 0 time step
			}
			else{
				if (breakableSpring.IsBroken) {
					contactSpring.Update(currentTimeStep, dT);
				}
				else{
					breakableSpring.Update(currentTimeStep, dT);
					//Also include the contact when they are contacting
					if (breakableSpring.CenterpointDistance_YZ < (f1.Radius + f2.Radius)) {
						contactSpring.Update(currentTimeStep, dT);
					}
				}
			}
		}

		/// <summary>
		/// This method saves the data from the current times step so that it can be accessed later (for example for drawing/plotting)
		/// </summary>
		/// <param name="iCurrent">index of current time step</param>
		/// <param name="iSaved">index of current saved time step</param>
		public virtual void SaveTimeStep(int iSaved, int iCurrent){
			if (breakableSpring == null) { //This solves the double contact problem: when projections are touching each other, then contact is checked for projected and original, and if a spring was already updated, this prevents duplication
				
				contactSpring.SaveTimeStep(iSaved, iCurrent);
			}
			else{
				if (breakableSpring.IsBroken) {
					contactSpring.SaveTimeStep(iSaved, iCurrent);
				}
				else{
					breakableSpring.SaveTimeStep(iSaved, iCurrent);
					contactSpring.SaveTimeStep(iSaved, iCurrent);
				}
			}
		}
		
		/// <summary>
		/// Draws a line between the two bodies
		/// </summary>
		
		public void WriteOutput(int i, StreamWriter dataWrite){
			if (breakableSpring == null) {
				
				contactSpring.WriteOutput(i, dataWrite);
			}
			else{
				breakableSpring.WriteOutput(i, dataWrite);
				contactSpring.WriteOutput(i, dataWrite);
			}
		}

		/// <summary>
		/// Returns the contact stress between the two bodies, which is a cross-product of the position and the force vector for the outer bodies
		/// </summary>
		/// <param name="nTimeStep">current time step</param>
		/// <returns>matrix with normal components on the diagnol, shear on off-diagonal</returns>
		public double [,] ContactStress(int nTimeStep){
			if (breakableSpring == null) {
				
				return contactSpring.FetchSpringStress(nTimeStep);
			}
			else{
				return MatrixMath.Add(contactSpring.FetchSpringStress(nTimeStep),
				                             breakableSpring.FetchSpringStress(nTimeStep));
			}
		}

		public double [,] CalculateCurrentContactStress(int icurrent){
			if (breakableSpring == null) {

				return contactSpring.CalculateCurrentSpringStress(icurrent);
			}
			else{
				return MatrixMath.Add(contactSpring.CalculateCurrentSpringStress(icurrent),
				                             breakableSpring.CalculateCurrentSpringStress(icurrent));
			}
		}

		public bool HasSizing(ref double kSiz){

			bool hasSiz = false;
			if (breakableSpring != null) {
				kSiz = breakableSpring.Knormal;
				hasSiz = true;
			}
			return hasSiz;
		}


		#endregion
	}
}
