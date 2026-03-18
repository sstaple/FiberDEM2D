using System;
using NUnit.Framework;
using FDEMCore.Contact;
using RandomMath;
using System.Collections.Generic;
using FDEMCore;

namespace FDEMTests
{
    
    public class TestMatrixWithElasticFiberSpring_Implicit
    {

        CellBoundary cb;


        //9/8/2022: This makes the stiffness singular, so commenting out until debug
        /*
        [Test]
        public void TestTwoFiberNewtonRaphson_NoLoading()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);

            myAnalysis.Analyze(lFibers, cb, myGrid);
             Assert.That(1, myAnalysis.NR.Iterations, 0.00001);
             Assert.That(0.0, myAnalysis.NR.FinalError, 0.00001);
        }
        */
        [Test]
        public void Test2FibNR_XForce()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0.01, 0, 0, 0 };

            myAnalysis.Analyze(lFibers, cb, myGrid);

            Assert.That(myAnalysis.NR.Iterations, Is.EqualTo(1).Within(0.00001));
            Assert.That(myAnalysis.NR.FinalError, Is.EqualTo(0.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][0], Is.EqualTo(0.0035875578389055773).Within(0.001));
            Assert.That(lFibers[1].Position[1][1], Is.EqualTo(7.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][2], Is.EqualTo(0).Within(0.00001));
            Assert.That(lFibers[1].Rotation[1], Is.EqualTo(0).Within(0.00001));

        }

        [Test]
        public void Test2FibNR_YForce()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0, 0.01, 0, 0 };

            myAnalysis.Analyze(lFibers, cb, myGrid);

            Assert.That(myAnalysis.NR.Iterations, Is.EqualTo(1).Within(0.00001));
            Assert.That(myAnalysis.NR.FinalError, Is.EqualTo(0.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][0], Is.EqualTo(0.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][1], Is.EqualTo(7.0011594447796996).Within(0.001));
            Assert.That(lFibers[1].Position[1][2], Is.EqualTo(0).Within(0.00001));
            Assert.That(lFibers[1].Rotation[1], Is.EqualTo(0).Within(0.00001));


        }

        [Test]
        public void Test2FibNR_ZForce()
        {
            //Add a z-force, no rotation BC on any fibers
            SetupAnalysisInitialSlope(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_InitialNumericalTangent myAnalysis);

            //ImplicitAnalysis_NumericalTangent myAnalysis;
            //SetupAnalysis(out myGrid, out lFibers, out myAnalysis);

            myAnalysis.maxNRIter = 1;
            myAnalysis.maxNRError = 0.0000001;
            myAnalysis.stepSizePercent = 0.1;
            myAnalysis.nLoadSteps = 1;

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0, 0, 0.0001, 0 };

            myAnalysis.Analyze(lFibers, cb, myGrid);

            Assert.That(myAnalysis.NR.Iterations, Is.EqualTo(1).Within(0.00001));
            Assert.That(myAnalysis.NR.FinalError, Is.EqualTo(9.9949598036319333E-05).Within(0.00001));
            Assert.That(lFibers[1].Position[1][0], Is.EqualTo(0.0).Within(0.00001));
            Assert.That(lFibers[1].Position[1][1], Is.EqualTo(6.9999999542918143).Within(0.00001));
            Assert.That(lFibers[1].Position[1][2], Is.EqualTo(2.482382888691316E-06).Within(0.00001));
            Assert.That(lFibers[1].Rotation[1], Is.EqualTo(4.6547606406841853E-07).Within(0.00001));
            /**/
            //myAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\fiberdem\\bin\\Debug\\Test",
            //    true, false, false, false), 0);
            /**/
        }

        [Test]
        public void Test2FibNR_ZDisp()
        {
            //Add a z-displacement, no rotation BC on any fibers
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);
            myAnalysis.maxNRIter = 10000;
            myAnalysis.maxNRError = 0.0000000001;
            myAnalysis.stepSizePercent = 0.01;
            myAnalysis.nLoadSteps = 8;

            myAnalysis.Analyze(lFibers, cb, myGrid);

            /* Assert.That(618, myAnalysis.NR.Iterations, 0.00001);
             Assert.That(9.9974332492211612E-06, myAnalysis.NR.FinalError, 0.00001);
             Assert.That(0.0, lFibers[1].Position[1][0], 0.00001);
             Assert.That(6.9848190048414649, lFibers[1].Position[1][1], 0.00001);
             Assert.That(0.460752135715668, lFibers[1].Position[1][2], 0.00001);
             Assert.That(0.065869346352609395, lFibers[1].Rotation[1], 0.00001);
           */
            myAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\fiberdem\\bin\\Debug\\Test",
                true, false, false, false), 0);
            /**/
        }

        [Test]
        public void Test2FibNR_ZForceSwitched()
        {
            //8/20/2021: doesn't Pass
            //Add a z-displacement, no rotation BC on any fibers
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);
            
            //Switch fiber 1 and fiber 2....
            Fiber f1 = lFibers[0];
            Fiber f2 = lFibers[1];
            lFibers = new List<Fiber>(new Fiber[] { f2, f1 });

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0, 0, 0.001, 0 };
            myAnalysis.nLoadSteps = 50;
            myAnalysis.maxNRIter = 100;
            myAnalysis.maxNRError = 0.00000001;

            myAnalysis.Analyze(lFibers, cb, myGrid);

            Assert.That(lFibers[1].Position[1][0], Is.EqualTo(0.0).Within(0.00001));
             Assert.That(lFibers[1].Position[1][1], Is.EqualTo(7).Within(0.00001));
             Assert.That(lFibers[1].Position[1][2], Is.EqualTo(7).Within(0.00001));
             Assert.That(lFibers[1].Rotation[1], Is.EqualTo(-1.5707960497646261).Within(0.00001));
             Assert.That(lFibers[0].Rotation[1], Is.EqualTo(-1.5707960497646261).Within(0.00001));
           /* */
            myAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\fiberdem\\bin\\Debug\\Test",
                true, false, false, false), 0);
            
        }

        [Test]
        public void Test2FibNR_ZForce_NoMoment()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);
            myAnalysis.maxNRIter = 1000;
            myAnalysis.maxNRError = 0.00000001;
            myAnalysis.nLoadSteps = 100;

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0, 0, 0.00001, 0 };

            myAnalysis.Analyze(lFibers, cb, myGrid);

            /* Assert.That(0.0, lFibers[1].Position[1][0], 0.00001);
             Assert.That(0.0, lFibers[1].Position[1][1], 0.00001);
             Assert.That(7.0, lFibers[1].Position[1][2], 0.01);
             Assert.That(1.5707960497646261, lFibers[1].Rotation[1], 0.00001);
             Assert.That(1.5707960497646261, lFibers[0].Rotation[1], 0.00001);
            */
            myAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\fiberdem\\bin\\Debug\\Test",
                true, false, false, false), 0);
        }

        [Test]
        public void Test2FibNR_ZForce_NoMoment_Down()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);
            myAnalysis.maxNRIter = 1;
            myAnalysis.maxNRError = 0.00000001;
            myAnalysis.nLoadSteps = 50;

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0, 0, -0.0001, 0 };

            myAnalysis.Analyze(lFibers, cb, myGrid);

            /* Assert.That(0.0, lFibers[1].Position[1][0], 0.00001);
             Assert.That(0.0, lFibers[1].Position[1][1], 0.00001);
             Assert.That(-7.0, lFibers[1].Position[1][2], 0.01);
             Assert.That(-1.5707960497646261, lFibers[1].Rotation[1], 0.00001);
             Assert.That(-1.5707960497646261, lFibers[0].Rotation[1], 0.00001);
*/
            myAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\fiberdem\\bin\\Debug\\Test",
                true, false, false, false), 0);
            /**/
        }

        [Test]
        public void Test2FibNR_Moment()
        {
            SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis);
            myAnalysis.maxNRIter = 100;
            myAnalysis.maxNRError = 0.00000001;
            myAnalysis.nLoadSteps = 1;

            myAnalysis.AppliedForce = new double[] { 0, 0, 0, 0, 0, 0, 0, 0.01};

            myAnalysis.Analyze(lFibers, cb, myGrid);


            // Assert.That(5, myAnalysis.NR.Iterations, 0.00001);
             Assert.That(myAnalysis.NR.FinalError, Is.EqualTo(0.0).Within(0.00001));
             Assert.That(lFibers[1].Position[1][0], Is.EqualTo(0.0).Within(0.00001));
             Assert.That(lFibers[1].Position[1][1], Is.EqualTo(6.999999851404564).Within(0.00001));
             Assert.That(lFibers[1].Position[1][2], Is.EqualTo(0.0010199181260017105).Within(0.00001));
             Assert.That(lFibers[1].Rotation[1], Is.EqualTo(0.00029139596966779296).Within(0.00001));
            /**/
            /*myAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\Programs\\FiberDEM_2D\\fiberdem\\bin\\Debug\\Test",
                true, false, false, false), 0);
            */
        }

        

        private void SetupAnalysis(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_NumericalTangent myAnalysis)
        {
            int nIntPts = 1000;
            //Make cell boundary
            double[] strain = new double[6] { 0, 0, 0, 0, 0.0, 0.0 };
            cb = new CellBoundary(new double[3] { 1.0, 14.0, 14.0 }, new double[3], strain, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            myGrid = new Grid(-7.0, -7.0, 50, 50, 10);
            //Make the two fibers next to each other
            FiberParameters tempFP = new FiberParameters(3.0, 1.0, 0.02, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            MatrixAssemblyParameters tempMCP = new MatrixAssemblyParameters(100.0, 0.3, 1.0, 1, 0.01, "ElasticFibersElasticMatrix", " ",
                "DamageFracturEnergyAndStrength", String.Format("1000/ 1000/ {0}", nIntPts))
            {
                dontMakeProjections = true
            };
            ContactParameters tempCP = new ContactParameters(0.01, 0.6, 0.0, 2.0);

            lFibers = new List<Fiber> { new Fiber(new double[] { 0.0, 0.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0) ,
            new Fiber(new double[] { 0.0, 7.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0)};
            

            myAnalysis = new ImplicitAnalysis_NumericalTangent(1, 100, 0.00001, 10, strain, tempCP, 0.001);
            myAnalysis.AddNonContactSprings(tempMCP);


        }
        private void SetupAnalysisInitialSlope(out Grid myGrid, out List<Fiber> lFibers, out ImplicitAnalysis_InitialNumericalTangent myAnalysis)
        {
            //ImplicitAnalysis, line 237, must uncomment to add a force in the moment also so that it can converge
            int nIntPts = 10;
            //Make cell boundary
            double[] strain = new double[6] { 0, 0, 0, 0, 0.0, 0.0 };
            cb = new CellBoundary(new double[3] { 1.0, 14.0, 14.0 }, new double[3], strain, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            myGrid = new Grid(-7.0, -7.0, 50, 50, 10);
            //Make the two fibers next to each other
            FiberParameters tempFP = new FiberParameters(3.0, 1.0, 0.02, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            MatrixAssemblyParameters tempMCP = new MatrixAssemblyParameters(100.0, 0.3, 1.0, 1, 0.01, "ElasticFibersElasticMatrix", " ",
                "DamageFracturEnergyAndStrength", String.Format("1000/ 1000/ {0}", nIntPts))
            {
                dontMakeProjections = true
            };
            ContactParameters tempCP = new ContactParameters(0.01, 0.6, 0.0, 2.0);

            lFibers = new List<Fiber> { new Fiber(new double[] { 0.0, 0.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0),
            new Fiber(new double[] { 0.0, 7.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0)};
            

            myAnalysis = new ImplicitAnalysis_InitialNumericalTangent(1, 100, 0.00001, 10, strain, tempCP, 0.001);
            myAnalysis.AddNonContactSprings(tempMCP);


        }
    }
}