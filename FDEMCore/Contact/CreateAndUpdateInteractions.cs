/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 10/9/2019
 * Time: 12:50 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RandomMath;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of CreateAndUpdateInteractions.
	/// </summary>
	public abstract class CreateAndUpdateInteractions
	{
		#region Private Members
		protected Grid myGrid;
		protected List<Fiber> lFibers;
		protected CellWall [] cellWalls;
		protected int tIndex;
		protected ContactParameters contactParams;
		protected List <FToFRelation> lSprings; //This mixes the boundary and fiber to fiber springs
		protected List<double [,]> homogenizedStress;
		protected CellBoundary cellBound;
		protected bool bCanSizingBreak = true;  //This thing is set to true, and must be set to false via public property.
		protected double dT;
		
		#endregion
		
		#region Public Members
		public List<FToFRelation> LSprings {
			get { return lSprings; }
		}
		
		public List<double[,]> HomogenizedStress {
			get { return homogenizedStress; }
		}

		/// <summary>
		/// Set to true when the sizing can be broken.  Otherwise, it's force will be set to 0 but it will not break
		/// </summary>
		public bool CanSizingBreak{
			get { return bCanSizingBreak; }
			set { bCanSizingBreak = value; }
		}
		#endregion
		
		#region Constructors
		protected CreateAndUpdateInteractions(List<Fiber> inlFibers, Grid inputGrid, CellBoundary inCellBound, ContactParameters inContPar)
		{
			//Create lists for saved results
			homogenizedStress = new List<double[,]> { new double[3, 3] };
			
			cellBound = inCellBound;
			lFibers = inlFibers;
			cellWalls = inCellBound.Walls;
			contactParams = inContPar;
			myGrid = inputGrid;
			lSprings = new List<FToFRelation>();
		}
		#endregion
		
		#region Public Methods
		
		public abstract void UpdateGrid(int timeStep);
		
		public abstract void UpdateContacts(int timeStep, double dT);
		
		public void SaveTimeStep(int iSaved){
			
			foreach (FToFRelation s in lSprings) {
				s.SaveTimeStep(iSaved, tIndex);
			}
			homogenizedStress.Add(GetHomogenizedStress(iSaved));
		}

		public double [,] CurrentContactStress(){

			double [,] con = new double[3,3];
			//double volume = cellBound.Volume; //2nd PK (use current volume for cauchy)
			//double S11 = 0;
			//dou
			//ble volume = lCellWalls[0].TransDist * lCellWalls[1].TransDist; //This updates the volume each time
			//Might need to revisit this, but this is from the fibers stretching
			//This is commented out because the fiber stress is taken care of inside of fiber now
			/*
			foreach (Fiber	f in lFibers) {
				S11 += cellBound.CalculteStrain()[0] * f.Modulus1 * Math.PI * Math.Pow(f.Radius, 2.0) * f.OLength;
			}
			S11 = S11 / volume;
			*/
			foreach (FToFRelation s in lSprings) {
				con = MatrixMath.Add(con, s.CalculateCurrentContactStress (tIndex));
			}

			//double[,] stress = MatrixMath.ScalarMultiply(1.0 / (volume), con);
			//stress[0,0] += S11;//TODO Not the best, but the stress is based on the original volume
			//MatrixMath.Add(mom, con));

			return con;
		}

		#endregion
		
		#region Private Methods
		
		/// <summary>
		/// This is for a saved time step
		/// </summary>
		/// <returns>The homogenized stress.</returns>
		/// <param name="timeStep">Time step.</param>
		protected double[,] GetHomogenizedStress(int timeStep){
			
			double [,] stress = new double[3,3]; //TODO another bad part.
			double [,] con = new double[3,3];
			double volume = cellBound.Volume; //cauchy stress
			double S11 = 0;
			//double volume = lCellWalls[0].TransDist * lCellWalls[1].TransDist; //This updates the volume each time
			
			foreach (Fiber	f in lFibers) {
				S11 += cellBound.Strain[timeStep][0] * f.Modulus1 * Math.PI * Math.Pow(f.Radius, 2.0) * f.OLength;
			}
			S11 = S11 / volume;
			foreach (FToFRelation s in lSprings) {
				con = MatrixMath.Add(con, s.ContactStress(timeStep));
			}
			
			stress = MatrixMath.ScalarMultiply(1.0 / (volume), con);
			stress[0,0] = S11;//TODO Not the best, but the stress is based on the original volume
			
			return stress;
		}
		#endregion
	}
}