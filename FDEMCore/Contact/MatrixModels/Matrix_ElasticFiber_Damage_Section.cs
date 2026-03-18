using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMath;

namespace FDEMCore.Contact
{
    public class Matrix_ElasticFiber_Damage_Section
    {
        #region Protected Members
        //These are all quantities per integration point
        protected double[] zIntPts;
        protected double[] damage;
        protected double[] yLeft_IntPts;
        protected double[] yRight_IntPts;
        protected double[][,] BxyzLeft;
        protected double[][,] BxyzRight;
        protected double[][] km11Coeff;
        protected double[][] km22Coeff;
        protected double[][] km24Coeff;
        protected double[][] km44Coeff;
        protected double[][] km48Coeff;
        protected double[][] km33Coeff;
        protected double[][] km34Coeff;
        protected double[][] km72Coeff;
        protected double[][] km73Coeff;

        //And the geometric quantities

        protected double r, r2, d, b, E0, G0;
        protected double strength0, fractureEnergy0, damageAccelerationCoefficient;

        //And these are the matrix entries that fill the block stiffness matrices
        protected double km11, km22, km24, km44, km48, km33, km34, km72, km73;
        #endregion

        #region Public Members
        public double[] Damage
        {
            get { return damage; }                 
        }

        #endregion

        #region Constructor
        public Matrix_ElasticFiber_Damage_Section(int nIntPts, double zAbove, double zBelow, bool isTop, double r, double d, double b, double E, double G, double strength, 
            double fractureEnergy, double damageAccelerationCoefficient)
        {
            this.r = r;
            this.d = d;
            this.b = b;
            this.E0 = E;
            this.G0 = G;
            this.r2 = r * r;

            strength0 = strength;
            fractureEnergy0 = fractureEnergy;
            this.damageAccelerationCoefficient = damageAccelerationCoefficient;


            //Initiate all quantities to be saved
            zIntPts = new double[nIntPts + 1];
            yLeft_IntPts = new double[nIntPts + 1];
            yRight_IntPts = new double[nIntPts + 1];
            BxyzLeft = new double[nIntPts + 1][,];
            BxyzRight = new double[nIntPts + 1][,];
            km11Coeff = new double[nIntPts + 1][];
            km22Coeff = new double[nIntPts + 1][];
            km24Coeff = new double[nIntPts + 1][];
            km44Coeff = new double[nIntPts + 1][];
            km48Coeff = new double[nIntPts + 1][];
            km33Coeff = new double[nIntPts + 1][];
            km34Coeff = new double[nIntPts + 1][];
            km72Coeff = new double[nIntPts + 1][];
            km73Coeff = new double[nIntPts + 1][];
            damage = new double[nIntPts + 1];


            //Set all of the integration points, B matrix, and K_coeff matrices 
            for (int i = 0; i < nIntPts + 1; i++)
            {
                //Set z
                zIntPts[i] = QuadraticZ(i, zBelow, zAbove, nIntPts, isTop);

                //Set y/left and y/right
                yLeft_IntPts[i] = CalculateYAtFiber1(r, zIntPts[i]);
                yRight_IntPts[i] = CalculateYAtFiber2(r, d, zIntPts[i]);

                //Set Bxyz
                BxyzLeft[i] = BMatrixForStrain(0, yLeft_IntPts[i], zIntPts[i], yLeft_IntPts[i], yRight_IntPts[i]);
                BxyzRight[i] = BMatrixForStrain(0, yRight_IntPts[i], zIntPts[i], yLeft_IntPts[i], yRight_IntPts[i]);

                //Set kmijCoeff
                EvaluateIntegralForMatrixAtPointWithoutD(zIntPts[i], yLeft_IntPts[i], yRight_IntPts[i], out km11Coeff[i], out km22Coeff[i], out km24Coeff[i], out km44Coeff[i],
            out km48Coeff[i], out km33Coeff[i], out km34Coeff[i], out km72Coeff[i], out km73Coeff[i]);

            }
        }
        #endregion

