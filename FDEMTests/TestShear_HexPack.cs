/*
 * Created by SharpDevelop.
 * User: Carsten
 * Date: 06.03.2015
 * Time: 13:35
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
	
	public class TestShear_HexPack
	{
		Fiber f1;
		Fiber f2;
		Fiber f3;
		
		
		CellBoundary cb;
		CreateAndUpdateContact ccC;
		double staticCOF = 0.35;  	
        double dynamicCOF = 0.35;
        double damping = 0.0;
        double KnOverKt = 4.0/3.0;
		int n = 14;		
		List<Fiber> lFibers;
		double h = Math.Sqrt(Math.Pow(1.9, 2.0) - Math.Pow(0.95, 2.0));
		
		
		private void ShearFourFiberTest(double [] pF1, double [] pF2, double [] pF3, double [] vF1, double [] vF2, double [] vF3){
			lFibers = new List<Fiber>();
			cb = new CellBoundary(new double [3]{4.0,4.0,4.0}, new double[3], new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1}, new double[6]{0.1, 0.1, 0.1, 0.1, 0.1, 0.1});
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
			f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
			f3 = new Fiber(pF3, tempFP, cb, vF3, 0);
			lFibers.Add(f1);
			lFibers.Add(f2);
			lFibers.Add(f3);			
			f1.UpdateTimeStep(0.00001);
			f2.UpdateTimeStep(0.00001);
			f3.UpdateTimeStep(0.00001);
			Grid grid = new Grid(-1.0*f1.Radius, -1.0*f1.Radius, 4.0 + 2.0*f1.Radius, 4.0 + 2.0*f1.Radius, 2.0*f1.Radius);
			ContactParameters conPar = new ContactParameters(staticCOF, dynamicCOF, damping, KnOverKt);
			ccC = new CreateAndUpdateContact(lFibers, grid, cb, conPar);
			for (int i = 0; i < n; i++) {
				cb.SaveTimeStep(i);			//extended array Strain[+1]: must be done, but value keeps in my case 0
				ccC.UpdateGrid(i);
				ccC.UpdateContacts(i, 0.1);	// forces
				ccC.SaveTimeStep(i);		// stress
			
			}
			
			
		}
		[Test]
		public void Test_1_F1_bottomLeft_v0_F2_bottomRight_v0_F3_TopMiddle_vx10_vz0()
		{
			//8/20/2021: doesn't Pass

			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0.95,h}, new double [3]{0,0,0}, new double [3]{0,0,0}, new double [3]{10.0,0,0});
			
		
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0.0039376).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.6995).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0.0039376).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.6995).Within(0.0001));
			
		}
		[Test]
		public void Test_1_F1_bottomLeft_v0_F2_bottomRight_v0_F3_TopMiddle_vx10_vzm10()
		{
			//8/20/2021: doesn't Pass
			ShearFourFiberTest(new double [3]{0,0,0}, new double [3]{0,1.9,0}, new double [3]{0,0.95,h}, new double [3]{0,0,0}, new double [3]{0,0,0}, new double [3]{10.0,0,-10.0});
			
		
			 Assert.That(ccC.HomogenizedStress[n][0,0], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,1], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][0,2], Is.EqualTo(0.0039376).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,0], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][1,1], Is.EqualTo(-0.6975).Within(0.0001));
			 Assert.That(ccC.HomogenizedStress[n][1,2], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,0], Is.EqualTo(0.0039376).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,1], Is.EqualTo(0).Within(0.0000001));
			 Assert.That(ccC.HomogenizedStress[n][2,2], Is.EqualTo(-0.7015).Within(0.0001));
			
		}
	}
}
