using System;
using NUnit.Framework;
using FDEMCore.Contact;
using FDEMCore.Contact.FailureTheories;
using RandomMath;
using FDEMCore;

namespace FDEMTests
{
    public class TestMatrixSpringModel1
    {
        /// <summary>
        /// Purpose:
        /// Created By: Scott_Stapleton
        /// Created On: 9/6/2022 4:18:44 PM
        /// </summary>

        [Test]
        public void TestMatrixStiffnessesTop()
        {
            double charDist = 0.00001;
            double rf = 0.003;
            double d = 0.007;
            int nIntPts = 200;

            MaxPrincStressZIntDNoDamage mps = new MaxPrincStressZIntDNoDamage(100.0);
            FDEMCore.Contact.MatrixModels.MatrixModel1 myMatrix = new FDEMCore.Contact.MatrixModels.MatrixModel1(1000, 0.3, rf, rf, d, 0.02, rf - charDist, 0, true, nIntPts, mps);
            double[,] KTop = myMatrix.CalculateStiffness(new double[nIntPts + 1]);

            double[,] KShould = new double[13, 13] {{1.5981E-19, 0, -7.0120E-18, -4.6755E-18, 3.0897E-19,
  0, -4.7428E-18, -4.3227E-18, -6.6679E-21, 0, 1.5468E-18,
   4.7880E-18, 1.1508E-17}, {0, 7.8182, 0, 0, 0, -7.5028, 0, 0,
  0, -6.8348, 0, 0, 0}, {-6.7555E-18, 0, 9.1822, 3.9346E-17,
  0.36597, 0, -10.153, -4.3434, -0.0091396,
  0, -4.1843, -0.52287, -0.015587}, {-8.0788E-18, 0,
  4.9249E-17, -1.9325E-16, -3.8156E-18, 0, -2.4211E-17,
  2.2597E-16, 6.2939E-18, 0,
  2.5822E-17, -1.0417E-16, -8.6313E-16}, {3.0897E-19, 0,
  0.36597, -3.8156E-18, 0.45743, 0, -1.3590, -0.99487, -0.00040575,
   0, 1.3590, -0.99487, 0.00040575}, {0, -7.5028, 0, 0, 0, 22.951, 0,
  0, 0, -8.6135, 0, 0, 0}, {-7.0914E-18,
  0, -10.153, -1.3769E-17, -1.3590, 0, 49.699, 4.7757, -0.025726,
  0, -35.361, 0.95514, 0.050453}, {-4.3227E-18, 0, -4.3434,
  4.1324E-18, -0.99487, 0, 4.7757, 53.581, 0.15762,
  0, -0.95514, -3.3994, 0.031441}, {-1.5950E-20, 0, -0.0091396,
  5.4488E-18, -0.00040575, 0, -0.025726, 0.15762, 0.00057426, 0,
  0.050453, -0.031441, 0.000073419}, {0, -6.8348, 0, 0, 0, -8.6135, 0,
   0, 0, 22.951, 0, 0, 0}, {3.9092E-18, 0, -4.1843, 3.6360E-17,
  1.3590, 0, -35.361, -0.95514, 0.050453, 0,
  49.699, -4.7757, -0.025726}, {6.0607E-18,
  0, -0.52287, -3.0020E-15, -0.99487, 0,
  0.95514, -3.3994, -0.031441, 0, -4.7757,
  53.581, -0.15762}, {4.0785E-18, 0, -0.015587, -8.5520E-16,
  0.00040575, 0, 0.050453, 0.031441, 0.000073419,
  0, -0.025726, -0.15762, 0.00057426}};

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                     Assert.That(KTop[i, j], Is.EqualTo(KShould[i, j]).Within(Math.Max(Math.Abs(KShould[i, j] * 0.01), 0.0000001)), string.Format("i={0}, j={1}", i, j));
                }
            }
        }

        [Test]
        public void TestMatrixStiffnessesTotal()
        {
            double charDist = 0.00001;
            double rf = 0.003;
            double d = 0.007;
            int nIntPts = 200;
            MaxPrincStressZIntDNoDamage mps = new MaxPrincStressZIntDNoDamage(100.0);

            //Do top and bottom by hand
            FDEMCore.Contact.MatrixModels.MatrixModel1 myMatrixTop = new FDEMCore.Contact.MatrixModels.MatrixModel1(1000, 0.3,  rf,rf, d, 0.02, rf - charDist, 0, true, nIntPts, mps);
            double[,] KTop = myMatrixTop.CalculateStiffness(new double[nIntPts + 1]);

            FDEMCore.Contact.MatrixModels.MatrixModel1 myMatrixBot = new FDEMCore.Contact.MatrixModels.MatrixModel1(1000, 0.3, rf, rf, d, 0.02, 0, charDist - rf, false, nIntPts, mps);
            double[,] KBot = myMatrixBot.CalculateStiffness(new double[nIntPts + 1]);

            double[,] KTotal = MatrixMath.Add(KBot, KTop);

            double[,] KShould = new double[13, 13] {{7.10651E-20, 0.0, -1.88062E-19,
  2.56611E-17, -1.31628E-21, 0.0,
  4.36605E-20, -4.50577E-18, -2.9035E-20,
  0.0, -5.28431E-19, -2.61096E-18, 3.10953E-17}, {0.0, 15.6363,
   0.0, 0.0, 0.0, -15.0056, 0.0, 0.0, 0.0, -13.6695, 0.0, 0.0,
  0.0}, {-1.71984E-19, 0.0, 18.3645, -3.17424E-19, 0.731942,
  0.0, -20.3065, 5.55112E-17, 3.31957E-18,
  0.0, -8.36866, -5.67324E-14, -7.47557E-17}, {-4.03094E-18,
  0.0, -4.5122E-19, -2.88979E-16, -3.45817E-19, 0.0,
  1.02813E-19, 4.09002E-16, 2.08475E-18,
  0.0, -9.81128E-20, -1.25553E-16,
  6.05169E-16}, {-1.31628E-21, 0.0, 0.731942, -3.45817E-19,
  0.914859, 0.0, -2.71806, 1.16573E-15, 2.00577E-18, 0.0, 2.71806,
   1.05471E-15, 2.22261E-18}, {0.0, -15.0056, 0.0, 0.0, 0.0,
  45.9022, 0.0, 0.0, 0.0, -17.227, 0.0, 0.0, 0.0}, {-6.35388E-21,
  0.0, -20.3065, -2.60207E-18, -2.71806, 0.0, 99.3979,
  0.0, -7.48642E-16, 0.0, -70.7228, -1.73906E-16,
  6.72205E-16}, {-4.50577E-18, 0.0, 5.55112E-17,
  5.04781E-16, 1.16573E-15, 0.0, 0.0, 107.162, 0.31524, 0.0,
  1.73906E-16, -6.79883, 0.0628829}, {-2.75481E-20,
  0.0, -3.78603E-16, -2.21352E-18, 2.00577E-18,
  0.0, -7.4604E-16, 0.31524, 0.00114853, 0.0,
  6.6093E-16, -0.0628829, 0.000146838}, {0.0, -13.6695, 0.0, 0.0,
  0.0, -17.227, 0.0, 0.0, 0.0, 45.9022, 0.0, 0.0, 0.0}, {-7.14742E-20,
  0.0, -8.36866, -8.85956E-19, 2.71806, 0.0, -70.7228,
  1.73906E-16, 6.66134E-16, 0.0, 99.3979, 0.0,
  7.4772E-16}, {-9.90309E-19, 0.0, -5.67324E-14,
  1.13957E-15, 1.05471E-15,
  0.0, -1.73906E-16, -6.79883, -0.0628829, 0.0, 0.0,
  107.162, -0.31524}, {6.46262E-18, 0.0, 7.52436E-17,
  6.42547E-16, 3.06287E-18, 0.0, 6.75675E-16, 0.0628829,
  0.000146838, 0.0, 7.50756E-16, -0.31524, 0.00114853}};

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                     Assert.That(KTotal[i, j], Is.EqualTo(KShould[i, j]).Within(Math.Max(Math.Abs(KShould[i, j] * 0.005), 0.0000001)), string.Format("i={0}, j={1}", i, j));
                }
            }
            
        }

        [Test]
        public void TestMatrixStiffnessesTotal_FMAssemblyWithMathematica()
        {
            
            double rf = 0.003;
            double charDist = 0.00001/rf;
            double d = 0.007;
            int nIntPts = 200;
            double lx = 0.02;

            MatrixAssemblyParameters matrixParameters = new(1000, 0.3, 1.0, 1, charDist,
                "ElasticFibersElasticMatrix", " ", "DamageFracturEnergyAndStrength", String.Format("1000/ 1000/ {0}", nIntPts));
            FiberParameters fp = new(rf, 6.811E-8, lx, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            Fiber f1 = new(new double[3], fp, new CellBoundary(new double[3]));
            Fiber f2 = new(new double[3], fp, new CellBoundary(new double[3]));


            //Do top and bottom by hand
            FDEMCore.Contact.MatrixModels.MatrixFiberAssembly mfAss = new( d, lx, matrixParameters, f1, f2, out double[] initialStateVariables);

            double[,] Km = mfAss.Km;

            double[,] KShould = new double[13, 13] {{7.10651E-20, 0.0, -1.88062E-19,
  2.56611E-17, -1.31628E-21, 0.0,
  4.36605E-20, -4.50577E-18, -2.9035E-20,
  0.0, -5.28431E-19, -2.61096E-18, 3.10953E-17}, {0.0, 15.6363,
   0.0, 0.0, 0.0, -15.0056, 0.0, 0.0, 0.0, -13.6695, 0.0, 0.0,
  0.0}, {-1.71984E-19, 0.0, 18.3645, -3.17424E-19, 0.731942,
  0.0, -20.3065, 5.55112E-17, 3.31957E-18,
  0.0, -8.36866, -5.67324E-14, -7.47557E-17}, {-4.03094E-18,
  0.0, -4.5122E-19, -2.88979E-16, -3.45817E-19, 0.0,
  1.02813E-19, 4.09002E-16, 2.08475E-18,
  0.0, -9.81128E-20, -1.25553E-16,
  6.05169E-16}, {-1.31628E-21, 0.0, 0.731942, -3.45817E-19,
  0.914859, 0.0, -2.71806, 1.16573E-15, 2.00577E-18, 0.0, 2.71806,
   1.05471E-15, 2.22261E-18}, {0.0, -15.0056, 0.0, 0.0, 0.0,
  45.9022, 0.0, 0.0, 0.0, -17.227, 0.0, 0.0, 0.0}, {-6.35388E-21,
  0.0, -20.3065, -2.60207E-18, -2.71806, 0.0, 99.3979,
  0.0, -7.48642E-16, 0.0, -70.7228, -1.73906E-16,
  6.72205E-16}, {-4.50577E-18, 0.0, 5.55112E-17,
  5.04781E-16, 1.16573E-15, 0.0, 0.0, 107.162, 0.31524, 0.0,
  1.73906E-16, -6.79883, 0.0628829}, {-2.75481E-20,
  0.0, -3.78603E-16, -2.21352E-18, 2.00577E-18,
  0.0, -7.4604E-16, 0.31524, 0.00114853, 0.0,
  6.6093E-16, -0.0628829, 0.000146838}, {0.0, -13.6695, 0.0, 0.0,
  0.0, -17.227, 0.0, 0.0, 0.0, 45.9022, 0.0, 0.0, 0.0}, {-7.14742E-20,
  0.0, -8.36866, -8.85956E-19, 2.71806, 0.0, -70.7228,
  1.73906E-16, 6.66134E-16, 0.0, 99.3979, 0.0,
  7.4772E-16}, {-9.90309E-19, 0.0, -5.67324E-14,
  1.13957E-15, 1.05471E-15,
  0.0, -1.73906E-16, -6.79883, -0.0628829, 0.0, 0.0,
  107.162, -0.31524}, {6.46262E-18, 0.0, 7.52436E-17,
  6.42547E-16, 3.06287E-18, 0.0, 6.75675E-16, 0.0628829,
  0.000146838, 0.0, 7.50756E-16, -0.31524, 0.00114853}};

            for (int i = 0; i < Km.GetLength(0); i++)
            {
                for (int j = 0; j < Km.GetLength(1); j++)
                {
                     Assert.That(Km[i, j], Is.EqualTo(KShould[i, j]).Within(Math.Max(Math.Abs(KShould[i, j] * 0.1), 0.0000001)), string.Format("km: i={0}, j={1}", i, j));
                }
            }
            
        }

        [Test]
        public void TestMatrixStiffnessesTotal_ThroughFMAssembly()
        {
            double rf = 0.003;
            double charDist = 0.00001 / rf;
            double d = 0.007;
            double lx = 0.02;
            int nIntPts = 200;

            MatrixAssemblyParameters matrixParameters = new(1000, 0.3, 1.0, 1, charDist,
                "ElasticFibersElasticMatrix", " ", "DamageFracturEnergyAndStrength", String.Format("1000/ 1000/ {0}", nIntPts));
            FiberParameters fp = new(rf, 6.811E-8, lx, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            Fiber f1 = new(new double[3], fp, new CellBoundary(new double[3]));
            Fiber f2 = new(new double[3], fp, new CellBoundary(new double[3]));

            //Do top and bottom by hand
            FDEMCore.Contact.MatrixModels.MatrixFiberAssembly mfAss = new(d, lx, matrixParameters, f1, f2, out double[] initialStateVariables);



            double[,] KTotal = mfAss.Kmm;

            //Needs to be all added together, and this'll be index numbers 6-13
            double[,] KShould = new double[8, 8]
                {{290.946, 0.0, 0.0, 0.0, -17.227, 0.0, 0.0, 0.0}, {0.0, 614.315,
  0.0, -7.48642E-16, 0.0, -70.7228, -1.73906E-16,
  6.72205E-16}, {0.0, 0.0, 291.24, 0.31524, 0.0,
  1.73906E-16, -6.79883, 0.0628829}, {0.0, -7.4604E-16, 0.31524,
  0.00197688, 0.0, 6.6093E-16, -0.0628829, 0.000146838}, {-17.227,
  0.0, 0.0, 0.0, 290.946, 0.0, 0.0, 0.0}, {0.0, -70.7228, 1.73906E-16,
  6.66134E-16, 0.0, 614.315, 0.0,
  7.4772E-16}, {0.0, -1.73906E-16, -6.79883, -0.0628829, 0.0, 0.0,
  291.24, -0.31524}, {0.0, 6.75675E-16, 0.0628829, 0.000146838, 0.0,
  7.50756E-16, -0.31524, 0.00197688}};



            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                     Assert.That(KTotal[i, j], Is.EqualTo(KShould[i, j]).Within(Math.Max(Math.Abs(KShould[i, j] * 0.1), 0.0000001)), string.Format("kmm: i={0}, j={1}", i, j));
                }
            } 
            
            /*
            double[,] KEq = mfAss.Keq;

            //Needs to be all added together, and this'll be index numbers 6-13
            double[,] KeqShould = new double[5, 5]
                {{22.9029163793269, 0,  0,  0,  0 },
{ 0,  81.5795593289474,  -2.43552032626854E-16,  -9.54174855509273E-20,  9.84196891673278 },
{ 0,  -3.42516769807099E-16,  19.3767917876372,  -0.0678187712567379, -9.35805070542525E-18 },
{ 0,  -1.46761255490397E-19,  -0.0678187712567405, 0.000464861701575537,  3.35413661709742E-20 },
{ 0,  9.84196891673278, -1.86590955838692E-17,  8.51268564788726E-21, 342.565653366718}
};



            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                     Assert.That(KeqShould[i, j], KEq[i, j], Math.Max(Math.Abs(KeqShould[i, j] * 0.1), 0.0000001), string.Format("kmm: i={0}, j={1}", i, j));
                }
            }
            */
        }

        [Test]
        public void TestFiberStiffness1()
        {
            
            double charDist = 0.00001;
            double rf = 0.003;
            double d = 0.007;
            double lx = 0.02;
            int nIntPts = 10;

            FiberParameters fp = new(rf, 6.811E-8, lx, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            Fiber f1 = new(new double[3], fp, new CellBoundary(new double[3]));
            double[] bounds = new double[4] { rf - charDist, 0, 0, charDist - rf };
            
            FDEMCore.Contact.MatrixModels.ElasticFiberModel f1m = new(rf, d, lx, bounds, true, f1);
            double[,] Kf1 = f1m.CalculateStiffness(new double[nIntPts + 1]);

            double[,] KShould = new double[13, 13] {
                {0.00082835, 0, 0, 0, 0, 0, 0, 0, -0.00082835, 0, 0, 0, 0}, 
                {0, 0, 0,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 171.507, 0, 24.8129, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 245.044, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 24.8129, 0, 514.917, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 184.078, 0, 0, 0, 0, 0}, 
                {-0.00082835, 0, 0, 0, 0, 0, 0, 0, 0.00082835, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}};

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                     Assert.That(Kf1[i, j], Is.EqualTo(KShould[i, j]).Within(Math.Max(Math.Abs(KShould[i, j] * 0.01), 0.0000001)), string.Format("i={0}, j={1}", i, j));
                }
            }
            
        }

        [Test]
        public void TestFiberStiffness2()
        {
            
            double charDist = 0.00001;
            double rf = 0.003;
            double d = 0.007;
            double lx = 0.02;
            int nIntPts = 10;

            FiberParameters fp = new(rf, 6.811E-8, lx, 240000, 15000, 0.25, 0.28, 7800, 1.0);
            Fiber f1 = new(new double[3], fp, new CellBoundary(new double[3]));
            Fiber f2 = new(new double[3], fp, new CellBoundary(new double[3]));

            double[] bounds = new double[4] { rf - charDist, 0, 0, charDist - rf };
            FDEMCore.Contact.MatrixModels.ElasticFiberModel f1m = new(rf, d, lx, bounds, false, f1);

            double[,] Kf1 = f1m.CalculateStiffness(new double[nIntPts + 1]);


            double[,] KShould = new double[13, 13] {{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, {0, 245.04, 0, 0, 0, 0, 0,
  0, 0, -245.04, 0, 0, 0}, {0, 0, 514.92, 0, 24.813, 0, 0, 0, 0,
  0, -514.92, 0, 0}, {0, 0, 0, 0.00082835, 0, 0, 0, 0, 0, 0, 0,
  1.9882E-12, -0.00082835}, {0, 0, 24.813, 0, 171.51, 0, 0, 0, 0,
  0, -24.813, 0, 0}, {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, {0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0}, {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, {0, -245.04, 0, 0,
  0, 0, 0, 0, 0, 245.04, 0, 0, 0}, {0, 0, -514.92, 0, -24.813, 0, 0,
  0, 0, 0, 514.92, 0, 0}, {0, 0, 0, 1.9882E-12, 0, 0, 0, 0, 0, 0,
  0, 184.08, -1.9882E-12}, {0, 0, 0, -0.00082835, 0, 0, 0, 0, 0, 0,
   0, -1.9882E-12, 0.00082835}};

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                     Assert.That(Kf1[i, j], Is.EqualTo(KShould[i, j]).Within(Math.Max(Math.Abs(KShould[i, j] * 0.01), 0.0000001)), string.Format("i={0}, j={1}", i, j));
                }
            }
            
        }
    }
}
