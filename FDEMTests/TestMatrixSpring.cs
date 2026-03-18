using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore;


namespace FDEMTests
{
    
    public class TestMatrixSpring
    {

        /* //Out of date: old matrix model
         * 
        //8/20/2021: doesn't Pass

        Fiber f1;
        Fiber f2;
        CellBoundary cb;
        FToFSpring ffSpring;
        int n = 22;

        private void SetupTwoFiberTest(double[] pF1, double[] pF2, double[] vF1, double[] vF2)
        {
            cb = new CellBoundary(new double[3] { 1.0, 1.0, 1.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            FiberParameters tempFP = new FiberParameters(0.003, 1.0, 0.02, 2400, 2400, 0.3, 1.0);
            f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
            f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
            f1.UpdateTimeStep(0.0001);
            f1.UpdatePosition();
            f2.UpdateTimeStep(0.0001);
            f2.UpdatePosition();
            //ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
            MatrixAssemblyParameters mp = new MatrixAssemblyParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuum", "VonMises", "1000/");
            
            ffSpring = new FToFMatrixContinuumElasticFiberSpring(myMath.VectorMath.Norm(myMath.VectorMath.Subtract(pF2, pF1)),
                myMath.VectorMath.Subtract(pF2, pF1), mp, f1, f2, 0, 1);

            for (int i = 0; i < n; i++)
            {
                ffSpring.Update(i + 1, 0.1);
                f1.UpdatePosition();
                f2.UpdatePosition();

            }
        }

        [Test]
        public void TestMatrixStiffnesses()
        {
            double[,] k = new double[4,8];
            double[,] d = new double[4,8];

            Matrix_RigidFibers myIndefInt = new Matrix_RigidFibers(0.003, 0.007, 0.02,
                3500.0 / ((1.0 + 0.3) * (1.0 - 2.0 * 0.3)), 0.3, 1.0, 1.0, 1.0, 1.0, 1.0);
            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.003 * 0.99, 0.0, 0.0, -0.003 * 0.99);
            //Have these numbers from the Mathematica Code
             Assert.That(113.027, k[0,0], 0.001);
             Assert.That(0.0, k[0, 1], 0.00000001);
             Assert.That(0.0, k[0, 2], 0.00000001);
             Assert.That(0.0, k[0, 3], 0.00000001);
             Assert.That(0.0, k[0, 4], 0.00000001);
             Assert.That(0.0, k[0, 5], 0.00000001);
             Assert.That(0.0, k[0, 6], 0.00000001);
             Assert.That(0.0, k[0, 7], 0.00000001);

             Assert.That(0.0, k[1, 0], 0.00000001);
             Assert.That(318.767, k[1, 1], 0.001);
             Assert.That(0.0, k[1, 2], 0.001);
             Assert.That(9.504, k[1, 3], 0.001);
             Assert.That(0.0, k[1, 4], 0.001);
             Assert.That(0.894627, k[1, 5], 0.001);
             Assert.That(0.0, k[1, 6], 0.001);
             Assert.That(0.703142, k[1, 7], 0.001);

             Assert.That(0.0, k[2, 0], 0.00000001);
             Assert.That(0.0, k[2, 1], 0.001);
             Assert.That(189.857, k[2, 2], 0.001);
             Assert.That(0.0, k[2, 3], 0.001);
             Assert.That(-0.771436, k[2, 4], 0.001);
             Assert.That(0.0, k[2, 5], 0.001);
             Assert.That(-0.557561, k[2, 6], 0.001);
             Assert.That(0.0, k[2, 7], 0.001);


             Assert.That(0.0, k[3, 0], 0.00000001);
             Assert.That(0.0, k[3, 1], 0.001);
             Assert.That(0.280156, k[3, 2], 0.001);
             Assert.That(0.0, k[3, 3], 0.001);
             Assert.That(-0.00149702, k[3, 4], 0.001);
             Assert.That(0.0, k[3, 5], 0.001);
             Assert.That(-0.000464077, k[3, 6], 0.001);
             Assert.That(0.0, k[3, 7], 0.001);
        }

        [Test]
        public void Test_v_Only()
        {
            n = 1;

            SetupTwoFiberTest(new double[3] { 0, 0, 0 }, new double[3] { 0, 0.007, 0 }, new double[3] { 0, 0, 0 }, new double[3] { 0, 2.0, 0 });

            //Forces
            
            //Normal Force
             Assert.That(0.0638, f1.CurrentForces[0][1], 0.001);
             Assert.That(-0.0638, f2.CurrentForces[0][1], 0.001);
            //Tangent Force (second step)
            //int step = 1;
             Assert.That(0, f1.CurrentForces[0][2], 0.00001);
             Assert.That(0, f2.CurrentForces[0][2], 0.00001);
            //Moment (second step)
             Assert.That(0, f1.CurrentMoments[0], 0.000001);
             Assert.That(0, f2.CurrentMoments[0], 0.000001);
        }

        [Test]
        public void Test_w_only()
        {
            n = 1;
            SetupTwoFiberTest(new double[3] { 0, 0, 0 }, new double[3] { 0, 0.007, 0 }, new double[3] { 0, 0, 0 }, new double[3] { 0, 0,2.0 });

            //Normal Force
             Assert.That(1.0866562691077484, f1.CurrentForces[0][1], 0.001);
             Assert.That(37.978265613577705, f1.CurrentForces[0][2], 0.001);
             Assert.That(-1.0866562691077484, f2.CurrentForces[0][1], 0.001);
             Assert.That(-37.978265613577705, f2.CurrentForces[0][2], 0.001);
            //Moment (second step)
             Assert.That(0.0560643873824101, f1.CurrentMoments[0], 0.000001);
             Assert.That(0.0560643873824101, f2.CurrentMoments[0], 0.000001);
        }
        */
    }
}