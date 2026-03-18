/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 14-Feb-13
 * Time: 11:59 AM
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
	/// This method is to create a friction contact spring between two fibers
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public abstract class FToFSpring:Spring
	{
		#region Private Members
		protected Fiber f1;
		protected Fiber f2;
		protected int nf1;
		protected int nf2;
        protected int npf1;
        protected int npf2;
		protected List<int> lNProjectedFiber1; //This is the number of the projected fiber 1.  It is -1 if it is not a projcted fiber, otherwise is 0, 1, or 2
		protected List<int> lNProjectedFiber2;  //This is the number of the projected fiber 2.  It is -1 if it is not a projcted fiber, otherwise is 0, 1, or 2

		#endregion

		#region Public Members

		public int Nf1 {
			get {return nf1;}
		}
		public int Nf2 {
			get {return nf2;}
		}
		#endregion
		
		#region Constructors
		/// <summary>Creates a contact spring between fibers</summary>
		protected FToFSpring(Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2) :base(){
			f1 = fiber1;
			f2 = fiber2;
			nf1 = nfiber1;
			nf2 = nfiber2;
            npf1 = -1;
            npf2 = -1;
			lNProjectedFiber2 = new List<int>();
			lNProjectedFiber1 = new List<int>();
		}
		
		#endregion
		
		#region Public Methods
		protected override void ApplyForcesAndMoments(){
			//Normal Force
			f1.CurrentForces.Add(normForceVect);
			f2.CurrentForces.Add(VectorMath.ScalarMultiply(-1.0, normForceVect));
			//Tangent Force
			f1.CurrentForces.Add(tanForceVect);
			f2.CurrentForces.Add(VectorMath.ScalarMultiply(-1.0, tanForceVect));
			//Moment
			f1.CurrentMoments.Add(momentMag);
			f2.CurrentMoments.Add(-1.0*momentMag);
		}
		
        public override void WriteOutput(int i, StreamWriter dataWrite)
        {

            if (!notYetActive)
            {
                if (lTimeSteps.Contains(i))
                {
                    int index = lTimeSteps.IndexOf(i);
                    dataWrite.Write(nf1 + "," + lNProjectedFiber1[index] + "," + nf2 + "," + lNProjectedFiber2[index]  
                                         +  "," + this.sType + "," + (this.lNormForceMag[index]) + "," );
                }
            }
        }

		public override void SaveTimeStep(int iSaved, int iCurrent)
		{
			if ((iCurrent == base.tIndex))
			{  //calling bse.tIndex is the same as checking current contact
				base.SaveTimeStep(iSaved, iCurrent);
				lNProjectedFiber1.Add(npf1);
				lNProjectedFiber2.Add(npf2);
			}
		}
		#endregion

		#region Private Methods

		#endregion

		#region Static Methods
		/// <summary>
		/// Returns shortest distance between two fibers, including all of their projections. Also, gives the distance between the two closest
		/// Fibers and their relative velocities
		/// </summary>
		public static double GetMinYZDistanceBetweenFibersIncludingProjections(Fiber fn1, Fiber fn2, ref double [] xl12_YZ, ref double [] vl12, ref double [] pt1, ref double [] pt2,
            ref int nInList1, ref int nInList2){
			
			//Make two lists with the fibers and projections
			List <IPoint> lF1 = new List<IPoint>();
			List <IPoint> lF2 = new List<IPoint>();
			
			//Add original fibers
			lF1.Add(fn1);
			lF2.Add(fn2);
			
			//Add any projections
			if (fn1.HasProjectedFibers) {
				foreach (ProjectedFiber pf1 in fn1.ProjectedFibers) {
					lF1.Add(pf1);
				}
			}
			if (fn2.HasProjectedFibers) {
				foreach (ProjectedFiber pf2 in fn2.ProjectedFibers) {
					lF2.Add(pf2);
				}
			}
			
			//Now return the minimum distance (and x and v through references)
			return GetYZDistanceFromClosestPointsBetweenTwoLists(lF1, lF2, ref xl12_YZ, ref vl12, ref pt1, ref pt2, ref nInList1, ref nInList2);
		}
		
		public static double GetMinYZDistanceBetweenFibersIncludingProjections(Fiber fn1, Fiber fn2){
			double [] xl12 = new double[3];
			double [] vl12 = new double[3];
			double [] pt1 = new double[3];
			double [] pt2 = new double[3];
            int nInList1 = 0;
            int nInList2 = 0;
			return GetMinYZDistanceBetweenFibersIncludingProjections(fn1, fn2, ref xl12, ref vl12, ref pt1, ref pt2, ref nInList1, ref nInList2);
		}
		#endregion
	}
}