        #region public Methods
        /// <summary>
        /// First, checks for damage at the fiber/matrix interfaces.  If there is damage, update the damage variable and re-calculate the stiffness
        /// </summary>
        /// <param name="qm">Matrix Displacements</param>
        /// <param name="km_11">Block Matrix of the stiffness</param>
        /// <param name="km_12">Block Matrix of the stiffness</param>
        /// <param name="km_21">Block Matrix of the stiffness</param>
        /// <param name="km_22">Block Matrix of the stiffness</param>
        /// <param name="km_13">Block Matrix of the stiffness</param>
        /// <param name="km_23">Block Matrix of the stiffness</param>
        public void UpdateStiffness(out double[,] km_11, out double[,] km_12, out double[,] km_21, out double[,] km_22, out double[,] km_13, out double[,] km_23)
        {
            //Calculate inside the integral for each component of the stiffness matrix
            CalculateComponentsOfStiffness();

            //Assemble and return stiffness block components of the section
            AssembleStiffnessToBlockMatrices(km11, km22, km24, km44, km48, km33, km34, km72, km73,
            out km_11, out km_12, out km_21, out km_22, out km_13, out km_23);
        }

        public void UpdateDamage(double[] qm, ref bool isChanged)
        {
            //Check Stress on both sides
            isChanged = false;

            for (int i = 0; i < damage.Length; i++)
            {
                UpdateDamageAtAPoint(yLeft_IntPts[i], yRight_IntPts[i], ref damage[i], qm, BxyzLeft[i], ref isChanged);
                UpdateDamageAtAPoint(yLeft_IntPts[i], yRight_IntPts[i], ref damage[i], qm, BxyzRight[i], ref isChanged);
            }
        }
        #endregion

        #region Initiation Methods

        ///This samples the z space with a quadratic.  It is only public so that it can be easily tested.... This is from QuadraticSampling.nb
        public static double QuadraticZ(int i, double zLower, double zUpper, int nIntPts, bool isTop)
        {
            //This is quadratic with the slope 0 at the upper bound
            //The higher the slope, the more biased the distribution
            double z;
            if (isTop)
            {
                z = zLower + 2.0 / nIntPts * (zUpper - zLower) * i - (zUpper - zLower) / Math.Pow(nIntPts, 2.0) * Math.Pow(i, 2.0);
            }
            else
            {
                z = zLower + (Math.Pow(i, 2.0) * (-zLower + zUpper)) / Math.Pow(nIntPts, 2.0);
            }
            return z;
        }

        protected double[,] BMatrixForStrain(double x, double y, double z, double yL, double yR)
        {
            //Initial values
            double l = yR - yL;
            double l2 = l * l;
            double z2 = z * z;

            double yLp = -(z / Math.Sqrt(r2 - z2));
            double yRp = (z / Math.Sqrt(r2 - z2));
            double lp = yRp - yLp;

            double yL2 = yL * yL;
            double yR2 = yR * yR;

            //If the fibers are overlapped at z
            if (yL > yR)
            {
                throw new ArgumentException("Integration Point occurs where fibers overlap");
            }

            double[,] Bxyz = new double[6, 9]{
                { 0,0,0,0,0,0,0,0,1.0/b },
                { 0,-1.0 / l,0,(z / l),0,(1.0 / l),0,-z / l,0},
                { 0,0,(yLp * (-y + yR) + (y - yL) * yRp) / l2,(-(y * yLp * yR) + yLp * yR2 + (y - yL) * yL * yRp) / l2,0,0,(yLp * (y - yR) + (-y + yL) * yRp) / l2,(yLp * (d - yR) * (-y + yR) + (d - yL) * (y - yL) * yRp) / l2,0},
                { 0,(yLp*(-y + yR) + (y - yL)*yRp)/l2, -1.0/l, (yL2 - yR2 - lp*y*z + yR*(y - yLp*z) + yL*(-y + yRp*z))/l2,0,(yLp*(y - yR) + (-y + yL)*yRp)/l2, (1.0/l), (-yL2 + yR2 + lp*y*z - yR*(d + y - yLp*z) + yL*(d + y - yRp*z))/l2,0 },
                { (yLp * (-y + yR) + (y - yL) * yRp) / l2,0,0,0,(yLp * (y - yR) + (-y + yL) * yRp) / l2,0,0,0,0},
               { -1.0 / l,0,0,0,(1.0 / l),0,0,0,0}
            };

            
            return Bxyz;
        }

