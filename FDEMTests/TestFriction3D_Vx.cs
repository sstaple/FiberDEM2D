/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 16-Jan-15
 * Time: 9:42 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore;

namespace FDEMTests
{
	
	public class TestFriction3D_Vx
	{
		Fiber f1;
		Fiber f2;
		CellBoundary cb;
		int n = 22;
		
		private void SetupTwoFiberTest(double [] pF1, double [] pF2, double [] vF1, double [] vF2){
			cb = new CellBoundary(new double [3]{1.0,1.0,1.0}, new double[3], new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1}, new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1});
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
			f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
			f1.UpdateTimeStep(0.00001);
			f2.UpdateTimeStep(0.00001);
			ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
			FToFSpring ffSpring = new FToFContactSpring(cp, f1, f2, 0, 1);
			for (int i = 0; i < n; i++) {
				ffSpring.Update(i+1, 0.1);
			}
			
		}
		[Test]
		public void Test_F1_Bottom_vx0_F2_Top_vx10()
		{
			SetupTwoFiberTest(new double[3] { 0, 0, 0 }, new double[3] { 0, 0, 1.9 }, new double[3] { 0, 0, 0 }, new double[3] { 10, 0, 0 });

			//Normal Force
			Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
			Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
			//Tangent Force (second step)
			int step = 1;
			Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2 * step + 1][0]));
			Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2 * step + 1][0]));
			//Moment (second step)
			Assert.That(0, Is.EqualTo(f1.CurrentMoments[2 * step]));
			Assert.That(0, Is.EqualTo(f2.CurrentMoments[2 * step]));
		}

            [Test]
		public void Test_F1_Bottom_vx0_F2_Top_vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0});
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Moment (second step)
            Assert.That(0, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Top_vx10_F2_Bottom_v0()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,0,0});
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(-0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Moment (second step)
            Assert.That(0, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Top_vxm10_F2_Bottom_v0()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,0,0});
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Moment (second step)
            Assert.That(0, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		//BothMoving
		[Test]
		public void Test_F1_Bottom_vxm10_F2_Top_vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{-10,0,0}, new double [3]{10,0,0});
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(0.003927, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(-0.003927, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Moment (second step)
            Assert.That(0, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Bottom_vxm10_F2_Top_vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{-10,0,0}, new double [3]{-10,0,0});
			
			//Normal Force
			 Assert.That(-3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(0, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Moment (second step)
            Assert.That(0d, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0d, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Top_vx10_F2_Bottom_vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{-10,0,0});
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(-0.003927, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0.003927, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            Assert.That(0, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		[Test]
		public void Test_F1_Top_vx10_F2_Bottom_vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{10,0,0});
			
			//Normal Force
			 Assert.That(3.927, Is.EqualTo(f1.CurrentForces[0][2]));
            Assert.That(-3.927, Is.EqualTo(f2.CurrentForces[0][2]));
            //Tangent Force (second step)
            int step = 1;
			 Assert.That(0d, Is.EqualTo(f1.CurrentForces[2*step + 1][0]));
            Assert.That(0d, Is.EqualTo(f2.CurrentForces[2*step + 1][0]));
            //Moment (second step)
            Assert.That(0d, Is.EqualTo(f1.CurrentMoments[2*step]));
            Assert.That(0d, Is.EqualTo(f2.CurrentMoments[2*step]));
        }
		
		//Try it over more ranges
		
		[Test]
		public void Test_F1_Bottom_v0_F2_Top_v10LONG()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,10,0});
			
			int step = 1;
			//Tangent Force (second step)
			 Assert.That(0.00196, Is.EqualTo(f1.CurrentForces[2*step + 1][1]));
            Assert.That(-0.00196, Is.EqualTo(f2.CurrentForces[2*step + 1][1]));

            //Right before the tangent stiffness changes
            Assert.That(0.0392, Is.EqualTo(f1.CurrentForces[2*(n-2)+1][1]));
            Assert.That(-0.0392, Is.EqualTo(f2.CurrentForces[2*(n-2)+1][1]));
            //Right after the tangent stiffness changes
            Assert.That(2.3562, Is.EqualTo(f1.CurrentForces[2*(n-1)+1][1]));
            Assert.That(-2.3562, Is.EqualTo(f2.CurrentForces[2*(n-1)+1][1]));
        }
		
	}
}
