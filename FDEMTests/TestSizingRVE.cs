/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 6/17/2015
 * Time: 10:17 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
/* implement this later when sizing becomes an issue again....
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using FiberDEM;

namespace FDEMTests
{
	
	public class TestSizingRVE
	{
		
		Fiber f1;
		Fiber f2;
		CellBoundary cb;
		LoadStepAnalysis myAnalysis;
		int nTS = 10;
		int nSS = 1;
		double dT = 0.01;
		
		private void SetupTwoFiberTest(double [] pF1, double [] pF2,
		                               double [] cbLengths, double [] str){
			cb = new CellBoundary(cbLengths, myMath.VectorMath.ScalarMultiply(1.0/nTS, str), 
			                      myMath.VectorMath.ScalarMultiply(1.0/nTS/dT, str));
			//Be carefull: if grid is too small, then we lose contact pairs with long sizing
			Grid grd = new Grid(-1.0, -1.0, 2.0*cbLengths[2], 2.0*cbLengths[1], 4.0);
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 0.0);
			f1 = new Fiber(pF1, tempFP, cb, new double[3], 0);
			f2 = new Fiber(pF2, tempFP, cb, new double[3], 0);
			List <Fiber> lFibers = new List<Fiber> {f1, f2 };
			ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
			SizingParameters sp = new SizingParameters(10, 0.3, 0.5, 100.0, 1.0, 0.0, 2);
			myAnalysis = new LoadStepAnalysis(str, nTS,
			                                  dT, nSS, 1, cp);
			myAnalysis.AddNonContactSprings(sp);
			
			myAnalysis.Analyze(lFibers, cb, grd);
			
			//myAnalysis.GenerateOutput(new OutputParameters(Directory.GetCurrentDirectory(), 
			//                                               true, false, false, false, false), 1);
			
		}
		[Test]
		public void TwoFibersRVE_Tension()
		{
			SetupTwoFiberTest(new double [3]{0,0.5,1.0}, new double [3]{0,8.5,1.0}, 
			                  new double [3]{1.0,10.1,2.0}, new double [6]{0.0,0.1,0.0,0.0,0.0,0.0});
			
			//ksizing=37.62807
			//l0=0.1
			//
			//This should make no sizing 
			//Check Stresss (if it doesn't pass, could be a volume problem)
			 Assert.That(0.327590435, myAnalysis.HomogenizedStress[3][1,1], 0.000001);
			 Assert.That(0.669666888, myAnalysis.HomogenizedStress[4][1,1], 0.000001);
			 Assert.That(1.023795121, myAnalysis.HomogenizedStress[5][1,1], 0.000001);
			//Look in the excel spreadsheet to do more with this...
			//ValidationOfSizingAssumption.xclx
		}
		
		[Test]
		public void TwoFibersRVE_Compression()
		{
			SetupTwoFiberTest(new double [3]{0,0.5,1.0}, new double [3]{0,8.5,1.0}, 
			                  new double [3]{1.0,10.1,2.0}, new double [6]{0.0,-0.1,0.0,0.0,0.0,0.0});
			
			//ksizing=37.62807
			//l0=0.1
			//
			//This should make no sizing 
			//Check Stresss (if it doesn't pass, could be a volume problem)
			 Assert.That(-0.005926621, myAnalysis.HomogenizedStress[1][1,1], 0.000001);
			 Assert.That(-0.801253029, myAnalysis.HomogenizedStress[2][1,1], 0.000001);
			 Assert.That(-1.513957204, myAnalysis.HomogenizedStress[3][1,1], 0.000001);
			//Look in the excel spreadsheet to do more with this...
			//ValidationOfSizingAssumption.xclx
		}
		
		[Test]
		public void TwoFibersRVE_Shear()
		{
			nTS=1000;
			nSS=10;
			double Estar = 1d / ( 2.0 *(1d - 0.0 * 0.0 ) / 100.0 );
			double maxK = Math.PI* Estar * 1.0 / 4d;
			dT = Analysis.MaxDT(1.0, maxK, 0.0);
			
			double [] pF1 = new double [3]{0,3.0,0.5};
			double [] pF2 = new double [3]{0,3.0, 8.5};
			double [] cbLengths = new double [3]{1.0,6.0,10.0};
			double [] str = new double [6]{0.0,0.0,0.0,0.0,0.0,-0.1};
		                       
			cb = new CellBoundary(cbLengths, myMath.VectorMath.ScalarMultiply(1.0/nTS, str), 
			                      myMath.VectorMath.ScalarMultiply(1.0/nTS/dT, str));
			//Be carefull: if grid is too small, then we lose contact pairs with long sizing
			Grid grd = new Grid(-1.0, -1.0, 2.0*cbLengths[2], 2.0*cbLengths[1], 4.0);
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 1.0);
			f1 = new Fiber(pF1, tempFP, cb, new double[3], 0);
			f2 = new Fiber(pF2, tempFP, cb, new double[3], 0);
			List<Fiber> lFibers = new List<Fiber> { f1, f2 };
			ContactParameters cp = new ContactParameters(0.01, 0.6, 1.0, 2.0);
			SizingParameters sp = new SizingParameters(10, 0.3, 0.5, 100.0, 1.0, 1.0, 2);
			myAnalysis = new LoadStepAnalysis(str, nTS,
			                                  dT, nSS, 1, cp);
			myAnalysis.AddNonContactSprings(sp);
			
			myAnalysis.Analyze(lFibers, cb, grd);
			
			myAnalysis.GenerateOutput(new OutputParameters(Directory.GetCurrentDirectory(), 
			                                               true, false, false, false), 1);
			//MessageBox.Show("Hey there handsome");
			
			
			//ksizing=37.62807
			//l0=0.1
			//
			//This should make no sizing 
			//Check Stresss (if it doesn't pass, could be a volume problem)
			
			 Assert.That(8.33091443261e-5, myAnalysis.HomogenizedStress[1][1,1], 0.000001); //Didn't calculate this, just looked it up at a good point in life
			
			//Look in the excel spreadsheet to do more with this...
			//ValidationOfSizingAssumption.xclx
		}
		[Test]
		public void ThreeFibersRVE_Shear()
		{
			nTS=10000;
			nSS=100;
			double Estar = 1d / ( 2.0 *(1d - 0.0 * 0.0 ) / 100.0 );
			double maxK = Math.PI* Estar * 1.0 / 4d;
			dT = Analysis.MaxDT(1.0, maxK, 0.0);
			
			double [] pF1 = new double [3]{0,3.0,0.5};
			double [] pF2 = new double [3]{0,3.0, 8.5};
			double [] pF3 = new double [3]{0,5.0, 8.5};
			double [] cbLengths = new double [3]{1.0,10.0,10.0};
			double [] str = new double [6]{0.0,0.0,0.0,0.0,0.0,-0.1};
		                       
			cb = new CellBoundary(cbLengths, myMath.VectorMath.ScalarMultiply(1.0/nTS, str), 
			                      myMath.VectorMath.ScalarMultiply(1.0/nTS/dT, str));
			//Be carefull: if grid is too small, then we lose contact pairs with long sizing
			Grid grd = new Grid(-2.0, -2.0, 2.0*cbLengths[1], 2.0*cbLengths[2], 2.0);
			FiberParameters tempFP = new FiberParameters(1.0, 1.0, 1.0, 100.0, 100.0, 0.0, 1.0);
			f1 = new Fiber(pF1, tempFP, cb, new double[3], 0);
			f2 = new Fiber(pF2, tempFP, cb, new double[3], 0);
			Fiber f3 = new Fiber(pF3, tempFP, cb, new double[3], 0);
			List<Fiber> lFibers = new List<Fiber> { f1, f2, f3};
			ContactParameters cp = new ContactParameters(0.01, 0.6, 1.0, 2.0);
			SizingParameters sp = new SizingParameters(10, 0.3, 0.5, 100.0, 1.0, 1.0, 2);
			myAnalysis = new LoadStepAnalysis(str, nTS,
			                                  dT, nSS, 1, cp);
			myAnalysis.AddNonContactSprings(sp);
			
			
			//Analysis mySecondAnalysis = new LoadStepAnalysis(str, nTS,dT, nSS, 1, cp);
			myAnalysis.Analyze(lFibers, cb, grd);
			
			myAnalysis.GenerateOutput(new OutputParameters(Directory.GetCurrentDirectory(), 
			                                               true, false, false, false), 1);
			
		}
	}
}
*/
