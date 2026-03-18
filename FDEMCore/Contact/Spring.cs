/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/6/2013
 * Time: 8:54 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RandomMath;
using System.Collections.Generic;
using System.IO;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of Spring.
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public abstract class Spring
	{
		#region Private Members
		//Data to be saved
		protected List <int> lTimeSteps; //a list of all the time steps that the spring was there (or that the 2 bodies came into contact
		protected List <double> lNormForceMag; //a list of all of the normal force magnitudes from fiber 1 to fiber 2
		protected List <double> lTanForceMag; //a list of all of the normal force magnitudes from fiber 1 to fiber 2
		protected List <double> lMomentMag; //a list of all of the normal force magnitudes from fiber 1 to fiber 2
		protected List <double[]> lp1;  //list of all of the locations of body 1
		protected List <double[]> lp2;  //'' of body 2
		protected List<double [,]> lSpringStress; //The stress due to the spring based on forces only (not moments)
		
		//Geometry
		protected double [] x12_YZ; //fiber 1 center minus fiber 2 (with x taken out)
		protected double [] v12; //body 1 velocity minus body 2 velocity
		protected double [] e12N_YZ; //normalized x12
		protected double centerpointDistance_YZ; //current distance of overlap between 2 parts
		
		
		//Forces
		protected double normForceMag; //current magnitude of the normal force
		protected double tanForceMag; //current magnitude of the Tangential force
		protected double momentMag;  //current magnitude of the moment
		protected double [] normForceVect;
		protected double [] tanForceVect;
		//Stiffnesses
		protected double kn12; // normal spring stiffness
		protected double kt12;  //tangential spring stiffness
		protected double krot12;  //rotational spring stiffness
		//damping
		protected double dn12;  //normal damping coefficient
		protected double dt12;  //tangential damping coefficient
		protected double drot12;  //rotational damping coefficient
		protected double dCoefficient; //global coefficient for all directions, 1.0 for critical damping, 0 for no damping
		//Other
		protected int tIndex;  //index of current iteration.  Needed for getting info out of the bodies (eg. fibers)
		protected bool notYetActive;  //true = there hasn't been contact between the two bodies yet
		protected bool currentlyActive; //true = there is currently contact between the two bodies
		protected double[] p1;  //location of body 1 (needed because there are projections, so location is not soley determined by fiber location)
		protected double[] p2;  //location of body 2
		protected double contactScaleFactor = 1;  //size the contact pen thickness (pensize = maxNormForce / contactScaleFactor
		protected double currDT; //Current time step
		
		protected string sType = "Spring";
		
		#endregion
		
		#region Public Members

		public List<int> LTimeSteps {
			get {
				return lTimeSteps;
			}
		}
		public double CenterpointDistance_YZ {
			get {
				return centerpointDistance_YZ;
			}
		}
		public double ContactScaleFactor {
			get { return contactScaleFactor; }
			set { contactScaleFactor = value; }
		}

		public List<double> LNormForce {
			get {
				return lNormForceMag;
			}
		}

		public List<double> LTanForce {
			get {
				return lTanForceMag;
			}
		}
		public double Knormal{get { return kn12;}}
		
		public bool CurrentlyActive { get { return currentlyActive; } }
		#endregion
		
		#region Constructors
		/// <summary>
		/// This is to initialize a spring.  Spring is not necessarily active, but has been created
		/// </summary>
		protected Spring()
		{
			//Initialize Saved Data
			lTimeSteps = new List<int>();
			lNormForceMag = new List<double>();
			lTanForceMag = new List<double>();
			lMomentMag  = new List<double>();
			lp1 = new List<double[]>();
			lp2 = new List<double[]>();
			lSpringStress = new List<double[,]>();
			notYetActive = true;
			currentlyActive = false;
		}
		#endregion
		
		#region Private Methods
		
		/// <summary>This method is called when contact has been found.  It sets the current time step, sets up the contact vectors, finds the normal
		/// and tangential forces, and updates the tangential spring.</summary>
		protected void IsFoundToBeActive(double centerptDist, double [] pt1, double [] pt2, double [] x1Tox2, double [] v1Tov2, double [] normx1Tox2, int currentTimeStep){
			
			//Save a bunch of values that were calculated in the distance check
			tIndex = currentTimeStep;
			
			notYetActive = false;
			centerpointDistance_YZ = centerptDist;
			x12_YZ = x1Tox2;
			v12 = v1Tov2;
			e12N_YZ = normx1Tox2;
			p1 = pt1;
			p2 = pt2;
			
			UpdateInternalValues();
			CalculateForcesAndMoments();
			ApplyForcesAndMoments();
			IncrementInternalValues();
			
		}
		
		//Abstract Members for use in IsFoundToBeActive
		protected abstract void UpdateInternalValues();
		protected abstract void CalculateForcesAndMoments();
		protected abstract void ApplyForcesAndMoments();
		protected abstract void IncrementInternalValues();
		protected abstract void InitiateInternalValues();
		
		#endregion
		
		#region Public Methods
		
		public abstract bool IsSpringActive(int currentTimeStep);
		
		/// <summary>
		/// Update will check for contact at the current time step.  If no contact is found, the tangent spring is reset
		/// </summary>
		/// <param name="currentTimeStep">Current time step index: needed because the spring is not always active, so it tracks the time steps when it is</param>
		public void Update(int currentTimeStep, double timeStep){
			currDT = timeStep;
			
			if (currentTimeStep != tIndex) { //This solves the double contact problem: when projections are touching each other, then contact is checked for projected and original, and if a spring was already updated, this prevents duplication
				
				if (!currentlyActive) {
					InitiateInternalValues();
				}
				
				currentlyActive = IsSpringActive(currentTimeStep);
				
			}
		}
		
		/// <summary>
		/// This method saves the data from the current times step so that it can be accessed later (for example for drawing/plotting)
		/// </summary>
		public virtual void SaveTimeStep(int iSaved, int iCurrent){
			
			if (!notYetActive && currentlyActive && (iCurrent == tIndex)) {
				
				//Store if there has been contact and there was contact this round
				lNormForceMag.Add(normForceMag);
				lTanForceMag.Add(tanForceMag);
				lMomentMag.Add(momentMag);
				lTimeSteps.Add(iSaved);
				lp1.Add(p1);
				lp2.Add(p2);
				double[,] cs = CalculateCurrentSpringStress(iCurrent);
				lSpringStress.Add(cs); //send empty array if neither are
			}
		}
		
		public virtual void WriteOutput(int i, StreamWriter dataWrite){
			
			if (!notYetActive) {
				if (lTimeSteps.Contains(i)) {
					int index = lTimeSteps.IndexOf(i);
					dataWrite.WriteLine(lp1[index][0]+ "," + lp1[index][1] + "," + lp1[index][2]
					                    + "," + lp2[index][0] + "," + lp2[index][1] + "," + lp2[index][2]
					                    + "," + (this.lNormForceMag[index]) +
					                    "," + this.sType);
				}
			}
		}
		
		/// <summary>
		/// Returns the contact stress between the two bodies, which is a cross-product of the position and the force vector for the outer bodies
		/// </summary>
		/// <param name="nTimeStep">current time step</param>
		/// <returns>matrix with normal components on the diagnol, shear on off-diagonal</returns>
		public virtual double [,] FetchSpringStress(int nTimeStep){
			double [,] cs = new double[3,3];
			if (lTimeSteps.Contains(nTimeStep)) {
				int index = lTimeSteps.IndexOf(nTimeStep);
				cs = lSpringStress[index];
			}
			return cs;
		}

		/// <summary>
		/// This gives you the current contact stress without saving anything else
		/// </summary>
		/// <param name="iCurrent">current time step index</param>
		/// <returns>The current contact stress.</returns>
		public virtual double [,] CalculateCurrentSpringStress(int iCurrent){
			
			double [,] cs = new double[3,3];
			
			if (!notYetActive && currentlyActive && (iCurrent == tIndex)) {

				double [] fTot = VectorMath.Add(normForceVect, tanForceVect);
				
				double [,] xCx = MatrixMath.ScalarMultiply(0.5, MatrixMath.Add(VectorMath.Cross(x12_YZ, fTot),
				                                                               VectorMath.Cross(fTot, x12_YZ)));// Carsten
				//double [,] xCx = VectorMath.Cross(fNorm, x12);
				cs = MatrixMath.ScalarMultiply(-1.0, xCx); //Check sign here: who knows?????
			}
			
			return cs;
		}
		
		
		#endregion
		
		#region Static Methods
		
		/// <summary>
		/// Rotates a vector on the yz plane
		/// </summary>
		/// <param name="angle">angle which will be rotated on the yz plane</param>
		/// <param name="a">vector (3-d, with x, y, z components)</param>
		/// <returns>rotated vector</returns>
		public static double []  VectorRotationYZ(double angle, double [] a){
			if (a.Length != 3) {
				throw new Exception("Only for a 3-D Vector");
			}
			double s = Math.Sin(angle);
			double c = Math.Cos(angle);
			double [,] T = new double[3,3]{{c, -1.0 * s, 0.0},{s, c, 0.0},{0.0, 0.0, 1.0}};
			return MatrixMath.Multiply(T, a);
        }

        public static double[] NormalizeVector(double[] a)
        {
			double [] b = VectorMath.DeepCopy(a);
			VectorMath.NormalizeVector(ref b);
			return b;
		}
		
		public static double SetCriticalDamping(double M, double K, double dCoeff){
			
			double contactDamp =  dCoeff *  Math.Sqrt(M*K); //for critical damping
            //Keep the d from being infinity
            if (K.Equals(0.0))
            {
                contactDamp = 0.0;
            }
			return contactDamp;
		}

        public static double GetDistanceBetweenLocations(double[] l1, double[] l2, ref double[] xl12_YZ)
        {
            xl12_YZ = VectorMath.Subtract(l1, l2); 
			double distance = VectorMath.Norm(xl12_YZ);
			return distance;
		}

        public static double GetYZDistanceFromClosestPointsBetweenTwoLists(List<IPoint> lP1, List<IPoint> lP2, ref double[] xl12_YZ, ref double[] vl12, ref double[] pt1, ref double[] pt2, 
            ref int nList1, ref int nList2)
        {
			
			nList1 = 0;
			nList2 = 0;
			double [] x1Temp = new double[]{0, lP1[0].CurrentPosition[1] ,lP1[0].CurrentPosition[2]};
			double [] x2Temp = new double[]{0, lP2[0].CurrentPosition[1] ,lP2[0].CurrentPosition[2]};
			double minDist_YZ = GetDistanceBetweenLocations(x1Temp, x2Temp, ref xl12_YZ);
			
			for (int i = 0; i < lP1.Count; i++) {
				for (int j = 0; j < lP2.Count; j++) {
					
					double [] tempXl12 = new double[3];
					x1Temp = new double[]{0, lP1[i].CurrentPosition[1] ,lP1[i].CurrentPosition[2]};
					x2Temp = new double[]{0, lP2[j].CurrentPosition[1] ,lP2[j].CurrentPosition[2]};
					double currDist = GetDistanceBetweenLocations(x1Temp, x2Temp, ref tempXl12);
					
					//Add this requirement to avoid oscillation between two projection pairs
					if ((currDist - minDist_YZ) < (-1.0 * 10e-9)) {
						minDist_YZ = currDist;
						xl12_YZ = tempXl12;
						nList1 = i;
						nList2 = j;
					}
				}
			}
			vl12 = VectorMath.Subtract(lP1[nList1].CurrentVelocity, lP2[nList2].CurrentVelocity);
			pt1 = lP1[nList1].CurrentPosition;
			pt2 = lP2[nList2].CurrentPosition;
			
			//Now give out minimum distance
			return minDist_YZ;
		}
		#endregion
	}
}




