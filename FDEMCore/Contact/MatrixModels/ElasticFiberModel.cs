using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RandomMath;


namespace FDEMCore.Contact.MatrixModels
{
    /// <summary>
    /// Purpose: Model of a fiber in a fiber/matrix/fiber assembly
    /// Created By: Scott_Stapleton
    /// Created On: 7/21/2022 2:26:37 PM
    /// Note: this runs on a 6 dof model, with thetaf1, uf2, vf2, thetaf2, Ug, um1, vm1, wm1, thetam1, um2, vm2, wm2, thetam2 = q.
    /// </summary>
    public class ElasticFiberModel : MaterialModel
    {

        #region Protected Members
        protected double E1, E2, nu12, nu23, G12;
        protected double C11;
        protected double C12;
        protected double C22;
        protected double C23;
        protected double C55;
        protected double lx;
        protected double Pi;
        protected bool isItFiber1;
        protected double S22ShapeFactor = 1.0;//
        protected double C22ShapeFactor = 1.0;//0.7;//2.75;
        protected double S33ShapeFactor = 1.0;
        protected double C23ShapeFactor = 1.0;//0.010;//0.1;
        protected double C12ShapeFactor = 1.0;//1.5;
        protected double SthetaSF = 1.0;
        protected double[,] Dmatrix;
        #endregion

        #region Public Members
        public new const string Name = "ElasticFiberModel";
        #endregion

        #region Constructor
        public ElasticFiberModel(double r, double d, double b, double[] zBoundsTopToBottom, bool isItFiber1, Fiber f)
            : base(isItFiber1?r:0.0, isItFiber1?0.0:r , d, b, zBoundsTopToBottom[0], zBoundsTopToBottom[1], new FailureTheories.NoFailure())
        {
            Pi = Math.PI;
            lx = b;
            this.isItFiber1 = isItFiber1;
            E1 = f.Modulus1;
            E2 = f.Modulus2;
            nu12 = f.Nu12;
            nu23 = f.Nu23;
            G12 = f.ShearModulus12;

            C11 = (Math.Pow(E1, 2.0) * (-1.0 + nu23)) / (2.0 * E2 * Math.Pow(nu12, 2.0) + E1 * (-1.0 + nu23));
            C22 = C22ShapeFactor * (E2 * (-E1 + E2 * Math.Pow(nu12, 2.0))) / ((2.0 * E2 * Math.Pow(nu12, 2.0) + E1 * (-1.0 + nu23)) * (1.0 + nu23));
            C12 = C12ShapeFactor * (E1 * E2 * nu12) / (E1 - 2.0 * E2 * Math.Pow(nu12, 2.0) - E1 * nu23);
            C23 = C23ShapeFactor  * - 1.0 * ((E2 * (E2 * Math.Pow(nu12, 2.0) + E1 * nu23)) / ((2.0 * E2 * Math.Pow(nu12, 2.0) + E1 * (-1.0 + nu23)) * (1.0 + nu23)));
            C55 = G12;

            Dmatrix = new double[6,6]{
                {C11, C12, C12, 0, 0, 0}, 
                {C12, C22, C23, 0, 0, 0}, 
                {C12, C23, C22, 0, 0, 0}, 
                {0, 0, 0, (C22 - C23)/2.0, 0, 0}, 
                {0, 0, 0, 0, C55, 0}, 
                {0, 0, 0, 0, 0, C55}
            };
        }

        #endregion

        #region Public Methods

        public override double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double[,] N = isItFiber1 ? CalculateNFiber1(x, y, z) : CalculateNFiber2(x, y, z);
            return MatrixMath.Multiply(N, q);
        }

        public override double[] CalculateStress(double x, double y, double z, double[] q, double[] stateVariables)
        {
            //This ignores failure!!!
            double[] strain = CalculateStrain(x, y, z, q, stateVariables);

            return MatrixMath.Multiply(Dmatrix, strain);
        }

        public override double[] CalculateStrain(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double[,] B = isItFiber1 ? CalculateBFiber1(x, y, z) : CalculateBFiber2(x, y, z);

            return MatrixMath.Multiply(B, q);
        }

        public override double[,] CalculateStiffness(double[] stateVariables)
        {
            if (isItFiber1)
            {
                return CalculateStiffnessFiber1();
            }
            return CalculateStiffnessFiber2();
        }

        public override bool IsThereFailure(double[] q, ref double[] stateVariables)
        {
            return false;
        }

        public override bool IsItTotallyBroken(double[] stateVariables)
        {
            return false;
        }

