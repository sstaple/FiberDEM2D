/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 10/9/2019
 * Time: 10:27 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
/*LOOK INTO THIS AGAIN IF DOING SOMETHING WITH SIZING.....
using System;
using myMath;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.IO;

namespace FiberDEM.Contact
{
	/// <summary>
	/// This model is for the sizing where I make a beam/truss that has an equivalent area.  This was the first sizing model created.
	/// </summary>
	public class FToFSizingSpring_EqArea : FToFBreakableSpring
	{
		
		#region Private Members
		//Defines Failure
		protected double maxF;
		protected double maxDL;  //This is the maximum possible dl that it can have in the normal direction (although it doesn't have to go this far)
		protected bool isZeroForce; //Different than "isBroken" because it is only broken when BreakSizing() is called
		protected double failureStress;
		
		protected double l0;
		protected double relRotVel;
		protected double rotSpring;
		
		protected double currentSizingStress;
		protected double xSectAreaEquivalent;
		protected double initialCenterlineDist;
		
		#endregion
		
		#region Public Members
		
		
		#endregion
		
		#region Constructors
		public FToFSizingSpring_EqArea(double initialCenterlineDistance, SizingParameters inSizingParams, Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
			: base(fiber1, fiber2, nfiber1, nfiber2)
		{
			this.initialCenterlineDist = initialCenterlineDistance;
			l0 = initialCenterlineDistance - fiber1.Radius - fiber2.Radius;
			failureStress = inSizingParams.MaxStress;
			CalculateSizingKF(ref kn12, ref kt12, ref krot12, ref maxF, ref xSectAreaEquivalent, initialCenterlineDistance,
				inSizingParams.E, inSizingParams.Nu, inSizingParams.MaxStress, f1.Radius, f2.Radius, f1.OLength);
			maxDL = maxF / kn12;
			currentlyActive = true;
			isBroken = false;
			dCoefficient = inSizingParams.DampCoeff;
			
			sType = "Sizing";
			rotSpring = 0.0;

		}
		#endregion
		
		#region Override Methods
		
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
				IsFoundToBeActive(centerpointDistance_YZ, f1.CurrentPosition, f2.CurrentPosition, xl12, vl12, e12Ntemp, currentTimeStep);
				
			}
			return !isBroken;
		}
		
		protected override void UpdateInternalValues(){
			
		}
		
		protected override void CalculateForcesAndMoments(){

			//First calculate forces and moments:
			CalculateNormalForce();


			#region Check to see if it broke

			//TODO Change failure criteria??????  And make this it's own function
			currentStress = stressFromForces(normForceMag, tanForceMag);
			if (failureStress > currentStress && !isBroken)
			{

				#region Add the normal Force
				fNorm = VectorMath.ScalarMultiply(normForceMag, e12N);
				f1.currentForces.Add(VectorMath.DeepCopy(fNorm));
				f2.currentForces.Add(VectorMath.ScalarMultiply(-1.0, fNorm));
				#endregion

				#region add Tangent Forces / Moments
				//It's never slideing...
				isSliding = false;

				fTan = VectorMath.ScalarMultiply(tanForceMag, tan);

				//Now Remove the x component for moment (moment only in yz plane)
				double[] fTanYZ = new double[3] { 0, fTan[1], fTan[2] };
				double fMom = VectorMath.VectorProduct(fTanYZ, e12N)[0];

				AssignTanLoadToFiber(fMom, fTan, f1);               //Carsten
				AssignTanLoadToFiber(fMom, VectorMath.ScalarMultiply(-1d, fTan), f2); //Carsten

				#region Find Moment Due to rotation diff
				relRotVel = f2.currentRotVel - f1.currentRotVel;
				rotSpring = rotSpring + relRotVel * f1.Dt;
				rotMom = rotSpring * krot;
				f1.currentMoments.Add(rotMom);
				f2.currentMoments.Add(-1.0 * rotMom);

				#endregion
				#endregion

				isZeroForce = false;
			}
			else
			{
				isZeroForce = true;
				normForceMag = 0.0;
				tanForceMag = 0.0;
				currentStress = 0.0;
			}
			#endregion
		}

		protected override void IncrementInternalValues(){
			//Calculate Some Geometry

		}
		
		protected override void InitiateInternalValues(){
			double m12 = (base.f1.Mass * base.f2.Mass) / (base.f1.Mass + base.f2.Mass);
			dn12 = 1.0 * Spring.SetCriticalDamping(m12, kn12, dCoefficient);
			dt12 = 1.0 * Spring.SetCriticalDamping(m12, kt12, dCoefficient); //damping in tangent direction

		}
		
		#endregion
		
		#region public Methods
		
		public override bool BreakSpring(){
			return false;
		}
		
		public override void SaveTimeStep(int iSaved, int iCurrent){
			
			if (!notYetActive && currentlyActive && (iCurrent == tIndex)) {
				//Store if there has been contact and there was contact this round
				
				base.SaveTimeStep(iSaved, iCurrent);
			}
		}
		
        public override void WriteOutput(int nTimeStep, StreamWriter dataWrite)
        {

            if (!notYetActive && lTimeSteps.Contains(nTimeStep))
            {
                int index = lTimeSteps.IndexOf(nTimeStep);
                base.WriteOutput(nTimeStep, dataWrite);
                dataWrite.WriteLine(",");

            }
        }
        #endregion

        #region Protected Methods

        protected void CalculateNormalForce(){
			
			double relNormVelocity = -1.0 * VectorMath.Dot (v12, e12N_YZ); //Positive is moving to each other, negative away
																		   //positive normal force means the spring is stretched (switch the sign from contact)
			double dx = centerpointDistance_YZ - initialCenterlineDist;
			normForceMag = -1.0*(kn12 * dx) + dn12 * relNormVelocity; //The damping is defined as a coefficient of ave. Mass (1/s)
			//These signs are for sure!!!!!!!
		}
		
		protected void CalculatTanForce(){
			//First rotate the previous spring
			currentTanSpring = VectorMath.Subtract(currentTanSpring, VectorMath.ScalarMultiply(VectorMath.Dot(e12N_YZ, currentTanSpring), e12N_YZ));

			//Now find the Tangent Force (using a spring)
			tanForceVect = VectorMath.Add(VectorMath.ScalarMultiply(-1.0 * kt12, currentTanSpring), VectorMath.ScalarMultiply(-1.0 * dt12, vrelT));
			tanForceMag = VectorMath.Norm(tanForceVect);
			normTanF = VectorMath.DeepCopy(tanForceVect);
			VectorMath.NormalizeVector(ref normTanF);

			//Now check whether it's sliding
			isSliding = false;

			if (tanForceMag > stCOF * normForceMag)
			{
				//Dynamic friction
				tanForceMag = dyCOF * normForceMag;
				isSliding = true;
			}
			tanForceVect = VectorMath.ScalarMultiply(tanForceMag, normTanF);
		}
		
		protected void CalculatMoment(){
			double[] fRolling = VectorMath.Add(VectorMath.ScalarMultiply(-1.0 * kt12, currentTanSpring), VectorMath.ScalarMultiply(-1.0 * dt12, vRolling));
			momentMag = VectorMath.Norm(fRolling);
		}

		public static void CalculateSizingKF(ref double k, ref double kT, ref double kR, ref double fmax, ref double xSectArea, double dist, double E, double nu, double maxStress, double r1, double r2, double lx){

			double G = E / (2.0 * (1.0 + nu));

			double phi1 =  Math.Asin (r1 / dist);
			double theta2 =  Math.Acos (r1 / dist);
			double phi2 =  Math.Asin (r2 / dist);
			double theta1 =  Math.Acos (r2 / dist);
			//Underestimated Area
			double du = dist - r1 * Math.Cos (phi1) - r2 * Math.Cos (phi2);
			double hu = r1 * Math.Sin(phi1) + r2 * Math.Sin(phi2);
			double Au = du * hu;
			double Acorr1 = r1*r1*(phi1-0.5*Math.Sin(2.0*phi1)) + r2*r2*(phi2-0.5*Math.Sin(2.0*phi2));
			//Overestimated Area
			double dov = dist - r1 * Math.Cos (theta1) - r2 * Math.Cos (theta2);
			double hov = r1 * Math.Sin(theta1) + r2 * Math.Sin(theta2);
			double Aov = dov * hov;
			double Acorr2 = r1*r1*(theta1-0.5*Math.Sin(2.0*theta1)) + r2*r2*(theta2-0.5*Math.Sin(2.0*theta2));
			//Now look at the idealized area
			double d12 = du;// - ((4.0*r1*Math.Pow(Math.Sin(phi1),3.0)) / (3.0 * (2.0 * phi1 - Math.Sin(2.0 * phi1))) - r1 * Math.Cos(phi1))
			//- ((4.0*r2*Math.Pow(Math.Sin(phi2),3.0)) / (3.0 * (2.0 * phi2 - Math.Sin(2.0 * phi2))) - r1 * Math.Cos(phi2));
			double A12 = 0.5 * (Au + Aov - Acorr1 - Acorr2);
			double h12 = A12 / d12;

			k = E * h12 * lx / d12;
			kT = G * h12 * lx / d12;
			kR = E * lx * Math.Pow(h12,3.0) / (12.0 * d12);
			xSectArea = h12 * lx;
			fmax = xSectArea * maxStress;
			
		}
		
		#endregion
	}
}
*/