/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 02.03.2015
 * Time: 20:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore;

namespace FDEMTests
{
	
	public class TestFriction3D_Vx_Vy_Rotation
	{
		Fiber f1;
		Fiber f2;
		CellBoundary cb;
		
		
		private void SetupTwoFiberTest(double [] pF1, double [] pF2, double [] vF1, double [] vF2, double wF1, double wF2){
			cb = new CellBoundary(new double [3]{1.0,1.0,1.0}, new double[3], new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1}, new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1});
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, vF1, wF1);
			f2 = new Fiber(pF2, tempFP, cb, vF2, wF2);
			f1.UpdateTimeStep(0.00001);
			f2.UpdateTimeStep(0.00001);
			ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
			FToFSpring ffSpring = new FToFContactSpring(cp, f1, f2, 0, 1);
			ffSpring.Update(1, 0.1);
			ffSpring.Update(2, 0.1);
			ffSpring.Update(2, 0.1);
			
		}
		
		// Fiber1: bottom      Fiber2: top
		
		[Test]
		public void Test_F1_Bottom_Vy10_Omega_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{10,0,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_Vy10_Omega_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{-10,0,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_Vym10_Omega_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{10,0,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_Vym10_Omega_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{-10,0,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
		}
		
		
		[Test]
		public void Test_F1_Bottom_Vy10_OmegaM_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{10,0,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_Vy10_OmegaM_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{-10,0,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_Vym10_OmegaM_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{10,0,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_Vym10_OmegaM_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{-10,0,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
	//*******************************************************************************************************	
	//*******************************************************************************************************
	// Fiber1:Top    Fiber2:Bottom
	
		[Test]
		public void Test_F2_Bottom_Vy10_Omega_F1_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,10,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
		
		[Test]
		public void Test_F2_Bottom_Vy10_Omega_F1_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,10,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
		
		[Test]
		public void Test_F2_Bottom_Vym10_Omega_F1_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,-10,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
		}
		
		[Test]
		public void Test_F2_Bottom_Vym10_Omega_F1_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,-10,0}, 10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.000001));
		}
		
		
		[Test]
		public void Test_F2_Bottom_Vy10_OmegaM_F1_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,10,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
		}
		
		[Test]
		public void Test_F2_Bottom_Vy10_OmegaM_F1_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,10,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.00373).Within(0.000001));
		}
		
		[Test]
		public void Test_F2_Bottom_Vym10_OmegaM_F1_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,-10,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
		
		[Test]
		public void Test_F2_Bottom_Vym10_OmegaM_F1_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,-10,0}, -10.526, 0);
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.0001));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.0001));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196).Within(0.00001));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0).Within(0.000001));
		}
	}
}