using System;
using NUnit.Framework;
using FDEMCore.Contact;
using RandomMath;
using FDEMCore;

namespace FDEMTests
{
    
    public class TestMatrixWithElasticFiberSpring
    {
        /* //Out of date: old matrix model
        Fiber f1;
        Fiber f2;
        CellBoundary cb;
        FToFSpring ffSpring;
        int n = 22;

        private void SetupTwoFiberTest(double radius, double[] pF1, double[] pF2, double[] vF1, double[] vF2)
        {
            cb = new CellBoundary(new double[3] { 1.0, 1.0, 1.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            FiberParameters tempFP = new FiberParameters(radius, 1.0, 0.02, 2400, 2400, 0.3, 1.0);
            f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
            f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
            f1.UpdateTimeStep(0.0001);
            f1.UpdatePosition();
            f2.UpdateTimeStep(0.0001);
            f2.UpdatePosition();
            //ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
            MatrixAssemblyParameters mp = new MatrixAssemblyParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers", "VonMises", "1000/");
            ffSpring = new FToFMatrixContinuumElasticFiberSpring(myMath.VectorMath.Norm(myMath.VectorMath.Subtract(pF1, pF2)),
                myMath.VectorMath.Subtract(pF1, pF2), mp, f1, f2, 0, 1);

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
            SetupTwoFiberTest(0.003, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.007, 0.0 }, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.0, 0.0 });
            Matrix_ElasticFiber myIndefInt = new Matrix_ElasticFiber(0.003, 0.007, f1.OLength,
                3500.0 / ((1.0 + 0.3) * (1.0 - 2.0 * 0.3)), 0.3, f1, f2, 1.0);
            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.003 * 0.99, 0.0, 0.0, -0.003 * 0.99);

            //Have these numbers from the Mathematica Code
            double[,] km11should = { {114.909, 0, 0, 0 }, {0, 348.373, 0.0, 0.0 }, 
                { 0, 0.0, 168.719, 0.590517 }, {0, 0.0, 0.590517, 0.00333693} };

            double[,] km12should = { { -114.909, 0, 0, 0}, { 0, -348.373, 0.0, 0}, 
                { 0, 0.0, -168.719, 0.590517}, { 0, 0.0, -0.590517, 0.000796688} };
            
            double[,] km21should = { { -114.909, 0, 0, 0 }, { 0, -348.373, 0.0, 0.0 }, 
                { 0, 0.0, -168.719, -0.590517}, { 0,0, 0.590517 , 0.000796688}};

            double[,] km22should = { { 114.909, 0, 0, 0}, { 0, 348.373, 0.0, 0.0},
                 { 0, 0, 168.719, -0.590517}, { 0, 0.0, -0.590517, 0.00333693} };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                     Assert.That(km11should[i,j], myIndefInt.Km_11[i,j], 0.001);
                     Assert.That(km12should[i, j], myIndefInt.Km_12[i, j], 0.001);
                     Assert.That(km21should[i, j], myIndefInt.Km_21[i, j], 0.001);
                     Assert.That(km22should[i, j], myIndefInt.Km_22[i, j], 0.001);
                }

            }
        }

        [Test]
        public void TestMatrixStiffnessesUnbalancedIntegral()
        {
            double[,] k = new double[4, 8];
            double[,] d = new double[4, 8];
            SetupTwoFiberTest(0.003, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.007, 0.0 }, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.0, 0.0 });
            Matrix_ElasticFiber myIndefInt = new Matrix_ElasticFiber(0.003, 0.007, f1.OLength,
                3500.0 / ((1.0 + 0.3) * (1.0 - 2.0 * 0.3)), 0.3, f1, f2, 1.0);
            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.003 * 0.99, 0.0, 0.0, -0.003 * 0.99 / 3.0);

            //Have these numbers from the Mathematica Code
            double[,] km11should = {{ 81.9249, 0, 0, 0 }, { 0, 259.11, 0, -0.105296 }, { 0, 0, 109.552, 0.383431}, { 0, -0.105296, 0.383431, 0.00200083 } };
            double[,] km12should = { { -81.9249, 0, 0, 0}, {0, -259.11, 0, 0.105296}, { 0, 0, -109.552, 0.383431}, { 0, 0.105296, -0.383431, 0.00068319 } };
            double[,] km21should = { { -81.9249, 0, 0, 0 }, { 0, -259.11, 0, 0.105296}, { 0, 0, -109.552, -0.383431}, { 0, 0.105296, 0.383431, 0.00068319 } };
            double[,] km22should = { { 81.9249, 0, 0, 0 }, { 0, 259.11, 0, -0.105296}, { 0, 0, 109.552, -0.383431}, { 0, -0.105296, -0.383431, 0.00200083} };
            double[,] km13should = { { 0 }, { -7.99615 }, { 0 }, { -0.00119093 } };
            double[,] km23should = { { 0 }, { 7.99615 }, { 0 }, { 0.00119093 } };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                     Assert.That(km11should[i, j], myIndefInt.Km_11[i, j], 0.001);
                     Assert.That(km12should[i, j], myIndefInt.Km_12[i, j], 0.001);
                     Assert.That(km21should[i, j], myIndefInt.Km_21[i, j], 0.001);
                     Assert.That(km22should[i, j], myIndefInt.Km_22[i, j], 0.001);
                    
                }
                 Assert.That(km13should[i,0], myIndefInt.Km_13[i, 0], 0.001);
                 Assert.That(km23should[i, 0], myIndefInt.Km_23[i, 0], 0.001);
            }
        }

        [Test]
        public void TestMatrixStiffnessesOverlappingFiber()
        {
            //8/20/2021: doesn't Pass

            double[,] k = new double[4, 8];
            double[,] d = new double[4, 8];
            SetupTwoFiberTest(0.0025, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.004, 0.0 }, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.0, 0.0 });

            //This is just to get the z values
            //MatrixContinuumParameters mp = new MatrixContinuumParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers", "VonMises", "1000/");
            //FToFMatrixContinuumElasticFiberSpring myspring = new FToFMatrixContinuumElasticFiberSpring(0.004, new double[] { 0.0, 0.004, 0.0 }, mp,f1, f2, 0, 1);


            Matrix_ElasticFiber myIndefInt = new Matrix_ElasticFiber(0.0025, 0.004, f1.OLength,
                3500.0 / ((1.0 + 0.3) * (1.0 - 2.0 * 0.3)), 0.3, f1, f2, 1.0);
            
            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.0025 * 0.99, -0.0015*1.01, 0.0015 * 1.01, -0.0025 * 0.99 );

            //Have these numbers from the Mathematica Code
            double[,] km11should = { { 189.751, 0, 0, 0 }, { 0, 520.341, 0, 0.0 }, { 0, 0, 333.54, 0.66708 }, { 0, 0, 0.66708, 0.00297997 } };
            double[,] km12should = { {-189.751, 0, 0, 0}, {0, -520.341, 0, 0}, {0, 0, -333.54, 0.66708}, {0, 0, -0.66708, -0.000311647}};
            double[,] km21should = { {-189.751, 0, 0, 0}, {0, -520.341, 0, 0}, {0, 0, -333.54, -0.66708}, {0, 0, 0.66708, -0.000311647}};
            double[,] km22should ={ {189.751, 0, 0, 0}, {0, 520.341, 0, 0}, {0, 0, 333.54, -0.66708}, {0, 0, -0.66708, 0.00297997}};

            double[,] km13should = { { 0.0 }, { -3.87692 }, { 0.0 }, { 0.0 } };
            double[,] km23should = { { 0.0 }, { 3.87692 }, { 0.0 }, { 0.0 } };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                     Assert.That(km11should[i, j], myIndefInt.Km_11[i, j], 0.001);
                     Assert.That(km12should[i, j], myIndefInt.Km_12[i, j], 0.001);
                     Assert.That(km21should[i, j], myIndefInt.Km_21[i, j], 0.001);
                     Assert.That(km22should[i, j], myIndefInt.Km_22[i, j], 0.001);

                }
                 Assert.That(km13should[i, 0], myIndefInt.Km_13[i, 0], 0.001);
                 Assert.That(km23should[i, 0], myIndefInt.Km_23[i, 0], 0.001);
            }
        }

        /* //Didn't implement this into the Mathematica code yet....
        [Test]
        public void TestAllStiffnessTermsBalanced()
        {
            double[,] k = new double[4, 8];
            double[,] d = new double[4, 8];
            setupTwoFiberTest(0.003, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.007, 0.0 }, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.0, 0.0 });
            Matrix_ElasticFiber myIndefInt = new Matrix_ElasticFiber(0.003, 0.007, f1.OLength,
                3500.0 / ((1.0 + 0.3) * (1.0 - 2.0 * 0.3)), 0.3, f1, f2, 1.0);
            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.003 * 0.99, 0.0, 0.0, -0.003 * 0.99);

            //Have these numbers from the Mathematica Code
            double[,] kmmshould = { { 151.832, 0, 0, -114.909, 0, 0 }, { 0, 477.604, 0.0, 0, -348.373, 0.0}, { 0, 0.0, 0.00398215, 0, 0.0, 0.000796688 },
                { -114.909, 0, 0, 151.832, 0, 0 }, { 0, -348.373, 0.0, 0, 477.604, 0.0}, { 0, 0.0, 0.000796688, 0, 0.0, 0.00398215 } };
            double[,] kffshould = { {0.000645212, 0, 0, 0, 0}, {0, 36.9231, 0, 0, 0}, {0, 0, 129.231, 0, 4.15385},
                {0, 0, 0, 0.000645212, 0}};

            double[,] kmfshould ={ {0, 0, 0, 0, 0}, {0, 0, 0, 0, -7.84038}, {-0.000645212, 0, 0, 0, 0.0}, {0, -36.9231, 0, 0, 0},
                {0, 0, -129.231, 0, 7.84038}, {0, 0, 0, -0.000645212, 0.0}};
            double[,] kfmshould = { {0, 0, -0.000645212, 0, 0, 0}, {0, 0, 0, -36.9231, 0, 0},
                {0, 0, 0, 0, -129.231, 0}, {0, 0, 0, 0, 0, -0.000645212}};
            double[,] kmmInvshould ={ {0.0154161, 0.0, 0.0, 0.0116672, 0.0, 0.0}, {0.0, 0.00447439, 0.0, 0.0, 0.0032637, 0.0}, {0.0, 0.0, 261.591, 0.0, 0.0, -52.3353},
                {0.0116672, 0.0, 0.0, 0.0154161, 0.0, 0.0}, {0.0, 0.0032637, 0.0, 0.0, 0.00447439, 0.0}, {0.0, 0.0, -52.3353, 0.0, 0.0, 261.591}};
            double[,] keqshould ={ {0.000536312, 0.0, 0.0, 0.0000217871, 0.0}, {0.0, 15.906, 0.0, 0.0, 0.0},
                {0.0, 0.0, 54.5058, 0.0, 5.38054}, {0.0000217871, 0.0, 0.0, 0.000536312, 0.0}};

            double[,] kmmDif = MatrixMath.Subtract(kmmshould, myIndefInt.Kmm);
            double[,] kfmDif = MatrixMath.Subtract(kfmshould, myIndefInt.Kfm);
            double[,] kmfDif = MatrixMath.Subtract(kmfshould, myIndefInt.Kmf);
            double[,] kffDif = MatrixMath.Subtract(kffshould, myIndefInt.Kff);
            double[,] kmmInvDif = MatrixMath.Subtract(kmmInvshould, myIndefInt.KmmInverse);
            double[,] keqDif = MatrixMath.Subtract(keqshould, k);


            foreach (double diff in kmmDif) { Assert.That(diff, 0.0, 0.001);}
            foreach (double diff in kfmDif) {  Assert.That(diff, 0.0, 0.001); }
            foreach (double diff in kmfDif) {  Assert.That(diff, 0.0, 0.001); }
            foreach (double diff in kffDif) {  Assert.That(diff, 0.0, 0.001); }
            foreach (double diff in kmmInvDif) {  Assert.That(diff, 0.0, 0.001); }
            foreach (double diff in keqDif) {  Assert.That(diff, 0.0, 0.001); }
        }

        [Test]
        public void TestTwoFibersRigidRotation()
        {
            double radius = 3.0;
            cb = new CellBoundary(new double[3] { 1.0, 1.0, 1.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            FiberParameters tempFP = new FiberParameters(radius, 1.0, 0.02, 2400, 2400, 0.3, 1.0);
            f1 = new Fiber(new double[] { 0.0, 0.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0);
            f2 = new Fiber(new double[] { 0.0, 7.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0);

            //ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
            double[] x12 = VectorMath.Subtract(f1.CurrentPosition, f2.CurrentPosition);
            double dist = VectorMath.Norm(x12);
            MatrixAssemblyParameters mp = new MatrixAssemblyParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers", "VonMises", "1000/");
            FToFMatrixContinuumElasticFiberSpring matSpring = new FToFMatrixContinuumElasticFiberSpring(dist,x12, mp, f1, f2, 0, 1);

            matSpring.Update(0, 0.0);

            //90 deg rotation: no force

            f2.CurrentPosition[1] = 0;
            f2.CurrentPosition[2] = 7;
            f2.CurrentRotation = 1.570796327;
            f1.CurrentRotation = 1.570796327;

            matSpring.Update(1, 0.0);
            ChechBothFibersZeroForce(f1, f2, 0);

            //45 deg rotation: no force 

            f2.CurrentPosition[1] = 4.949747468;
            f2.CurrentPosition[2] = 4.949747468;
            f2.CurrentRotation = 0.785398163;
            f1.CurrentRotation = 0.785398163;

            matSpring.Update(2, 0.0);
            ChechBothFibersZeroForce(f1, f2, 1);

            //Q2 deg rotation: no force

            f2.CurrentPosition[1] = -4.949747468;
            f2.CurrentPosition[2] = 4.949747468;
            f2.CurrentRotation = 2.35619449;
            f1.CurrentRotation = 2.35619449;

            matSpring.Update(3, 0.0);
            ChechBothFibersZeroForce(f1, f2, 2);

            //Q3 deg rotation: no force

            f2.CurrentPosition[1] = -4.949747468;
            f2.CurrentPosition[2] = -4.949747468;
            f2.CurrentRotation = 3.926990817;
            f1.CurrentRotation = 3.926990817;

            matSpring.Update(4, 0.0);
            ChechBothFibersZeroForce(f1, f2, 3);

            //Q4 deg rotation: no force

            f2.CurrentPosition[1] = 4.949747468;
            f2.CurrentPosition[2] = -4.949747468;
            f2.CurrentRotation = 5.497787144;
            f1.CurrentRotation = 5.497787144;

            matSpring.Update(5, 0.0);
            ChechBothFibersZeroForce(f1, f2, 4);

            //Q4 deg rotation: no force

            f2.CurrentPosition[1] = 4.949747468;
            f2.CurrentPosition[2] = -4.949747468;
            f2.CurrentRotation = -0.785398163;
            f1.CurrentRotation = -0.785398163;

            matSpring.Update(6, 0.0);
            ChechBothFibersZeroForce(f1, f2, 5);
        }

        [Test]
        public void TestTwoFibersRigidRotation_FibersSwitched()
        {
            double radius = 3.0;
            cb = new CellBoundary(new double[3] { 1.0, 1.0, 1.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            FiberParameters tempFP = new FiberParameters(radius, 1.0, 0.02, 2400, 2400, 0.3, 1.0);
            f2 = new Fiber(new double[] { 0.0, 0.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0);
            f1 = new Fiber(new double[] { 0.0, 7.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0);

            //ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
            double[] x12 = VectorMath.Subtract(f1.CurrentPosition, f2.CurrentPosition);
            double dist = VectorMath.Norm(x12);
            MatrixAssemblyParameters mp = new MatrixAssemblyParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers", "VonMises", "1000/");
            FToFMatrixContinuumElasticFiberSpring matSpring = new FToFMatrixContinuumElasticFiberSpring(dist, x12, mp, f1, f2, 0, 1);

            matSpring.Update(0, 0.0);

            //90 deg rotation: no force

            f1.CurrentPosition[1] = 0;
            f1.CurrentPosition[2] = 7;
            f2.CurrentRotation = 1.570796327;
            f1.CurrentRotation = 1.570796327;

            matSpring.Update(1, 0.0);
            ChechBothFibersZeroForce(f1, f2, 0);

            //45 deg rotation: no force 

            f1.CurrentPosition[1] = 4.949747468;
            f1.CurrentPosition[2] = 4.949747468;
            f2.CurrentRotation = 0.785398163;
            f1.CurrentRotation = 0.785398163;

            matSpring.Update(2, 0.0);
            ChechBothFibersZeroForce(f1, f2, 1);

            //Q2 deg rotation: no force

            f1.CurrentPosition[1] = -4.949747468;
            f1.CurrentPosition[2] = 4.949747468;
            f2.CurrentRotation = 2.35619449;
            f1.CurrentRotation = 2.35619449;

            matSpring.Update(3, 0.0);
            ChechBothFibersZeroForce(f1, f2, 2);

            //Q3 deg rotation: no force

            f1.CurrentPosition[1] = -4.949747468;
            f1.CurrentPosition[2] = -4.949747468;
            f2.CurrentRotation = 3.926990817;
            f1.CurrentRotation = 3.926990817;

            matSpring.Update(4, 0.0);
            ChechBothFibersZeroForce(f1, f2, 3);

            //Q4 deg rotation: no force

            f1.CurrentPosition[1] = 4.949747468;
            f1.CurrentPosition[2] = -4.949747468;
            f2.CurrentRotation = 5.497787144;
            f1.CurrentRotation = 5.497787144;

            matSpring.Update(5, 0.0);
            ChechBothFibersZeroForce(f1, f2, 4);

            //Q4 deg rotation: no force

            f1.CurrentPosition[1] = 4.949747468;
            f1.CurrentPosition[2] = -4.949747468;
            f2.CurrentRotation = -0.785398163;
            f1.CurrentRotation = -0.785398163;

            matSpring.Update(6, 0.0);
            ChechBothFibersZeroForce(f1, f2, 5);
        }

        private void ChechBothFibersZeroForce(Fiber f1, Fiber f2, int j)
        {
             Assert.That(f1.CurrentMoments[j], 0.0, 0.00001);
             Assert.That(f1.CurrentForces[j][0], 0.0, 0.00001);
             Assert.That(f1.CurrentForces[j][1], 0.0, 0.00001);
             Assert.That(f1.CurrentForces[j][2], 0.0, 0.00001);
             Assert.That(f2.CurrentMoments[j], 0.0, 0.00001);
             Assert.That(f2.CurrentForces[j][0], 0.0, 0.00001);
             Assert.That(f2.CurrentForces[j][1], 0.0, 0.00001);
             Assert.That(f2.CurrentForces[j][2], 0.0, 0.00001);
        }
        
        */
    }
}