        protected void EvaluateIntegralForMatrixAtPointWithoutD(double z, double yL, double yR, out double[] km11Coeff, out double[] km22Coeff, out double[] km24Coeff, out double[] km44Coeff,
            out double[] km48Coeff, out double[] km33Coeff, out double[] km34Coeff, out double[] km72Coeff, out double[] km73Coeff)
        {
            if (z > r)
            {
                throw new ArgumentException("z point above or below the fiber radius");
            }
            //Initial values
            double l = yR - yL;
            double z2 = z * z;

            double yLp = -(z / Math.Sqrt(r2 - z2));
            double yRp = (z / Math.Sqrt(r2 - z2));
            double lp = yRp - yLp;

            double yLp2 = yLp * yLp;
            double yRp2 = yRp * yRp;
            double yL2 = yL * yL;
            double yR2 = yR * yR;

            //If the fibers are overlapped at z
            if (yL > yR)
            {
                throw new ArgumentException("Integration Point occurs where fibers overlap");
            }

            km11Coeff = new double[2] { 0, (b * (3.0 + yLp2 + yLp * yRp + yRp2)) / (3.0 * l) };

            km22Coeff = new double[2] { (b / l), (b * (yLp2 + yLp * yRp + yRp2)) / (3.0 * l) };

            km24Coeff = new double[2] { -(b * z) / l, -(b * (2.0 * yRp2 * z + yRp * (2.0 * yL + yR + 2.0 * yLp * z) + yLp * (yL + 2.0 * (yR + yLp * z)))) / (6.0 * l) };

            km44Coeff = new double[2] { (b*yL*yLp*yR*yRp)/(3.0*l) + (b*yL2*yRp2)/(3.0*l) + (b*(yLp2*yR2 + 3*z2))/(3.0*l),
                (b*yRp*z*(2.0*yL + yR + yLp*z))/(3.0*l) + (b*yRp2*z2)/(3.0*l) + (b*(yL2 + yR2 + 2.0*yLp*yR*z + yL*(yR + yLp*z) + yLp2*z2))/(3.0*l)};

            km48Coeff = new double[2] { -(b*(2.0*yLp2*yR*(-d + yR) - yLp*(yL*(d - 2.0*yR) + d*yR)*yRp + 2.0*yL*(-d + yL)*yRp2))/(6.0*l) - (b*z2)/l,
                -(b*(2.0*yL2 - (3.0*d - 2.0*yR - 2.0*yLp*z)*(yR + yLp*z) + yRp*z*(-3.0*d + 2.0*yR + 2.0*yLp*z) + yL*(-3.0*d + 2.0*yR + 2.0*yLp*z + 4.0*yRp*z) + 2.0*yRp2*z2))/(6.0*l)};

            km33Coeff = new double[2] { (b * (yLp2 + yLp * yRp + yRp2)) / (3.0 * l), (b / l) };

            km34Coeff = new double[2] { (b * (yLp * yR * (2.0 * yLp + yRp) + yL * yRp * (yLp + 2.0 * yRp))) / (6.0 * l), (b * (yL + yR + (yLp + yRp) * z)) / (2.0 * l) };

            km72Coeff = new double[2] { 0, (b * (yLp + yRp)) / (2.0 * l) };

            km73Coeff = new double[2] { -(b * (yLp2 + yLp * yRp + yRp2)) / (3.0 * l), -b / l };

        }


        #endregion

        #region Damage Methods
        protected double[] CalculateStrain(double[,] Bxyz, double[] qm)
        {
            return MatrixMath.Multiply(Bxyz, qm);
        }

        protected void UpdateDamageAtAPoint(double yL, double yR, ref double D, double[] qm, double[,] Bxyz, ref bool isChanged)
        {
            //Don't do any of this garbage if it's already dead!
            if (D == 1.0)
            {
                return;
            }

            //OK now we can get started...
            double Dinitial = D;
            double length = yR - yL;
            double tempStrength0 = strength0;

            // check if the softening slope is negative.If it is, scale the strength
            if (length > (2.0 * E0 * fractureEnergy0 / Math.Pow(strength0, 2.0)))
            {
                tempStrength0 = Math.Sqrt(2.0 * fractureEnergy0 * E0 / length);
            }

            //First, Calculate initial Values
            double criticalStrain0 = tempStrength0 / E0;
            double maximumStrain = 2.0 * fractureEnergy0 / (length * tempStrength0);

            //First, update the strength, critical strain, and energy
            double E = E0 * (1.0 - D);
            double criticalStrain = maximumStrain / (1.0 - E / tempStrength0 * (criticalStrain0 - maximumStrain));

            // find the maximum principle strain
            double[] s = CalculateStrain(Bxyz, qm);

            //put strain in tensor form: Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY:
            double[,] strainTensor = new double[3, 3] { { s[0], 0.5*s[5], 0.5 * s[4] }, { 0.5 * s[5], s[1], 0.5 * s[3] }, { 0.5 * s[4], 0.5 * s[3], s[2] } };

            //find max principle (assumes max is in the first spot
            double[] princStrain = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(strainTensor);
            double maxPrincStrain = princStrain[0];


            // see if it has incurred more damage
            if (maxPrincStrain <= criticalStrain)
            {
                // no "yielding": don't update D
            }
            else if (maxPrincStrain < maximumStrain)
            {
                // "yielding": new D
                double DTemp = 1.0 - (1.0 - maximumStrain / maxPrincStrain) / (criticalStrain0 - maximumStrain) * criticalStrain0;

                //Grow the damage if there is new damage.  Also, accelerate the crack by the damage coefficient
                D = D > DTemp ? D : (DTemp - D) * damageAccelerationCoefficient + D;
                D = D > 1.0 ? 1.0 : D;
            }
            else
            {
                // it's dead!
                D = 1.0;
            }
            //Throw a flag if D has been changed.
            if (!Double.Equals(D, Dinitial))
            {
                isChanged = true;
            }
        }

