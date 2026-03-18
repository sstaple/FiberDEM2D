using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore.Contact.FailureTheories;
using RandomMath;
using FDEMCore;

namespace FDEMTests
{

    public class TestMatrixWithElastifFiber_Damage
    {
        /* //Out of date: this is with one of the old versions of the model...

        Fiber f1;
        Fiber f2;
        CellBoundary cb;
        FToFSpring ffSpring;
        int n = 22;
        MatrixAssemblyParameters mp;

        private void SetupTwoFiberTest(double radius, double[] pF1, double[] pF2, double[] vF1, double[] vF2)
        {
            cb = new CellBoundary(new double[3] { 1.0, 1.0, 1.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            FiberParameters tempFP = new FiberParameters(radius, 1.0, 0.02, 240000, 240000, 0.3, 1.0);
            f1 = new Fiber(pF1, tempFP, cb, vF1, 0);
            f2 = new Fiber(pF2, tempFP, cb, vF2, 0);
            f1.UpdateTimeStep(0.0001);
            f1.UpdatePosition();
            f2.UpdateTimeStep(0.0001);
            f2.UpdatePosition();
            //ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
            mp = new MatrixAssemblyParameters(3500.0, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers_Damage", "DamageFracturEnergyAndStrength", "3.0/64/40");
            ffSpring = new FToFMatrixContinuumElasticFiberSpring_Damage(myMath.VectorMath.Norm(myMath.VectorMath.Subtract(pF1, pF2)),
                myMath.VectorMath.Subtract(pF1, pF2), mp, f1, f2, 0, 1);

        }

        [Test]
        public void TestMatrixStiffnesses()
        {
            double[,] k = new double[4, 8];
            double[,] d = new double[4, 8];

            SetupTwoFiberTest(0.003, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.007, 0.0 }, new double[] { 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.0, 0.0 });
            DamageFracturEnergyAndStrength myFT = (DamageFracturEnergyAndStrength)mp.FailureTheory;
            Matrix_ElasticFiber_Damage myIndefInt = new Matrix_ElasticFiber_Damage(myFT.NumberOfIntegrationPoints, 0.003, 0.007, f1.OLength,
                mp.E, mp.Nu, f1, f2, mp.DampCoeff, 0.003 * 0.99, 0.0, 0.0, -0.003 * 0.99, myFT.Strength, myFT.CriticalFractureEnergy);

            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.003 * 0.99, 0.0, 0.0, -0.003 * 0.99);

            //Have these numbers from the Mathematica Code (LinearElasticLinearDisplacements_v7_RemoveNu.nb
            double[,] km11should = { {114.909, 0, 0, 0 }, {0, 264.326, 0.0, 0.0 },
                { 0, 0.0, 149.348,  0.522717 }, {0, 0.0, 0.522717, 0.00313768} };

            double[,] km12should = { { -114.909, 0, 0, 0 }, { 0, -264.326, 0, 0.0 },
                { 0, 0, -149.348, 0.522717 }, { 0, 0.0, -0.522717, 0.000521339 } };

            double[,] km21should = { {-114.909, 0, 0, 0}, {0, -264.326, 0, 0.0},
            {0, 0, -149.348, -0.522717}, {0, 0.0, 0.522717, 0.000521339}};

            double[,] km22should = { { 114.909, 0, 0, 0 }, { 0, 264.326, 0, 0.0 },
            { 0, 0, 149.348, -0.522717 }, { 0, 0.0, -0.522717, 0.00313768 } };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Assert.That(myIndefInt.Km_11[i, j], Is.EqualTo(km11should[i, j]).Within(Math.Max(Math.Abs(km11should[i, j] * 0.01), 0.0000001)));
                    Assert.That(myIndefInt.Km_12[i, j], Is.EqualTo(km12should[i, j]).Within(Math.Max(Math.Abs(km12should[i, j] * 0.1), 0.0000001)));
                    Assert.That(myIndefInt.Km_21[i, j], Is.EqualTo(km21should[i, j]).Within(Math.Max(Math.Abs(km21should[i, j] * 0.01), 0.0000001)));
                    Assert.That(myIndefInt.Km_22[i, j], Is.EqualTo(km22should[i, j]).Within(Math.Max(Math.Abs(km22should[i, j] * 0.01), 0.0000001)));
                }

            }
        }

        [Test]
        public void TestForceVsDisplacement()
        {
            double radius = 2.7;
            double length = 35;
            double d0 = 7.0;
            cb = new CellBoundary(new double[3] { 1.0, 1.0, 1.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 }, new double[6] { 0, 0, 0, 0, 0.0, 0.0 });
            FiberParameters tempFP = new FiberParameters(radius, 1.0, length, 2400000.0, 2400000.0, 0.3, 1.0);
            f1 = new Fiber(new double[] { 0.0, 0.0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0);
            f2 = new Fiber(new double[] { 0.0, d0, 0.0 }, tempFP, cb, new double[] { 0.0, 0.0, 0.0 }, 0);
            f1.UpdateTimeStep(0.0001);
            f1.UpdatePosition();
            f2.UpdateTimeStep(0.0001);
            f2.UpdatePosition();
            //ContactParameters cp = new ContactParameters(0.01, 0.6, 0.0, 2.0);
            mp = new MatrixAssemblyParameters(2482, 0.37, 0.0, 1, 0.05, "MatrixContinuumElasticFibers_Damage", "DamageFracturEnergyAndStrength", "3.0/64.1/40");
            ffSpring = new FToFMatrixContinuumElasticFiberSpring_Damage(d0, new double[] { 0.0, -d0, 0.0 }, mp, f1, f2, 0, 1);

            double totalDef = 0.1;
            int n = 40;
            double[] disp = new double[n];
            double[] force = new double[n];

            for (int i = 1; i < n; i++)
            {
                disp[i] = (i) * totalDef / n;
                f2.CurrentPosition[1] += totalDef / n;
                ffSpring.Update(i, 0.1);
                f2.SumAndClearForces();
                force[i] = -1 * f2.CurrentNetForce[1];
            }

            //Here is the matlab solution, from TestCZM_v2_ConstantD.mlx
            double[] FMatlab = new double[]{0,518.5833067 ,1037.166613 ,1555.74992  ,2074.333227 ,2592.916534 ,3111.49984  ,3630.083147 ,4148.666454 ,4667.249761 ,5185.833067 ,5704.416374 ,6222.999681 ,
                6741.582987 ,7260.166294 ,7778.749601 ,8297.332908 ,8771.988195 ,9014.852595 ,9148.183113 ,9211.473417 ,9193.181043 ,9009.918355 ,8894.522186 ,8352.234727 ,8040.219645 ,
                7204.215388 ,6635.36942  ,6066.523452 ,4180.118105 ,3047.335246 ,2415.772735 ,1784.210225 ,1291.997251 ,851.1306014 ,507.0456463 ,224.2302847 ,39.20681419 ,0 };

            for (int i = 0; i < 4; i++)
            {
                 Assert.That(force[i], FMatlab[i], Math.Max(Math.Abs(FMatlab[i] * 0.01), 0.0000001));
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
            double[,] km11should = { { 81.9249, 0, 0, 0 }, { 0, 259.11, 0, -0.105296 }, { 0, 0, 109.552, 0.383431 }, { 0, -0.105296, 0.383431, 0.00200083 } };
            double[,] km12should = { { -81.9249, 0, 0, 0 }, { 0, -259.11, 0, 0.105296 }, { 0, 0, -109.552, 0.383431 }, { 0, 0.105296, -0.383431, 0.00068319 } };
            double[,] km21should = { { -81.9249, 0, 0, 0 }, { 0, -259.11, 0, 0.105296 }, { 0, 0, -109.552, -0.383431 }, { 0, 0.105296, 0.383431, 0.00068319 } };
            double[,] km22should = { { 81.9249, 0, 0, 0 }, { 0, 259.11, 0, -0.105296 }, { 0, 0, 109.552, -0.383431 }, { 0, -0.105296, -0.383431, 0.00200083 } };
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
                 Assert.That(km13should[i, 0], myIndefInt.Km_13[i, 0], 0.001);
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

            myIndefInt.CalculateStiffnesses(ref k, ref d, 0.0025 * 0.99, -0.0015 * 1.01, 0.0015 * 1.01, -0.0025 * 0.99);

            //Have these numbers from the Mathematica Code
            double[,] km11should = { { 189.751, 0, 0, 0 }, { 0, 520.341, 0, 0.0 }, { 0, 0, 333.54, 0.66708 }, { 0, 0, 0.66708, 0.00297997 } };
            double[,] km12should = { { -189.751, 0, 0, 0 }, { 0, -520.341, 0, 0 }, { 0, 0, -333.54, 0.66708 }, { 0, 0, -0.66708, -0.000311647 } };
            double[,] km21should = { { -189.751, 0, 0, 0 }, { 0, -520.341, 0, 0 }, { 0, 0, -333.54, -0.66708 }, { 0, 0, 0.66708, -0.000311647 } };
            double[,] km22should = { { 189.751, 0, 0, 0 }, { 0, 520.341, 0, 0 }, { 0, 0, 333.54, -0.66708 }, { 0, 0, -0.66708, 0.00297997 } };

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
            MatrixContinuumParameters mp = new MatrixContinuumParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers", "VonMises", "1000/");
            FToFMatrixContinuumElasticFiberSpring matSpring = new FToFMatrixContinuumElasticFiberSpring(dist, x12, mp, f1, f2, 0, 1);

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
            MatrixContinuumParameters mp = new MatrixContinuumParameters(3500, 0.3, 0.0, 1, 0.01, "MatrixContinuumElasticFibers", "VonMises", "1000/");
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
    }*/
    }
}
