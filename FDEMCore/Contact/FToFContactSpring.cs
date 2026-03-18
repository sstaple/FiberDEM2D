/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 9/14/2019
 * Time: 7:51 AM
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
	/// Description of FToFContactSpring.
	/// </summary>
	public class FToFContactSpring:FToFSpring
	{
		#region Private Members
		protected double [] currentTanSpring; //distance of the tangential spring (or the amount of sliding that has occured)
		protected double stCOF; //Static coefficient of friction
		protected double dyCOF; //dynamic coefficient of friction
		protected bool isSliding = false;
		protected double [] vRolling;
		protected double knOverkt; //ratio of normal to tangential
		protected double overlapDistance;
		protected List <bool> lIsSliding; //a list of all of the normal force magnitudes from fiber 1 to fiber 2
		//Some Geometry
		protected double [] e12T; //Tangential to the contact plane
		protected double [] vrelT; //relative tangential velocities of the 2 bodies (3-d)
		protected double vrelN; //relative normal velocities of the 2 bodies (3-d)
		protected double [] normTanF;
		#endregion
		
		#region Public Members
		#endregion
		
		#region Constructors
		
		public FToFContactSpring(ContactParameters inContPar, Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2)
			:base(fiber1, fiber2, nfiber1, nfiber2)
		{
			knOverkt = inContPar.KnOverKt;
			stCOF = inContPar.SCOF;
			dyCOF = inContPar.DCOF;
			dCoefficient = inContPar.ContactDamping;
			lIsSliding = new List<bool>();
			currentTanSpring = new double[3];
			
			UpdateStiffness();
			sType = "Contact";
		}
		#endregion
		
		#region Private Methods to Incorporate and Delete
		//Haven't really incorporated this yet.....
		/*protected void AssignTanLoadToFiber(double fMag, double [] force, Fiber f){    // Carsten: "f.radius" to "leverArm"
			f.currentForces.Add(force);
			double leverArm = f.Radius - centerlineDistance / 2.0;
			f.currentMoments.Add(fMag * leverArm);
		}*/
		
		
		#endregion
		
		#region Override Methods
		
		public override bool IsSpringActive(int currentTimeStep){
			
			double [] xl12 = new double[3];
			double [] vl12 = new double[3];
			double [] pt1 = new double[3];
			double [] pt2 = new double[3];
            int nList1 = 0;
            int nList2 = 0;
			
			centerpointDistance_YZ = GetMinYZDistanceBetweenFibersIncludingProjections(f1, f2, ref xl12, ref vl12, ref pt1, ref pt2, ref nList1, ref nList2);
			overlapDistance = f1.Radius + f2.Radius - centerpointDistance_YZ;
			bool contact = overlapDistance > 0;
			
			if (contact) {
				double [] x12YZ = new double[3]{0, xl12[1], xl12[2]};
				double [] e12Ntemp = Spring.NormalizeVector(x12YZ);
                npf1 = nList1 - 1;
                npf2 = nList2 - 1;
                IsFoundToBeActive(centerpointDistance_YZ, pt1, pt2, xl12, vl12, e12Ntemp, currentTimeStep);
				
			}
			return contact;
		}
		
		protected override void UpdateInternalValues(){
			
			//Calculate Some Geometry
			
			FindTanVelocity();
			e12T = NormalizeVector(vrelT);
			vrelN = -1.0 * VectorMath.Dot(v12, e12N_YZ);
		}
		
		protected override void CalculateForcesAndMoments(){
			CalculateNormalForce();
			CalculatTanForce();
			CalculateMoment();
		}
		
		protected override void IncrementInternalValues(){
			
			if (!isSliding) {
				double [] currExt = VectorMath.ScalarMultiply (f1.Dt, vrelT);
				currentTanSpring = VectorMath.Add (currentTanSpring, currExt);
			} else {
				double [] temp1 = VectorMath.ScalarMultiply(tanForceMag, normTanF);
				double [] temp2 = VectorMath.ScalarMultiply(1.0*dt12, vrelT);
				currentTanSpring = VectorMath.ScalarMultiply (-1.0 / kt12, VectorMath.Add(temp1, temp2));
			}
		}
		
		protected override void InitiateInternalValues(){
			currentTanSpring = new double[3];
		}
		
		protected override void ApplyForcesAndMoments(){
			
			#region Add the moment resulting from the tangential/frictional load
			
			//First, find just the in-plane component of the tangent force (in yz plane)
			double [] fTanYZ = new double[3]{0, tanForceVect[1], tanForceVect[2]};
			double fMom = VectorMath.VectorProduct(fTanYZ, e12N_YZ)[0];
			
			//Add moment resulting from tangential friction force
			double r1p = f1.Radius - overlapDistance / 2.0;
			double r2p = f2.Radius - overlapDistance / 2.0;
			
			f1.CurrentMoments.Add(r1p*fMom);
			f2.CurrentMoments.Add(r2p*fMom);
			#endregion
			
			base.ApplyForcesAndMoments();
		}
		
		protected void UpdateStiffness(){
			double Estar = 1d / ( (1d - f2.Nu12 * f2.Nu12) / f2.Modulus2 + (1d - f1.Nu12 * f1.Nu12) / f1.Modulus2);
			double k = Math.PI* Estar * ((f1.OLength + f2.OLength) / 2d) / 4d; //Herzian contact of 2 cylinders: from wikipedia
			double MStar= f1.Mass * f2.Mass / (f1.Mass + f2.Mass);

			kn12 = k;
			kt12 = kn12 / knOverkt;//TODO Get a physically significant value for this
			dn12 = Spring.SetCriticalDamping(MStar, k, dCoefficient);
			dt12 = dn12 / knOverkt;
		}
		
		#endregion
		
		#region public Methods
		
		public override void SaveTimeStep(int iSaved, int iCurrent){
			
			if (!notYetActive && currentlyActive && (iCurrent == tIndex)) {
				//Store if there has been contact and there was contact this round
				
				base.SaveTimeStep(iSaved, iCurrent);
				lIsSliding.Add(isSliding);
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
			
			normForceMag = kn12 * overlapDistance + dn12 * vrelN; //The damping is defined as a coefficient of ave. Mass (1/s)
			if (normForceMag < 0d) {  //This just makes sure that theres no repelling force if it leaves very fast
				normForceMag = 0d;
			}
			normForceVect = VectorMath.ScalarMultiply(normForceMag, e12N_YZ);
		}
		
		protected void CalculatTanForce(){
			
			//First rotate the previous spring
			currentTanSpring = VectorMath.Subtract(currentTanSpring, VectorMath.ScalarMultiply(VectorMath.Dot(e12N_YZ, currentTanSpring), e12N_YZ));

			//Now find the Tangent Force (using a spring)
			tanForceVect = VectorMath.Add (VectorMath.ScalarMultiply(-1.0 * kt12, currentTanSpring), VectorMath.ScalarMultiply(-1.0 * dt12, vrelT));
			tanForceMag = VectorMath.Norm(tanForceVect);
			normTanF =  VectorMath.DeepCopy(tanForceVect);
			VectorMath.NormalizeVector(ref normTanF);

			//Now check whether it's sliding
			isSliding = false;

			if (tanForceMag > stCOF * normForceMag) {
				//Dynamic friction
				tanForceMag = dyCOF * normForceMag;
				isSliding = true;
			}
			tanForceVect = VectorMath.ScalarMultiply(tanForceMag, normTanF);

		}
		
		protected void CalculateMoment(){
			
			//First, find the force that contributes to the moment
			double[] fRolling = VectorMath.Add (VectorMath.ScalarMultiply (-1.0 * kt12, currentTanSpring), VectorMath.ScalarMultiply (-1.0 * dt12, vRolling));
			momentMag = VectorMath.Norm (fRolling);
			
			//Check whether it is static or dynamic
			if (momentMag > stCOF * normForceMag) {
				//Dynamic friction
				momentMag = dyCOF * normForceMag;
			}
			
			//This assumes that the fibers are equal radius!  Change if necessary
			double r1p = f1.Radius - overlapDistance / 2.0;
			momentMag = r1p * momentMag;
		}
		
		protected void FindTanVelocity(){
			double r1p =  f1.Radius - overlapDistance / 2.0;
			double r2p =  f2.Radius - overlapDistance / 2.0;
			
			double [] e1T2D = VectorMath.VectorProduct(f1.FOrientation, e12N_YZ);
			double [] e2T2D = VectorMath.VectorProduct(f2.FOrientation, e12N_YZ);
			
			double [] vRotate1 = VectorMath.ScalarMultiply(r1p * f1.CurrentRotVel, e1T2D);
			double [] vRotate2 = VectorMath.ScalarMultiply(r2p * f2.CurrentRotVel, e2T2D);
			double [] vrel = VectorMath.Subtract(v12, VectorMath.Add(vRotate1, vRotate2));
			vrelT = VectorMath.Subtract(vrel, VectorMath.ScalarMultiply(VectorMath.Dot(e12N_YZ, vrel),e12N_YZ));
			
			//For the rolling, use the reduced radius
			double rr = (r1p * r2p) / (r1p + r2p);
			double [] vR1 = VectorMath.ScalarMultiply(rr * f1.CurrentRotVel, e1T2D);
			double [] vR2 = VectorMath.ScalarMultiply(rr * f2.CurrentRotVel, e2T2D);
			vRolling = VectorMath.Subtract(vR2, vR1);
		}
		#endregion
	}
}
