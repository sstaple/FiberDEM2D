using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using RandomMath;
using FDEMCore.Contact.FailureTheories;
using System.IO;

namespace FDEMCore.Contact.MatrixModels
{
    public class MatrixModel0 : ClosedFormMatrixModel
    {
        /// <summary>
        /// Purpose: This model is with rigid fibers and an elastic matrix.  
        /// Created By: Scott_Stapleton
        /// Created On: 7/21/2022 3:18:49 PM
        /// </summary>
        #region Protected Members

        protected Complex c1, c2;

        #endregion

        #region Public Members
        public new const string Name = "MatrixModel0";
        #endregion

        #region Constructor
        public MatrixModel0(double r, double d, double b, double Ep, double nu, double zTop, double zBottom, IFailureCriteria failureCriteria)
            : base(r, d, b, Ep, nu, zTop, zBottom, failureCriteria)
        {
            D = new double[6, 6] {
                {Ep * (1 - nu), Ep * nu, Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * (1 - nu), Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * nu, Ep * (1 - nu), 0, 0, 0},
                {0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0, 0},
                {0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0},
                {0, 0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0}
            };

            c1 = MyComplex.TakeSquareRoot(d2 - 4 * r2);
            c2 = MyComplex.TakeSquareRoot(-d2 + 4 * r2);

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculate the strain at a given location for a given set of matrix degrees of freedom
        /// </summary>
        /// <returns>{Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY}</returns>
        public override double[] CalculateStrain(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double z2 = z * z;

            double srmz = Math.Sqrt(r2 - z2);
            double c1 = d - 2 * srmz;
            double c12 = c1 * c1;

            double[,] Bxyz = { {0,0,0,0,1/b },
                { 0,1 / c1,0,z / (-d + 2 * srmz),0},
                { 0,0,((d - 2 * y) * z) / (c12 * srmz),(z * (2 * r2 + d * (-2 * srmz + y) - 2 * z2)) / (c12 * srmz),0},
                { 0,((d - 2 * y) * z) / (c12 * srmz),1 / c1,(2 * r2 * y - d * (srmz * y + z2)) / (c12 * srmz),0},
                { ((d - 2 * y) * z) / (c12 * srmz),0,0,0,0},
                { 1/c1,0,0,0,0}
            };

            return MatrixMath.Multiply(Bxyz, q);
        }

        public override double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables)
        {
            
            return new double[3];
        }
        public override void WriteFirstIterationOutput( StreamWriter dataWrite)
        {
            dataWrite.Write("," +  Ep + "," + nu + "," + failureCriteria.GetType().Name);
            dataWrite.Write(",");
            failureCriteria.WriteOutput(dataWrite);
        }

        public static MatrixModel0[] ReadFirstIterationOutput(string totalString, double r, double d, double b, double[] zBoundsTopToBottom)
        {
            string[] splitString = totalString.Split(',');

            double Ep = Convert.ToDouble(splitString[0]);
            double nu = Convert.ToDouble(splitString[1]);
            string failureTypeNam = splitString[2];
            string failureConst = splitString[3];

            IFailureCriteria failureCrit= FailureTheories.CreateFailureCriteria.CreateFailureCriteriaFromInput(failureTypeNam, failureConst);


            MatrixModel0 topMatrix = new MatrixModel0(r, d, b, Ep, nu, zBoundsTopToBottom[0], zBoundsTopToBottom[1], failureCrit);
            MatrixModel0 botMatrix = new MatrixModel0(r, d, b, Ep, nu, zBoundsTopToBottom[2], zBoundsTopToBottom[3], failureCrit);
            return new MatrixModel0[2] { topMatrix, botMatrix };

        }

        #endregion

