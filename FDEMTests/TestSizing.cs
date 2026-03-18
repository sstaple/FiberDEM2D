/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 3/18/2015
 * Time: 7:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
/*Ugly to comment this out, but put it back if sizing ever makes it's way back....
 * 
using System;
using NUnit.Framework;
using FiberDEM.Contact;
using FiberDEM;

namespace FDEMTests
{
	
	public class TestSizing
	{
		Fiber f1;
		Fiber f2;
		CellBoundary cb;
		int n = 22;
		
		private void SetupTwoFiberTest(double sizingDist, double [] pF1, double [] pF2, double [] vF1, double [] vF2){
			cb = new CellBoundary(new double [3]{1.0,1.0,1.0}, new double[6]{0.0,0.0,0.0,0.0,0.0,0.0}, new double[6]{0.0,0.0,0.0,0.0,0.0,0.0});
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 0.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
			f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
			ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
			FToFRelation ffSpring = new FToFRelation(cp, f1, f2, 0, 1);
			ffSpring.AddNonContactSpring(new SizingParameters(10, 0.3, sizingDist, 1.0, 1.0, 0.0, 2), 0.1);
			f1.UpdateTimeStep(0.01);
			f2.UpdateTimeStep(0.01);
			for (int i = 1; i < n; i++) {
				f1.UpdatePosition();
				f2.UpdatePosition();
				ffSpring.Update(i, 0.1);
			}
		}

		[Test]
		public void Test_F1_Bottom_v0_F2_Top_v1()
		{
			SetupTwoFiberTest(0.2, new double [3]{0,0,0}, new double [3]{0,0,2.1}, new double [3]{0,0,0}, new double [3]{0,0,1.0});
			
			//Normal Force
			//when l0 = 0.1, then k=37.6
			// Assert.That(0.0, f1.currentForces[0][2], 0.0000001);
			// Assert.That(0, f2.currentForces[0][2], 0.0000001);
			 Assert.That(0.3762807, f1.CurrentForces[0][2], 0.00001);
			 Assert.That(-0.3762807, f2.CurrentForces[0][2],  0.00001);
			 Assert.That(0.752561446, f1.CurrentForces[2][2], 0.0000001);
			 Assert.That(-0.752561446, f2.CurrentForces[2][2],  0.0000001);
			
		}
		[Test]
		public void Test_F1_Bottom_v0_F2_Top_vn1()
		{
			SetupTwoFiberTest(0.2, new double [3]{0,0,0}, new double [3]{0,0,2.1}, new double [3]{0,0,0}, new double [3]{0,0,-1.0});
			
			//Normal Force
			//sizimg: 0.3762807, 0.75 2nd iteration
			//Contact: 0.392699081698724 2nd iteration
			 Assert.That(-0.3762807, f1.CurrentForces[0][2], 0.0000001);
			 Assert.That(0.3762807, f2.CurrentForces[0][2],  0.0000001);
			 Assert.That(-0.752561446, f1.CurrentForces[2][2], 0.000001); //Sizing Force
			 Assert.That(0.752561446, f2.CurrentForces[2][2],  0.000001); //Sizing Force
			//Moving toward each other at v=-1.0
			 Assert.That(-0.392699081698724, f1.CurrentForces[6][2], 0.000001); //Contact Force
			 Assert.That(0.392699081698724, f2.CurrentForces[6][2],  0.000001); //Contact Force
			
		}
		[Test]
		public void Test_F1_Bottom_v0_F2_Top_vn1_NoSizing()
		{
			SetupTwoFiberTest(0.09, new double [3]{0,0,0}, new double [3]{0,0,2.1}, new double [3]{0,0,0}, new double [3]{0,0,-1.0});
			
			//This should make no sizing
			 Assert.That(-0.39269908, f1.CurrentForces[0][2], 0.000001);
			 Assert.That(0.39269908, f2.CurrentForces[0][2],  0.000001);
			 Assert.That(-0.785398163, f1.CurrentForces[2][2], 0.000001);
			 Assert.That(0.785398163, f2.CurrentForces[2][2],  0.000001);
			
		}
		[Test]
		public void TestShear_F1_Bottom_v0_F2_Top_v1()
		{
			SetupTwoFiberTest(0.2, new double [3]{0,0,0}, new double [3]{0,0,2.1}, new double [3]{0,0,0}, new double [3]{0,1.0,0.0});
			
			//Normal Force
			//when l0 = 0.1, then kt= 14.47233549
			 Assert.That(0.1447233549, f1.CurrentForces[3][1], 0.00001);
			 Assert.That(-0.1447233549, f2.CurrentForces[3][1],  0.00001);
			 Assert.That(0.289404, f1.CurrentForces[5][1], 0.0001);
			 Assert.That(-0.289404, f2.CurrentForces[5][1],  0.0001);
			 Assert.That(-0.1447233549, f1.CurrentMoments[2], 0.0001);
			 Assert.That(-0.1447233549, f2.CurrentMoments[2],  0.0001);
			 Assert.That(-0.289404, f1.CurrentMoments[4], 0.0001);
			 Assert.That(-0.289404, f2.CurrentMoments[4],  0.0001);
			
		}
		[Test]
		public void TestShear_Spring()
		{
			double [] pF1 = new double [3]{0,0,0};
			double [] pF2 = new double [3]{0,0,2.1};
			cb = new CellBoundary(new double [3]{1.0,1.0,1.0}, new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1}, new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1});
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 0.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, new double [3]{0,0,0}, 0);
			f2 = new Fiber(pF2, tempFP, cb, new double [3]{0,1.0,0.0}, 0);
			ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
			FToFRelation ffSpring = new FToFRelation(cp, f1, f2, 0, 1);
			ffSpring.AddNonContactSpring(new SizingParameters(10, 0.3, 0.2, 1.0, 1.0, 0.0, 2), 0.1);
			f1.UpdateTimeStep(1.0);
			f2.UpdateTimeStep(1.0);
			ffSpring.Update(0, 0.1);
			
			f2 = new Fiber(new double [3]{0,0,2.2}, tempFP, cb, new double [3]{0,1.0,0.0}, 0);
			f2.UpdateTimeStep(1.0);
			ffSpring.Update(1, 0.1);
			
			f2 = new Fiber(new double [3]{0,0,2.3}, tempFP, cb, new double [3]{0,-1.0,0.0}, 0);
			f2.UpdateTimeStep(1.0);
			ffSpring.Update(2, 0.1);
			
			f2 = new Fiber(new double [3]{0,0,2.2}, tempFP, cb, new double [3]{0,-1.0,0.0}, 0);
			f2.UpdateTimeStep(1.0);
			ffSpring.Update(3, 0.1);
			
			f2 = new Fiber(new double [3]{0,0,2.1}, tempFP, cb, new double [3]{0,-1.0,0.0}, 0);
			f2.UpdateTimeStep(1.0);
			ffSpring.Update(4, 0.1);
			
			f2 = new Fiber(new double [3]{0,0,2.0}, tempFP, cb, new double [3]{0,-1.0,0.0}, 0);
			f2.UpdateTimeStep(1.0);
			ffSpring.Update(5, 0.1);
			
		}
	}
}
*/