        public override void WriteFirstIterationOutput(StreamWriter dataWrite)
        {
            double r = isItFiber1 ? r1 : r2;
            dataWrite.Write(r + "," + E1 + "," + E2 + "," + nu12 + "," + nu23 + "," + G12);
        }
        public static ElasticFiberModel ReadFirstIterationOutput(string totalString, double d, double b, bool isItFiber1,  double charDist)
        {
            string[] allStrings = totalString.Split(',');

            double r = double.Parse(allStrings[0]);
            double E1 = double.Parse(allStrings[1]);
            double E2 = double.Parse(allStrings[2]);
            double nu12 = double.Parse(allStrings[3]);
            double nu23 = double.Parse(allStrings[4]);
            double G12 = double.Parse(allStrings[5]);

            Fiber f = new Fiber(new double[3], r, E1, E2, nu23,
                 nu12, G12, b, 1.0, 1.0, new CellBoundary(new double[3]));

            double[] zBoundsTopToBottom = MatrixFiberAssembly.DetermineIntegrationBounds(charDist, d, r, r, out double matrixArea);

            //Assumes that r1 = r2
            return new ElasticFiberModel(r, d, b, zBoundsTopToBottom, isItFiber1, f);

        }
        
        public override double[] CalculateIntegralOfStressOverVolume(double[] q, double[] stateVariables)
        {
            double[,] intBD_dV; 

            if (isItFiber1)
            {
                intBD_dV = CalculateIntegralOfOutOfPlaneStressOverVolumeFiber1();
            }
            else
            {
                intBD_dV = CalculateIntegralOfOutOfPlaneStressOverVolumeFiber2();
            }

            return MatrixMath.Multiply(intBD_dV , q);
        }
        #endregion

        #region Private Methods
        private double[,] CalculateStiffnessFiber1()
        {
            double[,] stiffness = {
                {SthetaSF * (1.0/8.0)*b*(C22 - C23)*Math.PI*r12, 0, 0, 0, 0, 0, 0, 0, SthetaSF * (-(1.0/8.0))*b*(C22 - C23)*Math.PI*r12, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, (C11*Math.PI*r12)/(2.0*b), 0, (C12*Math.PI*r1)/2.0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, (b*C55*Math.PI)/2.0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, (C12*Math.PI*r1)/2.0, 0, S22ShapeFactor * (b*C22*Math.PI)/2.0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, (1.0/4.0)*b*(C22 - C23)*Math.PI, 0, 0, 0, 0, 0},
                {SthetaSF * (-(1.0/8.0))*b*(C22 - C23)*Math.PI*r12, 0, 0, 0, 0, 0, 0, 0, SthetaSF * (1.0/8.0)*b*(C22 - C23)*Math.PI*r12, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
            };
            return stiffness;
        }

        private double[,] CalculateStiffnessFiber2()
        {

            double[,] stiffness = {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, (b*C55*Pi)/2.0, 0, 0, 0, 0, 0, 0, 0, (-(1.0/2.0))*b*C55*Pi, 0, 0, 0}, 
                {0, 0, S22ShapeFactor *(b*C22*Pi)/2.0, 0, (C12*Pi*r2)/2.0, 0, 0, 0, 0, 0, S22ShapeFactor *(-(1.0/2.0))*b*C22*Pi, 0, 0},
                {0, 0, 0, SthetaSF * (1.0/8.0)*b*(C22 - C23)*Pi*r22, 0, 0, 0, 0, 0, 0, 0, 0, SthetaSF * (-(1.0/8.0))*b*(C22 - C23)*Pi*r22}, 
                {0, 0, (C12*Pi*r2)/2.0, 0, (C11*Pi*r22)/(2.0*b), 0, 0, 0, 0, 0, (-(1.0/2.0))*C12*Pi*r2, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 
                {0, (-(1.0/2.0))*b*C55*Pi, 0, 0, 0, 0, 0, 0, 0, (b*C55*Pi)/2.0, 0, 0, 0},
                {0, 0, S22ShapeFactor *(-(1.0/2.0))*b*C22*Pi, 0,  (-(1.0/2.0))*C12*Pi*r2, 0, 0, 0, 0, 0, S22ShapeFactor *(b*C22*Pi)/2.0, 0, 0}, 
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (1.0/4.0)*b*(C22 - C23)*Pi, 0}, 
                {0, 0, 0,SthetaSF *  (-(1.0/8.0))*b*(C22 - C23)*Pi*r22, 0, 0, 0, 0, 0, 0, 0, 0, SthetaSF * (1.0/8.0)*b*(C22 - C23)*Pi*r22}
            };
            return stiffness;
        }

        private double[,] CalculateIntegralOfOutOfPlaneStressOverVolumeFiber1()
        {
            double[,] intBD_dV = {
                { 0.0,0.0,0.0,0.0,1.5707963267948966*C11*r12,0.0,1.5707963267948966*b*C12*r1,0.0,0.0,0.0,0.0,0.0,0},
               { 0.0,0.0,0.0,0.0,1.5707963267948966*C12*r12,0.0,1.5707963267948966*b*C22*r1,0.0,0.0,0.0,0.0,0.0,0},
                { 0.0,0.0,0.0,0.0,1.5707963267948966*C12*r12,0.0,1.5707963267948966*b*C23*r1,0.0,0.0,0.0,0.0,0.0,0},
                {0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.7853981633974483*b*(C22 - 1.0*C23)*r1,0.0,0.0,0.0,0.0,0},
                {0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0},
                {0.0,0.0,0.0,0.0,0.0,1.5707963267948966*b*C55*r1,0.0,0.0,0.0,0.0,0.0,0.0,0}
                };

            return intBD_dV;
        }

