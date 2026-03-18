using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FDEMCore;

namespace FDEMTests
{
    public class TestMatrixWElasticFiberShear
    {

        CellBoundary cb;

        private void SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis)
        {
            
            //Make cell boundary
            double[] strain = new double[6] { 0, 0, 0, 0, 0.1, 0.0 };
            cb = new CellBoundary(new double[3] { 1.0, 14.0, 14.0 }, new double[3], strain, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            myGrid = new Grid(-7.0, -7.0, 50, 50, 10);
            //Make the two fibers next to each other
            FiberParameters tempFP = new FiberParameters(3.0, 1.0, 0.02, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            MatrixAssemblyParameters tempMCP = new MatrixAssemblyParameters(100.0, 0.3, 1.0, 1, 0.01, "MatrixModel1", "10",
                "MaxPrincStressZIntDNoDamage", "1000")
            {
                dontMakeProjections = true
            };
            ContactParameters tempCP = new ContactParameters(0.01, 0.6, 0.0, 2.0);

            lFibers = new List<Fiber> { new Fiber(new double[] { 0.0, 0.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0) ,
            new Fiber(new double[] { 0.0, 7.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0)};


            myAnalysis = new ImplicitAnalysis_NumericalTangent(1, 100, 0.00001, 10, strain, tempCP, 0.001);
            myAnalysis.AddNonContactSprings(tempMCP);
        }

        [Test]
        public void Test2FibNR_XForce()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);

            myAnalysis.Analyze(lFibers, cb, myGrid);

            Assert.That(myAnalysis.NR.Iterations, Is.EqualTo(1).Within(0.00001));
            Assert.That(myAnalysis.NR.FinalError, Is.EqualTo(0.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][0], Is.EqualTo(0.0035875578389055773).Within(0.001));
            Assert.That(lFibers[1].Position[1][1], Is.EqualTo(7.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][2], Is.EqualTo(0).Within(0.00001));
            Assert.That(lFibers[1].Rotation[1], Is.EqualTo(0).Within(0.00001));

        }

    }
}
