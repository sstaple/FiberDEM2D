/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 17-Jan-15
 * Time: 11:56 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore;

namespace FDEMTests
{
	
	public class TestFriction3D_Vx_Vy
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
		public void Test_F1_Bottom_Vy10_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{10,0,0});
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.001865));
        }
		
		[Test]
		public void Test_F1_Bottom_Vy10_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,10,0}, new double [3]{-10,0,0});
			
			//Normal Force
            Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.001865));
        }
		
		[Test]
		public void Test_F1_Bottom_Vym10_F2_Top_Vx10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{10,0,0});
			
			//Normal Force
             Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927));
            int step = 1;
            //Tangent Force x-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.001865));
        }
		
		[Test]
		public void Test_F1_Bottom_Vym10_F2_Top_Vxm10()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,-10,0}, new double [3]{-10,0,0});
			
			//Normal Force
             Assert.That(f1.CurrentForces[0][2], Is.EqualTo(-3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(3.927));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.001865));
        }
		
		//BothMoving
		[Test]
		public void Test_F1_Top_Vx10_F2_Bottom_Vy10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.001865));
        }
		
		[Test]
		public void Test_F1_Top_Vxm10_F2_Bottom_Vy10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(0.001865));
        }
		
		[Test]
		public void Test_F1_Top_Vx10_F2_Bottom_Vym10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,-10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.001865));
        }
		
		[Test]
		public void Test_F1_Top_Vxm10_F2_Bottom_Vym10()
		{
			SetupTwoFiberTest(new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,-10,0});
			
			//Normal Force
			 Assert.That(f1.CurrentForces[0][2], Is.EqualTo(3.927));
            Assert.That(f2.CurrentForces[0][2], Is.EqualTo(-3.927));
            int step = 1;
			//Tangent Force x-direction (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][0], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][0], Is.EqualTo(-0.00196));
            //Tangent Force y-direction (second step)
            Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            //Moment (second step)
            Assert.That(f1.CurrentMoments[2*step], Is.EqualTo(-0.001865));
            Assert.That(f2.CurrentMoments[2*step], Is.EqualTo(-0.001865));
        }
		
		//Try it over more ranges
		
		[Test]
		public void Test_F1_Bottom_v0_F2_Top_v10LONG()
		{
			SetupTwoFiberTest(new double [3]{0,0,0}, new double [3]{0,0,1.9}, new double [3]{0,0,0}, new double [3]{0,10,0});
			
			int step = 1;
			//Tangent Force (second step)
			 Assert.That(f1.CurrentForces[2*step + 1][1], Is.EqualTo(0.00196));
            Assert.That(f2.CurrentForces[2*step + 1][1], Is.EqualTo(-0.00196));

            //Right before the tangent stiffness changes
            Assert.That(f1.CurrentForces[2*(n-2)+1][1], Is.EqualTo(0.0392));
            Assert.That(f2.CurrentForces[2*(n-2)+1][1], Is.EqualTo(-0.0392));
            //Right after the tangent stiffness changes
            Assert.That(f1.CurrentForces[2*(n-1)+1][1], Is.EqualTo(2.3562));
            Assert.That(f2.CurrentForces[2 * (n - 1) + 1][1], Is.EqualTo(-2.3562));
			
		}
		
	}
}