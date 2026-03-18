/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 11.01.2015
 * Time: 15:51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore;

namespace FDEMTests
{
	
	public class TestFrictionNoRotation
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
		public void Test_F1_Bottom_v0_F2_Top_v10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.001865).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.001865).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Bottom_v0_F2_Top_vm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,-10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.001865).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.001865).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Top_v10_F2_Bottom_v0()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,10,0}, new double [3]{0,0,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.001865).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.001865).Within(0.000001));
		}
		
		[Test]
		public void Test_F1_Top_vm10_F2_Bottom_v0()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,-10,0}, new double [3]{0,0,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.001865).Within(0.000001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.001865).Within(0.000001));
		}
		
		//BothMoving
		[Test]
		public void Test_F1_Bottom_vm10_F2_Top_v10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{0,10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.00001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.00001));
		}
		
		[Test]
		public void Test_F1_Bottom_v10_F2_Top_v10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{0,10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0d).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0d).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0d).Within(0.00001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0d).Within(0.00001));
		}
		
		[Test]
		public void Test_F1_Top_v10_F2_Bottom_vm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,10,0}, new double [3]{0,-10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00393).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00393).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.00001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.00373).Within(0.00001));
		}
		
		[Test]
		public void Test_F1_Top_vm10_F2_Bottom_vm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,-10,0}, new double [3]{0,-10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927).Within(0.001));
             Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927).Within(0.001));
            //Tangent Force (second step)
            int step = 1;
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0d).Within(0.00001));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0d).Within(0.00001));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0d).Within(0.00001));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0d).Within(0.00001));
		}
		
		//Try it over more ranges
		
		[Test]
		public void Test_F1_Bottom_v0_F2_Top_v10LONG()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,10,0});
			
			int step = 1;
			//Tangent Force (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196).Within(0.00001));
             Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196).Within(0.00001));
            //Right before the tangent stiffness changes
            Assert.That(f1.CurrentForces[2*(n-2)+1][1], Is.EqualTo(0.0392).Within(0.0001));
            Assert.That(f2.CurrentForces[2*(n-2)+1][1], Is.EqualTo(-0.0392).Within(0.0001));
            //Right after the tangent stiffness changes
            Assert.That(f1.CurrentForces[2*(n-1)+1][1], Is.EqualTo(2.3562).Within(0.0001));
            Assert.That(f2.CurrentForces[2*(n-1)+1][1], Is.EqualTo(-2.3562).Within(0.0001));
			
		}
		
	}
}
