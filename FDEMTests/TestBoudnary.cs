/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 10/13/2015
 * Time: 10:05 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using NUnit.Framework;
using FDEMCore;

namespace FDEMTests
{
	/// <summary>
	/// See file "3DStrains_V3.wxy in the folder "Derivations" for these numbers.  If something is off, you can debug through it.
	/// </summary>
	
	public class TestBoundary
	{
		CellBoundary cb;
		
		private void SetupBoundary(){
			cb = new CellBoundary(new double [3]{1.1,1.2,1.3}, new double[3], new double[6]{0.1,0.2,0.3,0.15,0.25,0.35}, 
			                      new double[6]{0.01,0.02,0.03,0.015,0.025,0.035});
			cb.UpdatePosition();
		}
		[Test]
		public void TestundefXtoDefx()
		{
			SetupBoundary();
			double [] X = cb.UndefXtoDefx(new double[3] {2.0,3.1,4.2});
			Assert.That(X[0], Is.EqualTo(4.956889145421755));
            Assert.That(X[1], Is.EqualTo(5.66638590684424));
            Assert.That(X[2], Is.EqualTo(4.488576695871084));
        }
		[Test]
		public void TestRotateNormals()
		{
			SetupBoundary();
			double [] n = cb.RotateNormals(new double[3]{0,0,1.0});
			Assert.That(n[0], Is.EqualTo(0.0));
            Assert.That(n[1], Is.EqualTo(0.0));
            Assert.That(n[2], Is.EqualTo(1.0));

            double [] n2 = cb.RotateNormals(new double[3]{0.4,-1.0,1.0});
			Assert.That(n2[0], Is.EqualTo(0.228635158481203));
            Assert.That(n2[1], Is.EqualTo(-0.598354267448643));
            Assert.That(n2[2], Is.EqualTo(0.767918052224502));
        }
		[Test]
		public void TestundefVtoDefv()
		{
			SetupBoundary();
			double [] V = cb.UndefVtoDefv(new double[3] {2.0,3.1,4.2});
			Assert.That(V[0], Is.EqualTo(0.2718073191619387));
            Assert.That(V[1], Is.EqualTo(0.186359010162234));
            Assert.That(V[2], Is.EqualTo(-0.02773725998961982));
        }
		[Test]
		public void TestdefyzToDefxAndv()
		{
			SetupBoundary();
			double [] xx_vx = cb.DefyzToDefxAndv(new double[3]{31.58,2.1,-3.2}, 1.1/2.0);
			Assert.That(xx_vx[0], Is.EqualTo(0.09128248395008587));
            Assert.That(xx_vx[1], Is.EqualTo(-0.04184034007588251));
        }
		[Test]
		public void TestNLStrainToDisplacement()
		{
			SetupBoundary();
			double [] d = cb.NLStrainToDisplacement(new double[6]{0.1,0.2,0.3,0.15,0.25,0.35});
			Assert.That(d[0], Is.EqualTo(0.1049896265113654));
            Assert.That(d[1], Is.EqualTo(0.1813037319865605));
            Assert.That(d[2], Is.EqualTo(0.08932135824581171));
            Assert.That(d[3], Is.EqualTo(0.2673072429237324));
            Assert.That(d[4], Is.EqualTo(0.4281927555819408));
            Assert.That(d[5], Is.EqualTo(0.4632700010803578));
        }
	}
}