        #region Private Methods
        protected override Complex[,] EvalIndefiniteIntegral(double z)
        {

            double z2 = z * z;
            //establish all of the constants that are particular to a z
            double rmz = Math.Sqrt(r2 - z2);
            double c3 = d2 - 4 * r2 + 4 * z2;

            Complex a1 = Complex.Atan(z / rmz);
            Complex a2 = Complex.Atan((d * z) / (c1 * rmz));
            Complex a3 = Complex.Atan((2 * z) / c1);
            Complex a4 = Complex.Atan(z / r1);
            Complex a5 = Complex.Atan((rmz * z) / (-r2 + z2));


            double l3 = Math.Log(c3);
            Complex l4 = Complex.Log(c2 - 2.0 * z);
            Complex l5 = Complex.Log(c2 + 2 * z);
            Complex l9 = Complex.Log(-2 * r2 - d * rmz + c2 * z);
            Complex l10 = Complex.Log(2 * r2 + d * rmz + c2 * z);
            Complex l11 = Complex.Log(-r2 + z2);
            Complex l12 = Complex.Log(-r1 + z);
            Complex l13 = Complex.Log(r1 + z);


            Complex[,] kComp = new Complex[5,5]{
                {(b*Ep*(-1 + 2*nu)*(a1*c1*d - a3*d2 - a4*c1*r1 - 2*a3*r12 - a2*(d2 + 2*r12)))/(6.0*c1*d),0,0,0,0},
                
                {0,(b*Ep*(5*a3*d2 - 4*a3*d2*nu + a1*c1*d*(-5 + 4*nu) + 2*a4*c1*r1 - 4*a4*c1*nu*r1 + 4*a3*r12 - 8*a3*nu*r12 + a2*(5*d2 - 4*d2*nu + 4*r12 - 8*nu*r12)))/(12.0*c1*d),0,
                    -0.020833333333333332*(b*Ep*(d2*(l10 - l3 - l4 - l5 + l9)*(-5 + 4*nu) + 4*(-(l13*r12) + l3*r12 + l4*r12 + l5*r12 - l9*r12 + 2*l13*nu*r12 - 2*l3*nu*r12 
                    - 2*l4*nu*r12 - 2*l5*nu*r12 + 2*l9*nu*r12 + l10*(-1 + 2*nu)*r12 + l12*(-1 + 2*nu)*r12 + 6*d*rmz)))/d,Ep*nu*z},

                {0,0,(b*Ep*(a3*d2 - 4*a3*d2*nu + a1*c1*d*(-1 + 4*nu) + 4*a4*c1*r1- 4*a4*c1*nu*r1+ 8*a3*r12 - 8*a3*nu*r12 + a2*(d2 - 4*d2*nu + 8*r12 - 8*nu*r12)))/(12.0*c1*d),
                    -0.041666666666666664*(b*Ep*(a3*d2 - 4*a3*d2*nu + a1*c1*d*(-1 + 4*nu) - 2*c1*l12*r1+ 2*c1*l13*r1+ 2*c1*l12*nu*r1- 2*c1*l13*nu*r1+ 8*a3*r12 - 8*a3*nu*r12 
                    + a2*(d2 - 4*d2*nu + 8*r12 - 8*nu*r12)))/c1,0},
               
                {0,-0.020833333333333332*(b*Ep*(d2*(l10 - l3 - l4 - l5 + l9)*(-5 + 4*nu) + 4*(-(l13*r12) + l3*r12 + l4*r12 + l5*r12 - l9*r12 + 2*l13*nu*r12 - 2*l3*nu*r12 
                - 2*l4*nu*r12 - 2*l5*nu*r12 + 2*l9*nu*r12 + l10*(-1 + 2*nu)*r12 + l12*(-1 + 2*nu)*r12 + 6*d*rmz)))/d,
                    -0.041666666666666664*(b*Ep*(a3*d2 - 4*a3*d2*nu + a1*c1*d*(-1 + 4*nu) + 4*a4*c1*r1- 4*a4*c1*nu*r1+ 8*a3*r12 - 8*a3*nu*r12 + a2*(d2 - 4*d2*nu + 8*r12 
                    - 8*nu*r12)))/c1,(b*Ep*(a1*c1*d*(d2 - 6*r12) - 2*a4*c1*r1*(2*d2*(-1 + nu) + (-1 + 2*nu)*r12) - a2*(d4 - 6*d2*r12 + 4*d2*nu*r12 - 4*r14 + 8*nu*r14) 
                    - a3*(d4 - 6*d2*r12 + 4*d2*nu*r12 - 4*r14 + 8*nu*r14) + 6*c1*d*rmz*z))/(12.0*c1*d), -0.5*(Ep*nu*(d*rmz + 2*z2))},
               
                {0,Ep*nu*z,0,(Ep*nu*(d3 - 8*d2*rmz + 20*d*(r12 - z2) - 16*Math.Pow(r12 - z2,1.5))*(-2*r12 + d*rmz + 2*z2))/(2.0*Math.Pow(d - 2*rmz,2)*(-d + 4*rmz)),-((Ep*(-1 + nu)*(a5*r12 + (d - rmz)*z))/b)}
            };

            return kComp;
        }

        protected override Complex[,] EvalIndefiniteIntegral_OutOfPlaneStressOverVolume(double z)
        {
            double z2 = z * z;
            double rmz = Math.Sqrt(r12 - z2);
            Complex a5 = Complex.Atan((rmz * z) / (-r2 + z2));


            Complex[,] IntS11S33dv = { {0, b * Ep * nu * z, 0, b * Ep * nu * (-0.5 * (d * rmz) - z2), Ep * (-1 + nu) * (-(a5 * r2) - d * z + rmz * z)},
                { 0,b*Ep*nu*z,0,(b*Ep*(d*(-1 + nu)*rmz - z2))/2.0,Ep*nu*(a5*r2 + d*z - rmz*z)}};

            return IntS11S33dv;
        }

        #endregion

        #region Static Methods

        #endregion
    }
}