        #endregion

        #region Stiffness Methods
        protected void AssembleStiffnessToBlockMatrices(double km11, double km22, double km24, double km44, double km48, double km33, double km34, double km72, double km73,
            out double[,] km_11, out double[,] km_12, out double[,] km_21, out double[,] km_22, out double[,] km_13, out double[,] km_23)
        {

            //These are the summed integrals
            km_11 = new double[,] { { km11, 0.0, 0.0, 0.0 }, { 0.0, km22, 0.0, km24 }, { 0.0, 0.0, km33, km34 }, { 0.0, km24, km34, km44 } };

            km_12 = new double[,] { { -1.0 * km11, 0.0, 0.0, 0.0 }, { 0.0, -1.0 * km22, 0.0, -1.0 * km24 }, { 0.0, 0.0, -1.0 * km33, km34 }, { 0.0, -1.0 * km24, -1.0 * km34, km48 } };

            km_21 = new double[,] { { -1.0 * km11, 0.0, 0.0, 0.0 }, { 0.0, -1.0 * km22, 0.0, -1.0 * km24 }, { 0.0, 0.0, -1.0 * km33, -1.0 * km34 }, { 0.0, -1.0 * km24, km34, km48 } };

            km_22 = new double[,] { { km11, 0.0, 0.0, 0.0 }, { 0.0, km22, 0.0, km24 }, { 0.0, 0.0, km33, -1.0 * km34 }, { 0.0, km24, -1.0 * km34, km44 } };

            km_13 = new double[,] { { 0.0 }, { km72 }, { 0.0 }, { km73 } };
            km_23 = new double[,] { { 0.0 }, { -1.0 * km72 }, { 0.0 }, { -1.0 * km73 } };
        }

        protected void CalculateComponentsOfStiffness()
        {
            //Calculate inside the integral for each component of the stiffness matrix
            km11 = IntegralOfMatrixComponent(km11Coeff);
            km22 = IntegralOfMatrixComponent(km22Coeff);
            km24 = IntegralOfMatrixComponent(km24Coeff);
            km44 = IntegralOfMatrixComponent(km44Coeff);
            km48 = IntegralOfMatrixComponent(km48Coeff);
            km33 = IntegralOfMatrixComponent(km33Coeff);
            km34 = IntegralOfMatrixComponent(km34Coeff);
            km72 = IntegralOfMatrixComponent(km72Coeff);
            km73 = IntegralOfMatrixComponent(km73Coeff);
        }
        
        protected double IntegralOfMatrixComponent(double[][] kCoeff)
        {
            double[] kIntAtPts = new double[zIntPts.Length];
            double sum = 0.0;
            for (int i = 0; i < zIntPts.Length; i++)
            {
                double E = E0 * (1.0 - damage[i]);
                kIntAtPts[i] = EvaluateIntegralForMatrix(E, E / E0 * G0, kCoeff[i]);
                if (i != 0)
                {
                    sum += 0.5 * (kIntAtPts[i] + kIntAtPts[i - 1]) * (zIntPts[i] - zIntPts[i - 1]);
                }
            }
            return sum;
        }

        protected double EvaluateIntegralForMatrix(double E, double G, double[] kCoeff)
        {
            double[] stiffnessVector = new double[2] { E, G };

            double k = VectorMath.Dot(kCoeff, stiffnessVector);
            return k;
        }
        #endregion

        #region Static methods
        public static double CalculateYAtFiber1(double r, double z)
        {
            return Math.Sqrt(r * r - z * z); ;
        }

        public static double CalculateYAtFiber2(double r, double d, double z)
        {
            return (d - Math.Sqrt(r * r - z * z));
        }

        #endregion
    }
}