        private double[,] CalculateIntegralOfOutOfPlaneStressOverVolumeFiber2()
        {
            double[,] intBD_dV = {
                { 0.0,0.0,1.5707963267948966*b*C12*r2,0.0,1.5707963267948966*C11*r22,0.0,0.0,0.0,0.0,0.0,-1.5707963267948966*b*C12*r2,0.0,0},
                { 0.0,0.0,1.5707963267948966*b*C22*r2,0.0,1.5707963267948966*C12*r22,0.0,0.0,0.0,0.0,0.0,-1.5707963267948966*b*C22*r2,0.0,0},
                { 0.0,0.0,1.5707963267948966*b*C23*r2,0.0,1.5707963267948966*C12*r22,0.0,0.0,0.0,0.0,0.0,-1.5707963267948966*b*C23*r2,0.0,0},
                { 0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.7853981633974483*b*(-1.0*C22 + C23)*r2,0},
                { 0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0},
                { 0.0,1.5707963267948966*b*C55*r2,0.0,0.0,0.0,0.0,0.0,0.0,0.0,-1.5707963267948966*b*C55*r2,0.0,0.0,0}
                };
            return intBD_dV;
        }

        private double[,] CalculateBFiber1(double x, double y, double z)
        {
            double z2 = z * z;
            double y2 = y * y;
            double C1 = r1 * Math.Sqrt(y2 + z2);

            double[,] B = new double[6, 13]{{0, 0, 0, 0, 1.0/b, 0, 0, 0, 0, 0, 0, 0, 0},
                {(y*z)/C1, 0, 0, 0, 0, 0, 1.0/r1, 0, -((y*z)/C1), 0, 0, 0,  0},
                {-((y*z)/C1), 0, 0, 0, 0, 0, 0, 0, (y*z)/C1, 0, 0, 0, 0},
                {(-y2 + z2)/C1, 0, 0, 0, 0, 0, 0, 1.0/r1, (y2 - z2)/C1, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0,  0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 1.0/r1, 0, 0, 0, 0, 0, 0, 0}};
            return B;
        }

        private double[,] CalculateBFiber2(double x, double y, double z)
        {
            double z2 = z * z;
            double y2 = y * y;
            double C1 = r2 * Math.Sqrt(d2 - 2.0 * d * y + y2 + z2);

            double[,] B = new double[6, 13]{
                { 0, 0, 0, 0, 1.0 / b, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0,0,1.0/r2,-((d - y)*z)/C1,0,0,0,0,0,0,-(1.0 / r2),0,((d - y) * z) / C1},
                {0,0,0,((d - y)*z)/C1,0,0,0,0,0,0,0,0,-(((d - y)*z)/C1)},
                {0,0,0,-(d2/C1) + (2.0* d* y)/C1 + (-y2 + z2)/C1,0,0,0,0,0,0,0,-(1.0/r2), d2/C1 - (2.0* d* y)/C1 + y2/C1 - z2/C1},
                { 0,0,0,0,0,0,0,0,0,0,0,0,0},
                { 0,1.0 / r2,0,0,0,0,0,0,0,-(1.0 / r2),0,0,0} 
            };
            return B;
        }

        private double[,] CalculateNFiber1(double x, double y, double z)
        {
            double z2 = z * z;
            double y2 = y * y;
            double y2z2 = Math.Sqrt(y2 + z2);

            double[,] N = new double[3, 13]{{0, 0, 0, 0, x / b, y / r1, 0, 0, 0, 0, 0, 0, 0},
                { -z + (y2z2 * z) / r1, 0, 0, 0, 0, 0, y / r1, 0, -((y2z2 * z) / r1), 0, 0, 0, 0},
                { y - (y * y2z2) / r1, 0, 0, 0, 0, 0, 0, y / r1, (y * y2z2) / r1, 0, 0, 0, 0} };
            return N;
        }

        private double[,] CalculateNFiber2(double x, double y, double z)
        {
            double z2 = z * z;
            double y2 = y * y;
            double c1 = Math.Sqrt(d2 - 2.0 * d * y + y2 + z2); 

            double[,] N = new double[3, 13]{{ 0, 1 - d / r2 + y / r2, 0, 0, x / b, 0, 0, 0, 0, d / r2 - y / r2, 0, 0, 0 },
                    { 0, 0, 1 - d / r2 + y / r2, -z + (c1 * z) / r2, 0, 0, 0, 0, 0, 0, d / r2 - y / r2, 0, -((c1 * z) / r2)}, 
                    {0, 0, 0, -d + (c1 * (d - y)) / r2 + y, 0, 0, 0, 0, 0, 0, 0, d / r2 - y / r2, -((c1 * (d - y)) / r2) } };
            return N;
        }

        #endregion

        #region Static Methods

        #endregion
    }
}
