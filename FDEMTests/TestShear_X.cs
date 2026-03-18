/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 09.02.2015
 * Time: 12:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using System.Collections.Generic;
using FDEMCore.Contact;
using FDEMCore;


namespace FDEMTests
{
	
	public class TestShear_X
	{
		Fiber f1;
		Fiber f2;
		Fiber f3;
		Fiber f4;
		
		CellBoundary cb;
		CreateAndUpdateContact ccC;
		double staticCOF = 0.01;  	
        double dynamicCOF = 0.35;
        double damping = 0.0;
        double KnOverKt = 4.0/3.0;
		int n = 14;		
		List<Fiber> lFibers;
		
		
		
		private void ShearFourFiberTest(double [] pF1, double [] pF2, double [] pF3, double [] pF4, double [] vF1, double [] vF2, double [] vF3, double [] vF4){
			lFibers = new List<Fiber>();
			cb = new CellBoundary(new double [3]{4.0,4.0,4.0}, new double[3], new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1}, new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1});
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
			f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
			f3 = new Fiber(pF3, tempFP, cb, vF3, 0);
			f4 = new Fiber(pF4, tempFP, cb, vF4, 0);
			lFibers.Add(f1);
			lFibers.Add(f2);
			lFibers.Add(f3);			
			lFibers.Add(f4);
			f1.UpdateTimeStep(0.00001);
			f2.UpdateTimeStep(0.00001);
			f3.UpdateTimeStep(0.00001);
			f4.UpdateTimeStep(0.00001);
			Grid grid = new Grid(-1.0*f1.Radius, -1.0*f1.Radius, 4.0 + 2.0*f1.Radius, 4.0 + 2.0*f1.Radius, 2.0*f1.Radius);
			ContactParameters conPar = new ContactParameters(staticCOF, dynamicCOF, damping, KnOverKt);
			ccC = new CreateAndUpdateContact(lFibers, grid, cb, conPar);
			for (int i = 0; i < n; i++) {
				cb.SaveTimeStep(i);			//extended array Strain[+1]: must be done, but value keeps in my case 0
				ccC.UpdateGrid(i);
				ccC.UpdateContacts(i, 0.1);     // forces
				ccC.SaveTimeStep(i);
				//ccC.UpdateContacts(i+1, 0.1);		// forces
				//ccC.SaveTimeStep(i+1);		// stress

			}
			
			
			
		}
		[Test]
		public void Test_1_F1_bottomLeft_v0_F2_bottomRight_v0_F3_TopLeft_v10_F4_TopRight_v10()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{0,0,0}, new double [3]{0,0,0}, new double [3]{10.0,0,0}, new double [3]{10.0,0,0});
            
            Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
            Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0).Within(0));
            Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0.0045467).Within(0.0000001));
            Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0).Within(0));
            Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
            Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
            Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0.0045467).Within(0.0000001));
            Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
            Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
        }
		[Test]
		public void Test_2_F1_bottomLeft_v0_F2_bottomRight_v0_F3_TopLeft_vm10_F4_TopRight_vm10()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{0,0,0}, new double [3]{0,0,0}, new double [3]{-10.0,0,0}, new double [3]{-10.0,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
		[Test]
		public void Test_3_F1_bottomLeft_v10_F2_bottomRight_v10_F3_TopLeft_v0_F4_TopRight_v0()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{10,0,0}, new double [3]{10,0,0}, new double [3]{0,0,0}, new double [3]{0,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
		[Test]
		public void Test_4_F1_bottomLeft_vm10_F2_bottomRight_vm10_F3_TopLeft_v0_F4_TopRight_v0()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{-10,0,0}, new double [3]{-10,0,0}, new double [3]{0,0,0}, new double [3]{0,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
		[Test]
		public void Test_5_F1_bottomLeft_v0_F2_bottomRight_v10_F3_TopLeft_v0_F4_TopRight_v10()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,0,0}, new double [3]{10,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
		[Test]
		public void Test_6_F1_bottomLeft_v0_F2_bottomRight_vm10_F3_TopLeft_v0_F4_TopRight_vm10()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,0,0}, new double [3]{-10,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
		[Test]
		public void Test_7_F1_bottomLeft_v10_F2_bottomRight_v0_F3_TopLeft_v10_F4_TopRight_v0()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{10,0,0}, new double [3]{0,0,0}, new double [3]{10,0,0}, new double [3]{0,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(-0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
		[Test]
		public void Test_8_F1_bottomLeft_vm10_F2_bottomRight_v0_F3_TopLeft_vm10_F4_TopRight_v0()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0,1.9}, new double [3]{0,1.9,1.9}, new double [3]{-10,0,0}, new double [3]{0,0,0}, new double [3]{-10,0,0}, new double [3]{0,0,0});

			
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0.0045467).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.9327).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.9327).Within(0.0001));
			
		}
	}
}