/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 20.01.2015
 * Time: 10:30
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore;

namespace FDEMTests
{
	
	public class TestFriction3D_Vx_Rotation
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
		[Test]
		public void Test_F1_Bottom_Omega_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, 10.526, 0);
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(-0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(-0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Bottom_Omega_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, 10.526, 0);
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(-0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(-0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Bottom_OmegaM_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, -10.526, 0);
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Bottom_OmegaM_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, -10.526, 0);
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		//BothMoving
		[Test]
		public void Test_F1_Top_Vx10_F2_Bottom_Omega()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,0,0}, 0, 10.526);
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(-0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(-0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Top_Vxm10_F2_Bottom_Omega()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,0,0}, 0, 10.526);
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(-0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(-0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Top_Vx10_F2_Bottom_OmegaM()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,0,0}, 0, -10.526);
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Tangent Force y-direction (second step)
            Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));
            //Moment (second step)
            Assert.That(0.001865, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0.001865, Is.EqualTo(f2.CurrentMoments[2*step]));
        }

		[Test]
		public void Test_F1_Top_Vxm10_F2_Bottom_OmegaM()
		{
			SetupTwoFiberTest(new double[3] { 0, 0, 1.9 }, new double[3] { 0, 0, 0 }, new double[3] { -10, 0, 0 }, new double[3] { 0, 0, 0 }, 0, -10.526);

			//Normal Force
			Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
			Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
			int step = 1;
			//Tangent Force x-direction (second step)
			Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2 * step + 1][0]));
			Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2 * step + 1][0]));
			//Tangent Force y-direction (second step)
			Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2 * step + 1][1]));
			Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2 * step + 1][1]));
			//Moment (second step)
			Assert.That(0.001865, Is.EqualTo(f1.CurrentMoments[2 * step]));
			Assert.That(0.001865, Is.EqualTo(f2.CurrentMoments[2 * step]));

		}
	}
}