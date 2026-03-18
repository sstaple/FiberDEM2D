using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomMath;
using System.IO;
using FDEMCore.Contact.FailureTheories;

namespace FDEMCore.Contact.MatrixModels
{
    /// <summary>
    /// Purpose: Matrix model 1 is for isotropic damage based on max principle strain
    /// Created By: Scott_Stapleton
    /// Created On: 8/23/2022 10:39:02 AM 
    /// q = {Tf1, uf2, vf2, Tf2, ug, um1, vm1, wm1, Tm1, um2, vm2, wm2, Tm2};
    /// </summary>
    public class MatrixModel1 : ZIntegratedMatrixModel
    {
        #region Protected Members

        protected double E0, nu;
        protected double C33Coefficient;
        #endregion

        #region Public Members

        public new const string Name = "MatrixModel1";

        #endregion

        #region Constructor
        public MatrixModel1(double E, double nu, double r1, double r2, double d, double b, double zTop, double zBottom, bool isTopMatrix, int nIntPts, FailureCritForZIntegratedMatrix failureCriteria)
            : base(r1, r2, d, b, zTop, zBottom, isTopMatrix, nIntPts, failureCriteria)
        {
            this.E0 = E;
            this.nu = nu;

            nDOF = 13;
            this.nStateVarPerIntPt = ((IFailureCriteria)failureCriteria).NStateVariables;
        }
        #endregion

        #region override Methods

        //IntegrationPt Overrides
        
        protected override double[,] IntegralBDB_dA(double z, double[] intPtStateVariables)
        {
            double z2 = z * z;
            double z3 = z2 * z;
            double z4 = z3 * z;
            double z5 = z4 * z;
            double z6 = z5 * z;
            double z7 = z6 * z;
            double z8 = z7 * z;

            //Em is actually not just E of the matrix, but is this:
            double Damage = intPtStateVariables.Length == 0 ? 0 : intPtStateVariables[0];
            double Em = E0 * (1.0 - Damage) / ((1.0 + nu) * (1.0 - 2.0 * nu));

            double rmz2 = Math.Sqrt(r22 - z2);
            double rmz1 = Math.Sqrt(r12 - z2);
            double rmz22 = rmz2 * rmz2;
            double rmz12 = rmz1 * rmz1;

            double c1 = Math.Pow(-d + rmz1 + rmz2, 2.0);
            double c21 = Math.Pow(r12 - z2, 1.5);
            double c22 = Math.Pow(r22 - z2, 1.5);
            double c3 = Math.Sqrt((-r12 + z2) * (-r22 + z2));
            double c4 = Math.Sqrt((r12 - z2) / (r22 - z2));
            double c52 = Math.Pow(r2 - rmz2, 2.0);
            double c6 = Math.Pow(-d + r2 + rmz1, 2.0);
            double c7 = Math.Sqrt((r12 - z2) * (r24 - r22 * z2));
            double c81 = Math.Pow(d - rmz2, 2.0);
            double c82 = Math.Pow(d - rmz1, 2.0);
            double c9 = Math.Pow(-d + rmz1 + rmz2, 3.0);


            double[,] intBDB_dA = {

                //R1
               { 0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0},
               //R2
               {0.0, (0.16666666666666666*b*Em*(1.0 - 2.0*nu)*(((-1.0*d + r2 + rmz1)*(r2 - 1.0*rmz2)*z2)/c3 + (c6*z2)/(-1.0*r22 + z2) - 1.0*c52*(3.0 + z2/(r12 - 1.0*z2))))/(r22*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0,
   (0.08333333333333333*b*Em*(-1.0 + 2.0*nu)*(((-1.0*d2+ r2*(rmz1 - 1.0*rmz2) - 2.0*rmz1*rmz2 + d*(r2 + rmz1 + rmz2))*z2)/c3 + (2.0*rmz1*(-1.0*d + r2 + rmz1)*z2)/(-1.0*r22 + z2) + 2.0*(-1.0*r2 + rmz2)*(3.0*rmz1 + ((d - 1.0*rmz2)*z2)/(r12 - 1.0*z2))))/
    (r1*r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, (0.08333333333333333*b*Em*(-1.0 + 2.0*nu)*((-2.0 - (1.0*d*r2)/c3 + (2.0*d)/rmz1 - (1.0*r2)/rmz1 + r2/rmz2)*z2 + (2.0*(-1.0*d + rmz1)*(-1.0*d + r2 + rmz1)*z2)/(-1.0*r22 + z2) -
        2.0*rmz2*(-1.0*r2 + rmz2)*(3.0 + z2/(r12 - 1.0*z2))))/(r22*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0},
                //R3
                {0.0, 0.0, (0.16666666666666666*b*Em*(6.0*c52*(-1.0 + nu) + (-1.0 + 2.0*nu)*(((-1.0*d + r2 + rmz1)*(-1.0*r2 + rmz2))/c3 + c52/(r12 - 1.0*z2) + c6/(r22 - 1.0*z2))*z2))/(r22*(-1.0*d + rmz1 + rmz2)),0.0, (Em*nu*(r2 - 1.0*rmz2))/r2,0.0,
   (0.08333333333333333*b*Em*(12.0*(-1.0 + nu)*rmz1*(-1.0*r2 + rmz2) + (-1.0 + 2.0*nu)*z2*((rmz1*(d + r2 - 2.0*rmz2) - 1.0*(d - 1.0*r2)*(d - 1.0*rmz2))/c3 + (2.0*(d - 1.0*rmz2)*(-1.0*r2 + rmz2))/(r12 - 1.0*z2) + (2.0*rmz1*(-1.0*d + r2 + rmz1))/(-1.0*r22 + z2))))/
    (r1*r2*(-1.0*d + rmz1 + rmz2)),(0.25*b*Em*z*((r2 - 1.0*rmz2)*(2.0*d*nu + rmz1 - 2.0*nu*(rmz1 + rmz2)) + ((d - 2.0*d*nu - 1.0*r2 - 1.0*rmz1 + 2.0*nu*rmz1 + 2.0*nu*rmz2)*(r12 - 1.0*z2))/rmz2))/(r1*r2*rmz1*(-1.0*d + rmz1 + rmz2)),
   (0.08333333333333333*b*Em*(12.0*(-1.0 + nu)*(r2 - 1.0*rmz2)*z + (6.0*nu*(-1.0*r2 + rmz2)*z*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/c3 +
        (-1.0 + 2.0*nu)*(((-1.0*r2 + rmz2)*z*(-2.0*c3 + r12 + 2.0*d*rmz1 - 3.0*z2))/(r12 - 1.0*z2) - (1.0*z*(c3*r2 + d2*rmz1 + r12*(-2.0*r2 - 2.0*rmz1 + rmz2) + 4.0*r2*z2 + 3.0*rmz1*z2 - 2.0*rmz2*z2 - 1.0*d*(c3 - 1.0*r12 + r2*rmz1 + 2.0*z2)))/c3 -
           (2.0*(-1.0*d + r2 + rmz1)*z3)/(-1.0*r22 + z2))))/(r2*(-1.0*d + rmz1 + rmz2)),0.0, (0.08333333333333333*b*Em*
      (12.0*(-1.0 + nu)*(r2 - 1.0*rmz2)*rmz2 + (1.0 - 2.0*nu)*z2*(2.0 - (2.0*d)/rmz1 + r2*(d/c3 + 1/rmz1 - 1.0/rmz2) + (2.0*(-1.0*d + rmz1)*(-1.0*d + r2 + rmz1))/(r22 - 1.0*z2) + (2.0*(-1.0*r22 + r2*rmz2 + z2))/(-1.0*r12 + z2))))/(r22*(-1.0*d + rmz1 + rmz2)),
   (0.25*b*Em*z*(rmz2*(r22 - 1.0*d*rmz1 + rmz12 - 1.0*z2) + r2*(-1.0*r22 + 2.0*d*nu*rmz1 - 2.0*nu*rmz12 + rmz1*rmz2 - 2.0*nu*rmz1*rmz2 + z2)))/(r22*rmz1*rmz2*(-1.0*d + rmz1 + rmz2)),
   (-0.08333333333333333*b*Em*z*(r14*(-1.0*(1.0 + 4.0*nu)*r22 + 2.0*(1.0 + nu)*r2*rmz2 - 2.0*(-1.0 + 2.0*nu)*(c3 - 2.0*d*rmz2) + 3.0*z2) +
        r12*(2.0*c3*d2*(1.0 - 2.0*nu) - 2.0*(1.0 + nu)*r24 + 2.0*r23*(5.0*rmz1 - 4.0*nu*rmz1 + rmz2 + nu*rmz2) + 6.0*c3*z2 + 2.0*r2*(2.0*(-2.0 + nu)*rmz1 - 1.0*(4.0 + nu)*rmz2)*z2 + r22*(c3*(-11.0 + 10.0*nu) + (7.0 + 10.0*nu)*z2) +
           d*(r23 - 2.0*nu*r23 + r22*(rmz1 + 4.0*nu*rmz1 + (-1.0 + 2.0*nu)*rmz2) + (-3.0*rmz1 + 10.0*(1.0 - 2.0*nu)*rmz2)*z2 - 1.0*r2*(2.0*c3*(1.0 + nu) + z2 - 2.0*nu*z2)) - 9.0*z4) +
        z2*(2.0*c3*d2*(-1.0 + 2.0*nu) + 2.0*(1.0 + nu)*r24 + r23*(4.0*(-2.0 + nu)*rmz1 - 2.0*(1.0 + nu)*rmz2) - 6.0*c3*z2 + 6.0*r2*(rmz1 + rmz2)*z2 - 3.0*r22*(c3*(-3.0 + 2.0*nu) + 2.0*(1.0 + nu)*z2) +
           d*(2.0*c3*(1.0 + nu)*r2 + (-1.0 + 2.0*nu)*r23 - 1.0*r22*rmz1 + r22*rmz2 - 2.0*nu*r22*(2.0*rmz1 + rmz2) + (1.0 - 2.0*nu)*r2*z2 + 3.0*(rmz1 - 2.0*rmz2 + 4.0*nu*rmz2)*z2) + 6.0*z4)))/(c21*r2*(-1.0*d + rmz1 + rmz2)*(-1.0*r22 + z2))},
                //R4
               { 0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0},
                //R5
                {0.0, 0.0, (Em*nu*(r2 - 1.0*rmz2))/r2,0.0, (Em*(-1.0 + nu)*(-1.0*d + rmz1 + rmz2))/b,0.0, (-1.0*Em*nu*rmz1)/r1,(0.5*Em*nu*z*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/(c3*r1),(0.5*Em*nu*z*(2.0*c3 + r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/c3,0.0, (Em*nu*rmz2)/r2,
   (0.5*Em*nu*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(c3*r2),(-0.5*Em*nu*r2*z*(2.0*c3 + r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/c7},
                //R6
                {0.0, (0.08333333333333333*b*Em*(-1.0 + 2.0*nu)*(((-1.0*d2 + r2*(rmz1 - 1.0*rmz2) - 2.0*rmz1*rmz2 + d*(r2 + rmz1 + rmz2))*z2)/c3 + (2.0*rmz1*(-1.0*d + r2 + rmz1)*z2)/(-1.0*r22 + z2) +
        2.0*(-1.0*r2 + rmz2)*(3.0*rmz1 + ((d - 1.0*rmz2)*z2)/(r12 - 1.0*z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0,
   (0.16666666666666666*b*Em*(-1.0 + 2.0*nu)*(z2 - (1.0*d*z2)/rmz2 + (c82*z2)/(r12 - 1.0*z2) + (r12 - 1.0*z2)*(3.0 + z2/(r22 - 1.0*z2))))/(r12*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0,
   (0.08333333333333333*b*Em*(-1.0 + 2.0*nu)*(((2.0*c3 + d2 - 1.0*d*(rmz1 + rmz2))*z2)/c3 + (2.0*z2*(-1.0*r12 + d*rmz1 + z2))/(-1.0*r22 + z2) - 2.0*rmz2*(3.0*rmz1 + ((d - 1.0*rmz2)*z2)/(r12 - 1.0*z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0},
                //R7
                {0.0, 0.0, (0.08333333333333333*b*Em*(12.0*(-1.0 + nu)*rmz1*(-1.0*r2 + rmz2) + (-1.0 + 2.0*nu)*z2*((rmz1*(d + r2 - 2.0*rmz2) - 1.0*(d - 1.0*r2)*(d - 1.0*rmz2))/c3 + (2.0*(d - 1.0*rmz2)*(-1.0*r2 + rmz2))/(r12 - 1.0*z2) +
           (2.0*rmz1*(-1.0*d + r2 + rmz1))/(-1.0*r22 + z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),0.0, (-1.0*Em*nu*rmz1)/r1,0.0,
   (0.16666666666666666*b*Em*(((-1.0 + 2.0*nu)*(-1.0*d + rmz2)*z2)/rmz2 + (c82*(-1.0 + 2.0*nu)*z2)/(r12 - 1.0*z2) + (r12 - 1.0*z2)*(6.0*(-1.0 + nu) + ((1.0 - 2.0*nu)*z2)/(-1.0*r22 + z2))))/(r12*(-1.0*d + rmz1 + rmz2)),
   (0.25*b*Em*z*(-1.0*d3*rmz2 + d2*(2.0*c3 + r12 + 3.0*r22 - 4.0*z2) + (r12 + r22 - 2.0*z2)*(2.0*c3 + r12 + r22 - 2.0*z2) - 1.0*d*(r12*(2.0*rmz1 + 3.0*rmz2) + r22*(4.0*rmz1 + 3.0*rmz2) - 6.0*(rmz1 + rmz2)*z2)))/(c9*r12*rmz2),
   (0.08333333333333333*b*Em*z*(-2.0*d2*r22*rmz1 + 4.0*d2*nu*r22*rmz1 + 11.0*r12*r22*rmz1 - 10.0*nu*r12*r22*rmz1 - 2.0*r24*rmz1 + 4.0*nu*r24*rmz1 + 2.0*r14*rmz2 + 2.0*nu*r14*rmz2 + r12*r22*rmz2 + 4.0*nu*r12*r22*rmz2 -
        1.0*c3*d*(-1.0 + 2.0*nu)*(r12 + 4.0*r22 - 6.0*z2) + 2.0*d2*rmz1*z2 - 4.0*d2*nu*rmz1*z2 - 9.0*r12*rmz1*z2 + 6.0*nu*r12*rmz1*z2 - 6.0*r22*rmz1*z2 - 6.0*r12*rmz2*z2 - 6.0*nu*r12*rmz2*z2 - 3.0*r22*rmz2*z2 + d*(r12 + 4.0*nu*r12 - 3.0*z2)*(-1.0*r22 + z2) +
        6.0*rmz1*z4 + 6.0*rmz2*z4))/(r1*(d - 1.0*rmz1 - 1.0*rmz2)*(r12 - 1.0*z2)*(-1.0*r22 + z2)),0.0, (0.08333333333333333*b*Em*
      (-12.0*c3*(-1.0 + nu) + ((-1.0 + 2.0*nu)*(2.0*c21*(d - 1.0*rmz1) + 2.0*c22*(d - 1.0*rmz2) + c3*(-1.0*d2 - 2.0*rmz1*rmz2 + d*(rmz1 + rmz2)))*z2)/((r12 - 1.0*z2)*(-1.0*r22 + z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),
   (0.25*b*Em*z*(-1.0*(-1.0*rmz2 + 2.0*nu*(rmz1 + rmz2))*(r12 + r22 - 2.0*z2) + d*(-1.0*r22 + 2.0*nu*(r12 + r22 - 2.0*z2) + z2)))/(c3*r1*r2*(d - 1.0*rmz1 - 1.0*rmz2)),
   (0.08333333333333333*b*Em*z*(r24*((-1.0 + 2.0*nu)*(2.0*c3 - 1.0*d*rmz1) - 3.0*z2) + 2.0*(1.0 + nu)*r14*(r22 - 1.0*z2) +
        r12*(r24 + 4.0*nu*r24 + z2*(c3*(-9.0 + 6.0*nu) + d*(2.0*(1.0 + nu)*rmz1 + rmz2 - 2.0*nu*rmz2) + 6.0*(1.0 + nu)*z2) - 1.0*r22*(c3*(-11.0 + 10.0*nu) + d*(2.0*(1.0 + nu)*rmz1 + rmz2 - 2.0*nu*rmz2) + (7.0 + 10.0*nu)*z2)) -
        1.0*z2*(c3*(d2 - 2.0*d2*nu - 6.0*z2) + 3.0*d*(2.0*nu*rmz1 + rmz2 - 2.0*nu*rmz2)*z2 + 6.0*z4) + r22*(c3*(d2 - 2.0*d2*nu - 6.0*z2) + d*(-1.0*rmz1 + 8.0*nu*rmz1 + 3.0*rmz2 - 6.0*nu*rmz2)*z2 + 9.0*z4)))/(c22*r1*(-1.0*d + rmz1 + rmz2)*(-1.0*r12 + z2))},
                //R8
                {0.0, 0.0, (0.25*b*Em*z*((r2 - 1.0*rmz2)*(2.0*d*nu + rmz1 - 2.0*nu*(rmz1 + rmz2)) + ((d - 2.0*d*nu - 1.0*r2 - 1.0*rmz1 + 2.0*nu*rmz1 + 2.0*nu*rmz2)*(r12 - 1.0*z2))/rmz2))/(r1*r2*rmz1*(-1.0*d + rmz1 + rmz2)),0.0,
   (0.5*Em*nu*z*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/(c3*r1),0.0, (0.25*b*Em*z*(-1.0*d3*rmz2 + d2*(2.0*c3 + r12 + 3.0*r22 - 4.0*z2) + (r12 + r22 - 2.0*z2)*(2.0*c3 + r12 + r22 - 2.0*z2) -
        1.0*d*(r12*(2.0*rmz1 + 3.0*rmz2) + r22*(4.0*rmz1 + 3.0*rmz2) - 6.0*(rmz1 + rmz2)*z2)))/(c9*r12*rmz2),
   (0.16666666666666666*b*Em*((2.0*(-1.0 + nu)*(-1.0*d + rmz2)*z2)/rmz2 + (2.0*c82*(-1.0 + nu)*z2)/(r12 - 1.0*z2) + (r12 - 1.0*z2)*(-3.0 + 6.0*nu + (2.0*(-1.0 + nu)*z2)/(r22 - 1.0*z2))))/(r12*(-1.0*d + rmz1 + rmz2)),
   (0.08333333333333333*b*Em*(r14*((3.0 - 6.0*nu)*r22 + z2 + 2.0*nu*z2) - 1.0*r12*(r22*(-3.0*(-1.0 + 2.0*nu)*(c3 - 1.0*d*rmz1) + (5.0 - 14.0*nu)*z2) + z2*(6.0*c3*(-1.0 + nu) + d*(3.0 - 6.0*nu)*rmz1 - 4.0*d*(-1.0 + nu)*rmz2 + (3.0 + 6.0*nu)*z2)) +
        z2*(-4.0*(-1.0 + nu)*r24 + d*r22*(-3.0*rmz1 + 8.0*(-1.0 + nu)*rmz2) + 3.0*r22*(c3 - 2.0*z2) - 4.0*d2*(-1.0 + nu)*(r22 - 1.0*z2) - 6.0*c3*z2 + 3.0*d*(rmz1 - 4.0*(-1.0 + nu)*rmz2)*z2 + 6.0*z4)))/(r1*(-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2)*(-1.0*r22 + z2)),0.0,
   (0.25*b*Em*z*(-1.0*(-1.0*rmz1 + 2.0*nu*(rmz1 + rmz2))*(r12 + r22 - 2.0*z2) + d*((-1.0 + 2.0*nu)*r12 + 2.0*nu*(r22 - 2.0*z2) + z2)))/(c3*r1*r2*(-1.0*d + rmz1 + rmz2)),
   (0.16666666666666666*b*Em*(3.0*c3*(1.0 - 2.0*nu) + ((-1.0 + nu)*(2.0*c21*(d - 1.0*rmz1) + 2.0*c22*(d - 1.0*rmz2) + c3*(-1.0*d2 - 2.0*rmz1*rmz2 + d*(rmz1 + rmz2)))*z2)/((r12 - 1.0*z2)*(-1.0*r22 + z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),
   (0.08333333333333333*b*Em*(3.0*(1.0 - 2.0*nu)*r12*r24*rmz1 + (-3.0 + 6.0*nu)*r14*r22*rmz2 - 1.0*(1.0 + 2.0*nu)*r14*rmz2*z2 + r12*r22*(3.0*(-3.0 + 4.0*nu)*rmz1 + (5.0 - 14.0*nu)*rmz2)*z2 + r24*(-3.0*rmz1 + 4.0*(-1.0 + nu)*rmz2)*z2 -
        2.0*d2*(-1.0 + nu)*rmz1*z2*(-1.0*r22 + z2) + d*r12*(c3*((3.0 - 6.0*nu)*r22 + z2 + 2.0*nu*z2) - 2.0*(-1.0 + nu)*(r22*z2 - 1.0*z4)) + 3.0*r22*(3.0*rmz1 + 2.0*rmz2)*z4 + 3.0*r12*(-2.0*(-1.0 + nu)*rmz1 + rmz2 + 2.0*nu*rmz2)*z4 -
        1.0*d*z2*(c3*(r22 - 10.0*nu*r22 + 3.0*z2 + 6.0*nu*z2) + 2.0*(-1.0 + nu)*(2.0*r24 - 5.0*r22*z2 + 3.0*z4)) - 6.0*(rmz1 + rmz2)*z6))/(c22*r1*(-1.0*d + rmz1 + rmz2)*(-1.0*r12 + z2))},
                //R9
                {0.0, 0.0, (0.08333333333333333*b*Em*((-6.0*r12*(r2 - 1.0*rmz2)*z*(-2.0*c3*(-1.0 + nu) + nu*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2)))/c3 +
        (-1.0 + 2.0*nu)*r12*(((-1.0*r2 + rmz2)*z*(-2.0*c3 + r12 + 2.0*d*rmz1 - 3.0*z2))/(r12 - 1.0*z2) - (1.0*z*(c3*r2 + d2*rmz1 + r12*(-2.0*r2 - 2.0*rmz1 + rmz2) + 4.0*r2*z2 + 3.0*rmz1*z2 - 2.0*rmz2*z2 - 1.0*d*(c3 - 1.0*r12 + r2*rmz1 + 2.0*z2)))/c3 -
           (2.0*(-1.0*d + r2 + rmz1)*z3)/(-1.0*r22 + z2))))/(r12*r2*(-1.0*d + rmz1 + rmz2)),0.0, (0.5*Em*nu*z*(2.0*c3 + r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/c3,0.0,
   (0.08333333333333333*b*Em*z*(2.0*c3*d2*(-1.0 + 2.0*nu)*r22 + (1.0 + 4.0*nu)*r12*r24 + 2.0*(-1.0 + 2.0*nu)*r24*(c3 - 2.0*d*rmz1) + 2.0*(1.0 + nu)*r14*(r22 - 1.0*z2) + 2.0*c3*d2*(1.0 - 2.0*nu)*z2 - 3.0*r24*z2 +
        r22*(-6.0*c3 + 10.0*d*(-1.0 + 2.0*nu)*rmz1 + 3.0*d*rmz2)*z2 + r12*z2*(c3*(-9.0 + 6.0*nu) + d*(-1.0*rmz1 + 2.0*nu*rmz1 + rmz2 + 4.0*nu*rmz2) + 6.0*(1.0 + nu)*z2) -
        1.0*r12*r22*(c3*(-11.0 + 10.0*nu) + d*(-1.0*rmz1 + 2.0*nu*rmz1 + rmz2 + 4.0*nu*rmz2) + (7.0 + 10.0*nu)*z2) + 6.0*c3*z4 + 9.0*r22*z4 - 3.0*d*((-2.0 + 4.0*nu)*rmz1 + rmz2)*z4 - 6.0*z6))/(c22*r1*(-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2)),
   (0.08333333333333333*b*Em*(-4.0*c22*d2*(-1.0 + nu)*z2 + r24*(3.0*rmz1 - 4.0*(-1.0 + nu)*rmz2)*z2 + r14*rmz2*((3.0 - 6.0*nu)*r22 + z2 + 2.0*nu*z2) + d*(-1.0*r22 + z2)*(-4.0*(-1.0 + nu)*(r12 + 2.0*r22 - 3.0*z2)*z2 + 3.0*c3*((-1.0 + 2.0*nu)*r12 + z2)) -
        3.0*r22*(3.0*rmz1 + 2.0*rmz2)*z4 + r12*(3.0*(-1.0 + 2.0*nu)*r24*rmz1 + r22*((9.0 - 12.0*nu)*rmz1 + (-5.0 + 14.0*nu)*rmz2)*z2 + 6.0*(-1.0 + nu)*rmz1*z4 - 3.0*(1.0 + 2.0*nu)*rmz2*z4) + 6.0*(rmz1 + rmz2)*z6))/(c22*r1*(-1.0*d + rmz1 + rmz2)*(-1.0*r12 + z2)),
   (-0.16666666666666666*b*Em*(r24*(c3 - 2.0*d*rmz1 - 3.0*z2)*z2 + r14*((-1.0 + 2.0*nu)*r24 + (c3 + d*(-1.0 + 2.0*nu)*rmz2 - 3.0*z2)*z2 + r22*(-1.0*(-1.0 + 2.0*nu)*(c3 + d*rmz2) - 2.0*(-2.0 + nu)*z2)) - 1.0*c3*d2*z4 + 6.0*c3*z6 - 3.0*d*rmz1*z6 - 3.0*d*rmz2*z6 +
        r22*(c3*d2*z2 - 6.0*c3*z4 + 5.0*d*rmz1*z4 + 3.0*d*rmz2*z4 + 9.0*z6) + r12*(c3*d2*(1.0 - 2.0*nu)*r22 - 1.0*(-1.0 + 2.0*nu)*r24*(c3 - 2.0*d*rmz1) + c3*d2*(-1.0 + 2.0*nu)*z2 - 2.0*(-2.0 + nu)*r24*z2 +
           r22*(c3*(2.0 + 4.0*nu) + d*(3.0 - 8.0*nu)*rmz1 + 2.0*d*(-2.0 + nu)*rmz2)*z2 - 6.0*c3*z4 + (-13.0 + 2.0*nu)*r22*z4 + d*((-1.0 + 4.0*nu)*rmz1 - 2.0*(-2.0 + nu)*rmz2)*z4 + 9.0*z6) - 6.0*z8))/((-1.0*d + rmz1 + rmz2)*Math.Pow((r12 - 1.0*z2)*(r22 - 1.0*z2),1.5)),0.0,
   (0.08333333333333333*b*Em*z*(-2.0*c3*r24 - 2.0*c3*nu*r24 - 2.0*(-1.0 + 2.0*nu)*r14*(r22 - 1.0*z2) + d2*(-1.0 + 2.0*nu)*(r12 - 1.0*z2)*(r22 - 1.0*z2) + 6.0*c3*r22*z2 + 6.0*c3*nu*r22*z2 + 9.0*r24*z2 - 6.0*nu*r24*z2 +
        r12*(-1.0*c3*(1.0 + 4.0*nu)*r22 + (-11.0 + 10.0*nu)*r24 + 3.0*c3*z2 + (17.0 - 10.0*nu)*r22*z2 - 6.0*z4) - 6.0*c3*z4 - 15.0*r22*z4 + 6.0*nu*r22*z4 +
        d*(2.0*(1.0 + nu)*r24*rmz1 - 1.0*r22*((2.0 + 8.0*nu)*rmz1 + rmz2 - 2.0*nu*rmz2)*z2 + (-1.0 + 2.0*nu)*r12*(r22*rmz1 - 1.0*r22*rmz2 - 1.0*rmz1*z2 + 3.0*rmz2*z2) + 3.0*(2.0*nu*(rmz1 - 1.0*rmz2) + rmz2)*z4) + 6.0*z6))/(c22*r2*(-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2))
    ,(0.08333333333333333*b*Em*((1.0 + 2.0*nu)*r24*rmz1*z2 + 2.0*d2*(-1.0 + nu)*rmz2*z2*(-1.0*r12 + z2) + r14*(3.0*(-1.0 + 2.0*nu)*r22*rmz2 - 4.0*(-1.0 + nu)*rmz1*z2 + 3.0*rmz2*z2) +
        d*(4.0*(-1.0 + nu)*r14*z2 + z2*(-1.0*r22 + 3.0*z2)*(c3 + 2.0*c3*nu + 2.0*(-1.0 + nu)*z2) + c3*r12*((-3.0 + 6.0*nu)*r22 + z2 - 10.0*nu*z2) + 2.0*(-1.0 + nu)*r12*(r22*z2 - 5.0*z4)) - 3.0*r22*(rmz1 + 2.0*nu*rmz1 - 2.0*(-1.0 + nu)*rmz2)*z4 -
        1.0*r12*(3.0*(-1.0 + 2.0*nu)*r24*rmz1 + r22*(5.0*rmz1 - 14.0*nu*rmz1 - 9.0*rmz2 + 12.0*nu*rmz2)*z2 + 6.0*rmz1*z4 + 9.0*rmz2*z4) + 6.0*(rmz1 + rmz2)*z6))/(c21*r2*(-1.0*d + rmz1 + rmz2)*(-1.0*r22 + z2)),
   (-0.08333333333333333*b*Em*(d2*(-1.0 + 2.0*nu)*r12*(r22 - 1.0*z2) - 1.0*d*r22*((2.0 + 4.0*nu)*rmz1 + (3.0 - 2.0*nu)*rmz2)*z2 + 2.0*r14*(r22 - 2.0*nu*r22 + z2) + d2*z2*(2.0*c3 - 2.0*c3*nu + r22 - 2.0*nu*r22 - 1.0*z2 + 2.0*nu*z2) +
        d*r12*((-1.0 + 2.0*nu)*r22*(rmz1 + rmz2) + (-3.0 + 2.0*nu)*rmz1*z2 - 2.0*(rmz2 + 2.0*nu*rmz2)*z2) + 2.0*r12*(c3*(-1.0 + 2.0*nu)*r22 + r24 - 2.0*nu*r24 + 3.0*c3*z2 + 2.0*r22*z2 + 4.0*nu*r22*z2 - 6.0*z4) + 6.0*d*(rmz1 + rmz2)*z4 +
        2.0*z2*(r24 + 3.0*c3*(r22 - 2.0*z2) - 6.0*r22*z2 + 6.0*z4)))/((-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2)*(-1.0*r22 + z2))},
                //R10
                {0.0, (0.08333333333333333*b*Em*(-1.0 + 2.0*nu)*((-2.0 - (1.0*d*r2)/c3 + (2.0*d)/rmz1 - (1.0*r2)/rmz1 + r2/rmz2)*z2 + (2.0*(-1.0*d + rmz1)*(-1.0*d + r2 + rmz1)*z2)/(-1.0*r22 + z2) - 2.0*rmz2*(-1.0*r2 + rmz2)*(3.0 + z2/(r12 - 1.0*z2))))/
    (r22*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, (0.08333333333333333*b*Em*(-1.0 + 2.0*nu)*(((2.0*c3 + d2 - 1.0*d*(rmz1 + rmz2))*z2)/c3 + (2.0*z2*(-1.0*r12 + d*rmz1 + z2))/(-1.0*r22 + z2) - 2.0*rmz2*(3.0*rmz1 + ((d - 1.0*rmz2)*z2)/(r12 - 1.0*z2))))/
    (r1*r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, (0.16666666666666666*b*Em*(-1.0 + 2.0*nu)*(z2 - (1.0*d*z2)/rmz1 + (c81*z2)/(r22 - 1.0*z2) + (r22 - 1.0*z2)*(3.0 + z2/(r12 - 1.0*z2))))/(r22*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0},
                //R11
                {0.0, 0.0, (0.08333333333333333*b*Em*(12.0*(-1.0 + nu)*(r2 - 1.0*rmz2)*rmz2 + (1.0 - 2.0*nu)*z2*(2.0 - (2.0*d)/rmz1 + r2*(d/c3 + 1/rmz1 - 1.0/rmz2) + (2.0*(-1.0*d + rmz1)*(-1.0*d + r2 + rmz1))/(r22 - 1.0*z2) + (2.0*(-1.0*r22 + r2*rmz2 + z2))/(-1.0*r12 + z2))))/
    (r22*(-1.0*d + rmz1 + rmz2)),0.0, (Em*nu*rmz2)/r2,0.0, (0.08333333333333333*b*Em*(-12.0*c3*(-1.0 + nu) +
        ((-1.0 + 2.0*nu)*(2.0*c21*(d - 1.0*rmz1) + 2.0*c22*(d - 1.0*rmz2) + c3*(-1.0*d2 - 2.0*rmz1*rmz2 + d*(rmz1 + rmz2)))*z2)/((r12 - 1.0*z2)*(-1.0*r22 + z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),
   (0.25*b*Em*z*(-1.0*(-1.0*rmz1 + 2.0*nu*(rmz1 + rmz2))*(r12 + r22 - 2.0*z2) + d*((-1.0 + 2.0*nu)*r12 + 2.0*nu*(r22 - 2.0*z2) + z2)))/(c3*r1*r2*(-1.0*d + rmz1 + rmz2)),
   (0.08333333333333333*b*Em*z*(-2.0*c3*r24 - 2.0*c3*nu*r24 - 2.0*(-1.0 + 2.0*nu)*r14*(r22 - 1.0*z2) + d2*(-1.0 + 2.0*nu)*(r12 - 1.0*z2)*(r22 - 1.0*z2) + 6.0*c3*r22*z2 + 6.0*c3*nu*r22*z2 + 9.0*r24*z2 - 6.0*nu*r24*z2 +
        r12*(-1.0*c3*(1.0 + 4.0*nu)*r22 + (-11.0 + 10.0*nu)*r24 + 3.0*c3*z2 + (17.0 - 10.0*nu)*r22*z2 - 6.0*z4) - 6.0*c3*z4 - 15.0*r22*z4 + 6.0*nu*r22*z4 +
        d*(2.0*(1.0 + nu)*r24*rmz1 - 1.0*r22*((2.0 + 8.0*nu)*rmz1 + rmz2 - 2.0*nu*rmz2)*z2 + (-1.0 + 2.0*nu)*r12*(r22*rmz1 - 1.0*r22*rmz2 - 1.0*rmz1*z2 + 3.0*rmz2*z2) + 3.0*(2.0*nu*(rmz1 - 1.0*rmz2) + rmz2)*z4) + 6.0*z6))/(c22*r2*(-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2))
    ,0.0, (0.16666666666666666*b*Em*(6.0*(-1.0 + nu)*(r22 - 1.0*z2) + (-1.0 + 2.0*nu)*(1.0 - (1.0*d)/rmz1 + c81/(r22 - 1.0*z2) + (r22 - 1.0*z2)/(r12 - 1.0*z2))*z2))/(r22*(-1.0*d + rmz1 + rmz2)),
   (-0.25*b*Em*z*(-1.0*c3*d3 + (r12 + r22 - 2.0*z2)*(r12*rmz2 + r22*(2.0*rmz1 + rmz2) - 2.0*(rmz1 + rmz2)*z2) + d2*(3.0*r12*rmz2 + r22*(2.0*rmz1 + rmz2) - 2.0*(rmz1 + 2.0*rmz2)*z2) -
        1.0*d*(2.0*r24 + 3.0*c3*(r12 + r22 - 2.0*z2) + 4.0*r12*(r22 - 1.0*z2) - 8.0*r22*z2 + 6.0*z4)))/(c3*c9*r22),
   (0.08333333333333333*b*Em*z*(r14*(r22 + 4.0*nu*r22 + 2.0*(-1.0 + 2.0*nu)*(c3 - 2.0*d*rmz2) - 3.0*z2) - 1.0*z2*
         (2.0*c3*d2*(-1.0 + 2.0*nu) + 3.0*c3*(3.0 - 2.0*nu)*r22 + 2.0*(1.0 + nu)*r24 - 1.0*d*r22*(rmz1 + 4.0*nu*rmz1 + (-1.0 + 2.0*nu)*rmz2) - 6.0*c3*z2 - 6.0*(1.0 + nu)*r22*z2 + 3.0*d*(rmz1 - 2.0*rmz2 + 4.0*nu*rmz2)*z2 + 6.0*z4) +
        r12*(2.0*c3*d2*(-1.0 + 2.0*nu) + 2.0*(1.0 + nu)*r24 + (-6.0*c3 + 3.0*d*rmz1 + 10.0*d*(-1.0 + 2.0*nu)*rmz2)*z2 - 1.0*r22*(c3*(-11.0 + 10.0*nu) + d*(rmz1 + 4.0*nu*rmz1 - 1.0*rmz2 + 2.0*nu*rmz2) + (7.0 + 10.0*nu)*z2) + 9.0*z4)))/
    (c21*r2*(-1.0*d + rmz1 + rmz2)*(r22 - 1.0*z2))},
                //R12
                {0.0, 0.0, (0.25*b*Em*z*(rmz2*(r22 - 1.0*d*rmz1 + rmz12 - 1.0*z2) + r2*(-1.0*r22 + 2.0*d*nu*rmz1 - 2.0*nu*rmz12 + rmz1*rmz2 - 2.0*nu*rmz1*rmz2 + z2)))/(r22*rmz1*rmz2*(-1.0*d + rmz1 + rmz2)),0.0,
   (0.5*Em*nu*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(c3*r2),0.0, (0.25*b*Em*z*(-1.0*(-1.0*rmz2 + 2.0*nu*(rmz1 + rmz2))*(r12 + r22 - 2.0*z2) + d*(-1.0*r22 + 2.0*nu*(r12 + r22 - 2.0*z2) + z2)))/(c3*r1*r2*(d - 1.0*rmz1 - 1.0*rmz2)),
   (0.16666666666666666*b*Em*(3.0*c3*(1.0 - 2.0*nu) + ((-1.0 + nu)*(2.0*c21*(d - 1.0*rmz1) + 2.0*c22*(d - 1.0*rmz2) + c3*(-1.0*d2 - 2.0*rmz1*rmz2 + d*(rmz1 + rmz2)))*z2)/((r12 - 1.0*z2)*(-1.0*r22 + z2))))/(r1*r2*(-1.0*d + rmz1 + rmz2)),
   (0.08333333333333333*b*Em*(4.0*(-1.0 + nu)*r14*rmz2*z2 - 2.0*d2*(-1.0 + nu)*rmz1*z2*(-1.0*r22 + z2) - 1.0*z2*(r24*(-6.0*(-1.0 + nu)*rmz1 + rmz2 + 2.0*nu*rmz2) + 6.0*(-2.0 + nu)*r22*rmz1*z2 - 3.0*(1.0 + 2.0*nu)*r22*rmz2*z2 + 6.0*(rmz1 + rmz2)*z4) +
        r12*(-3.0*(-1.0 + 2.0*nu)*r24*(rmz1 - 1.0*rmz2) + r22*(6.0*(-1.0 + nu)*rmz1 + (5.0 - 14.0*nu)*rmz2)*z2 + 3.0*(rmz1 + 2.0*rmz2)*z4) +
        d*((-2.0*c3*(-1.0 + nu) + (1.0 + 2.0*nu)*(r22 - 1.0*z2))*(r22 - 3.0*z2)*z2 + r12*((3.0 - 6.0*nu)*r24 - 4.0*c3*(-1.0 + nu)*z2 - 4.0*r22*z2 + 16.0*nu*r22*z2 + z4 - 10.0*nu*z4))))/(c22*r2*(-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2)),0.0,
   (-0.25*b*Em*z*(-1.0*c3*d3 + (r12 + r22 - 2.0*z2)*(r12*rmz2 + r22*(2.0*rmz1 + rmz2) - 2.0*(rmz1 + rmz2)*z2) + d2*(3.0*r12*rmz2 + r22*(2.0*rmz1 + rmz2) - 2.0*(rmz1 + 2.0*rmz2)*z2) -
        1.0*d*(2.0*r24 + 3.0*c3*(r12 + r22 - 2.0*z2) + 4.0*r12*(r22 - 1.0*z2) - 8.0*r22*z2 + 6.0*z4)))/(c3*c9*r22),
   (0.16666666666666666*b*Em*(3.0*(-1.0 + 2.0*nu)*(r22 - 1.0*z2) + 2.0*(-1.0 + nu)*(1.0 - (1.0*d)/rmz1 + c81/(r22 - 1.0*z2) + (r22 - 1.0*z2)/(r12 - 1.0*z2))*z2))/(r22*(-1.0*d + rmz1 + rmz2)),
   (0.08333333333333333*b*Em*(4.0*(-1.0 + nu)*r14*rmz2*z2 - 4.0*d2*(-1.0 + nu)*rmz2*z2*(-1.0*r12 + z2) - 1.0*z2*(r24*(-6.0*(-1.0 + nu)*rmz1 + rmz2 + 2.0*nu*rmz2) + 6.0*(-2.0 + nu)*r22*rmz1*z2 - 3.0*(1.0 + 2.0*nu)*r22*rmz2*z2 + 6.0*(rmz1 + rmz2)*z4) +
        r12*(-3.0*(-1.0 + 2.0*nu)*r24*(rmz1 - 1.0*rmz2) + r22*(6.0*(-1.0 + nu)*rmz1 + (5.0 - 14.0*nu)*rmz2)*z2 + 3.0*(rmz1 + 2.0*rmz2)*z4) +
        d*(r12*((-3.0 + 6.0*nu)*r24 - 8.0*c3*(-1.0 + nu)*z2 - 6.0*(-1.0 + nu)*r22*z2 - 3.0*z4) + z2*(-4.0*c3*(-1.0 + nu)*(r22 - 3.0*z2) + 3.0*(r24 - 2.0*nu*r24 + 2.0*(-1.0 + nu)*r22*z2 + z4)))))/(c22*r2*(-1.0*d + rmz1 + rmz2)*(-1.0*r12 + z2))},
                //R13
                {0.0, 0.0, (-0.08333333333333333*b*Em*z*(r14*(-1.0*(1.0 + 4.0*nu)*r22 + 2.0*(1.0 + nu)*r2*rmz2 - 2.0*(-1.0 + 2.0*nu)*(c3 - 2.0*d*rmz2) + 3.0*z2) +
        r12*(2.0*c3*d2*(1.0 - 2.0*nu) - 2.0*(1.0 + nu)*r24 + 2.0*r23*(5.0*rmz1 - 4.0*nu*rmz1 + rmz2 + nu*rmz2) + 6.0*c3*z2 + 2.0*r2*(2.0*(-2.0 + nu)*rmz1 - 1.0*(4.0 + nu)*rmz2)*z2 + r22*(c3*(-11.0 + 10.0*nu) + (7.0 + 10.0*nu)*z2) +
           d*(r23 - 2.0*nu*r23 + r22*(rmz1 + 4.0*nu*rmz1 + (-1.0 + 2.0*nu)*rmz2) + (-3.0*rmz1 + 10.0*(1.0 - 2.0*nu)*rmz2)*z2 - 1.0*r2*(2.0*c3*(1.0 + nu) + z2 - 2.0*nu*z2)) - 9.0*z4) +
        z2*(2.0*c3*d2*(-1.0 + 2.0*nu) + 2.0*(1.0 + nu)*r24 + r23*(4.0*(-2.0 + nu)*rmz1 - 2.0*(1.0 + nu)*rmz2) - 6.0*c3*z2 + 6.0*r2*(rmz1 + rmz2)*z2 - 3.0*r22*(c3*(-3.0 + 2.0*nu) + 2.0*(1.0 + nu)*z2) +
           d*(2.0*c3*(1.0 + nu)*r2 + (-1.0 + 2.0*nu)*r23 - 1.0*r22*rmz1 + r22*rmz2 - 2.0*nu*r22*(2.0*rmz1 + rmz2) + (1.0 - 2.0*nu)*r2*z2 + 3.0*(rmz1 - 2.0*rmz2 + 4.0*nu*rmz2)*z2) + 6.0*z4)))/(c21*r2*(-1.0*d + rmz1 + rmz2)*(-1.0*r22 + z2)),0.0,
   (-0.5*Em*nu*r2*z*(2.0*c3 + r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/c7,0.0, (0.08333333333333333*b*Em*z*(r24*((-1.0 + 2.0*nu)*(2.0*c3 - 1.0*d*rmz1) - 3.0*z2) + 2.0*(1.0 + nu)*r14*(r22 - 1.0*z2) +
        r12*(r24 + 4.0*nu*r24 + z2*(c3*(-9.0 + 6.0*nu) + d*(2.0*(1.0 + nu)*rmz1 + rmz2 - 2.0*nu*rmz2) + 6.0*(1.0 + nu)*z2) - 1.0*r22*(c3*(-11.0 + 10.0*nu) + d*(2.0*(1.0 + nu)*rmz1 + rmz2 - 2.0*nu*rmz2) + (7.0 + 10.0*nu)*z2)) -
        1.0*z2*(c3*(d2 - 2.0*d2*nu - 6.0*z2) + 3.0*d*(2.0*nu*rmz1 + rmz2 - 2.0*nu*rmz2)*z2 + 6.0*z4) + r22*(c3*(d2 - 2.0*d2*nu - 6.0*z2) + d*(-1.0*rmz1 + 8.0*nu*rmz1 + 3.0*rmz2 - 6.0*nu*rmz2)*z2 + 9.0*z4)))/(c22*r1*(-1.0*d + rmz1 + rmz2)*(-1.0*r12 + z2)),
   (0.08333333333333333*b*Em*(r14*((-3.0 + 6.0*nu)*r22 - 1.0*(1.0 + 2.0*nu)*z2) + r12*(r22*(-3.0*(-1.0 + 2.0*nu)*(c3 + d*rmz1) + (5.0 - 14.0*nu)*z2) + z2*(6.0*c3*(-1.0 + nu) + d*(rmz1 + 2.0*nu*rmz1 - 2.0*(-1.0 + nu)*rmz2) + (3.0 + 6.0*nu)*z2)) -
        1.0*z2*(-2.0*c3*d2*(-1.0 + nu) + 3.0*c3*r22 - 4.0*(-1.0 + nu)*r24 + d*r22*(rmz1 - 10.0*nu*rmz1 + 4.0*(-1.0 + nu)*rmz2) - 6.0*c3*z2 - 6.0*r22*z2 + 3.0*d*(rmz1 + 2.0*nu*rmz1 - 2.0*(-1.0 + nu)*rmz2)*z2 + 6.0*z4)))/
    (r1*(-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2)*(-1.0*r22 + z2)),(-0.08333333333333333*b*Em*(d2*(-1.0 + 2.0*nu)*r12*(r22 - 1.0*z2) - 1.0*d*r22*((2.0 + 4.0*nu)*rmz1 + (3.0 - 2.0*nu)*rmz2)*z2 + 2.0*r14*(r22 - 2.0*nu*r22 + z2) +
        d2*z2*(2.0*c3 - 2.0*c3*nu + r22 - 2.0*nu*r22 - 1.0*z2 + 2.0*nu*z2) + d*r12*((-1.0 + 2.0*nu)*r22*(rmz1 + rmz2) + (-3.0 + 2.0*nu)*rmz1*z2 - 2.0*(rmz2 + 2.0*nu*rmz2)*z2) +
        2.0*r12*(c3*(-1.0 + 2.0*nu)*r22 + r24 - 2.0*nu*r24 + 3.0*c3*z2 + 2.0*r22*z2 + 4.0*nu*r22*z2 - 6.0*z4) + 6.0*d*(rmz1 + rmz2)*z4 + 2.0*z2*(r24 + 3.0*c3*(r22 - 2.0*z2) - 6.0*r22*z2 + 6.0*z4)))/((-1.0*d + rmz1 + rmz2)*(r12 - 1.0*z2)*(-1.0*r22 + z2)),0.0,
   (0.08333333333333333*b*Em*z*(r14*(r22 + 4.0*nu*r22 + 2.0*(-1.0 + 2.0*nu)*(c3 - 2.0*d*rmz2) - 3.0*z2) - 1.0*z2*
         (2.0*c3*d2*(-1.0 + 2.0*nu) + 3.0*c3*(3.0 - 2.0*nu)*r22 + 2.0*(1.0 + nu)*r24 - 1.0*d*r22*(rmz1 + 4.0*nu*rmz1 + (-1.0 + 2.0*nu)*rmz2) - 6.0*c3*z2 - 6.0*(1.0 + nu)*r22*z2 + 3.0*d*(rmz1 - 2.0*rmz2 + 4.0*nu*rmz2)*z2 + 6.0*z4) +
        r12*(2.0*c3*d2*(-1.0 + 2.0*nu) + 2.0*(1.0 + nu)*r24 + (-6.0*c3 + 3.0*d*rmz1 + 10.0*d*(-1.0 + 2.0*nu)*rmz2)*z2 - 1.0*r22*(c3*(-11.0 + 10.0*nu) + d*(rmz1 + 4.0*nu*rmz1 - 1.0*rmz2 + 2.0*nu*rmz2) + (7.0 + 10.0*nu)*z2) + 9.0*z4)))/
    (c21*r2*(-1.0*d + rmz1 + rmz2)*(r22 - 1.0*z2)),(0.08333333333333333*b*Em*(4.0*c21*d2*(-1.0 + nu)*z2 - 1.0*(1.0 + 2.0*nu)*r24*rmz1*z2 + r14*((3.0 - 6.0*nu)*r22*rmz2 + 4.0*(-1.0 + nu)*rmz1*z2 - 3.0*rmz2*z2) +
        d*(r12 - 1.0*z2)*(-4.0*(-1.0 + nu)*(2.0*r12 + r22 - 3.0*z2)*z2 + 3.0*c3*((-1.0 + 2.0*nu)*r22 + z2)) + 3.0*r22*(rmz1 + 2.0*nu*rmz1 - 2.0*(-1.0 + nu)*rmz2)*z4 +
        r12*(3.0*(-1.0 + 2.0*nu)*r24*rmz1 + r22*(5.0*rmz1 - 14.0*nu*rmz1 - 9.0*rmz2 + 12.0*nu*rmz2)*z2 + 6.0*rmz1*z4 + 9.0*rmz2*z4) - 6.0*(rmz1 + rmz2)*z6))/(c21*r2*(-1.0*d + rmz1 + rmz2)*(-1.0*r22 + z2)),
   (0.16666666666666666*b*Em*(r24*rmz1*z2 + c21*d2*(r22 - 2.0*nu*r22 + z2) + r14*(-1.0*(-1.0 + 2.0*nu)*r22*(rmz1 - 1.0*rmz2) + (rmz1 + 3.0*rmz2)*z2) -
        1.0*d*(r12 - 1.0*z2)*(c3*(-1.0 + 2.0*nu)*r22 + 3.0*c3*z2 + (-1.0 + 4.0*nu)*r22*z2 + 2.0*r12*(r22 - 2.0*nu*r22 + z2) - 3.0*z4) - 6.0*r22*rmz1*z4 - 3.0*r22*rmz2*z4 +
        r12*(r24*(rmz1 - 2.0*nu*rmz1) + 2.0*r22*(rmz1 + 2.0*nu*rmz1 + 2.0*rmz2 - 1.0*nu*rmz2)*z2 - 3.0*(2.0*rmz1 + 3.0*rmz2)*z4) + 6.0*rmz1*z6 + 6.0*rmz2*z6))/(c21*(-1.0*d + rmz1 + rmz2)*(-1.0*r22 + z2))}
            };


            return intBDB_dA;
        }

        protected override double[,] IntegralBD_dA(double z, double[] intPtStateVariables)
        {

            double z2 = z * z;
            double z3 = z2 * z;
            double z4 = z3 * z;
            double z5 = z4 * z;
            double z6 = z5 * z;
            double z7 = z6 * z;
            double z8 = z7 * z;

            //Em is actually not just E of the matrix, but is this:

            double Damage = intPtStateVariables.Length == 0 ? 0 : intPtStateVariables[0];
            double Em = E0 * (1.0 - Damage) / ((1.0 + nu) * (1.0 - 2.0 * nu));

            double rmz2 = Math.Sqrt(r22 - z2);
            double rmz1 = Math.Sqrt(r12 - z2);
            double rmz22 = rmz2 * rmz2;
            double rmz12 = rmz1 * rmz1;

            double[,] intBD_dA = {
                { 0.0,0.0,(-1.0*b*Em*nu*(-1.0*r2 + rmz2))/r2,0.0,Em*(-1.0 + nu)*(-1.0*d + rmz1 + rmz2),0.0,(-1.0*b*Em*nu*rmz1)/r1,(0.5*b*Em*nu*z*(d2*rmz2 - 1.0*d*(r12 + 2.0*r22 + rmz1*rmz2 - 3.0*z2) + (rmz1 + rmz2)*(r12 + r22 - 2.0*z2)))/(r1*rmz1*rmz2*(-1.0*d + rmz1 + rmz2)),
   (0.5*b*Em*nu*z*(3.0*r22*rmz1 + d2*rmz2 + r22*rmz2 + r12*(rmz1 + 3.0*rmz2) - 1.0*d*(r12 + 2.0*r22 + 3.0*rmz1*rmz2 - 3.0*z2) - 4.0*rmz1*z2 - 4.0*rmz2*z2))/(rmz1*rmz2*(-1.0*d + rmz1 + rmz2)),0.0,(b*Em*nu*rmz2)/r2,
   (0.5*b*Em*nu*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(r2*rmz1*rmz2),(-0.5*b*Em*nu*z*(r12 + r22 - 1.0*d*rmz1 + 2.0*rmz1*rmz2 - 2.0*z2))/(rmz1*rmz2)
                },

                {0.0,0.0,(b*Em*(-1.0 + nu)*(-1.0*r2 + rmz2))/r2,0.0,-1.0*Em*nu*(-1.0*d + rmz1 + rmz2),0.0,(b*Em*(-1.0 + nu)*rmz1)/r1,
   (0.5*b*Em*nu*z*(d2*rmz2 - 1.0*d*(r12 + 2.0*r22 + rmz1*rmz2 - 3.0*z2) + (rmz1 + rmz2)*(r12 + r22 - 2.0*z2)))/(r1*rmz1*rmz2*(-1.0*d + rmz1 + rmz2)),
   (0.5*b*Em*z*(-1.0*d*nu*r12 + nu*r12*rmz1 + d2*nu*rmz2 + 2.0*r12*rmz2 - 1.0*nu*r12*rmz2 - 2.0*d*rmz1*rmz2 + d*nu*rmz1*rmz2 + r22*(-2.0*d*nu + 2.0*rmz1 - 1.0*nu*rmz1 + nu*rmz2) + (3.0*d*nu - 2.0*(rmz1 + rmz2))*z2))/(rmz1*rmz2*(-1.0*d + rmz1 + rmz2)),0.0,
   (-1.0*b*Em*(-1.0 + nu)*rmz2)/r2,(0.5*b*Em*nu*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(r2*rmz1*rmz2),(0.5*b*Em*z*(-1.0*nu*r12 - 1.0*nu*r22 + d*nu*rmz1 - 2.0*rmz1*rmz2 + 2.0*nu*rmz1*rmz2 + 2.0*nu*z2))/(rmz1*rmz2)
                },
                {0.0,0.0,(-1.0*b*Em*nu*(-1.0*r2 + rmz2))/r2,0.0,-1.0*Em*nu*(-1.0*d + rmz1 + rmz2),0.0,(-1.0*b*Em*nu*rmz1)/r1,(-0.5*b*Em*(-1.0 + nu)*z*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/(r1*rmz1*rmz2),
   (-0.5*b*Em*z*((-1.0 + nu)*r12 + (-1.0 + nu)*r22 + d*rmz2 - 1.0*d*nu*rmz2 - 2.0*nu*rmz1*rmz2 + 2.0*z2 - 2.0*nu*z2))/(rmz1*rmz2),0.0,(b*Em*nu*rmz2)/r2,(-0.5*b*Em*(-1.0 + nu)*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(r2*rmz1*rmz2),
   (0.5*b*Em*z*((-1.0 + nu)*r12 + (-1.0 + nu)*r22 + d*rmz1 - 1.0*d*nu*rmz1 - 2.0*nu*rmz1*rmz2 + 2.0*z2 - 2.0*nu*z2))/(rmz1*rmz2) },
                { 0.0,0.0,(0.25*b*Em*(-1.0 + 2.0*nu)*(((-1.0*d + r2 + rmz1)*z)/rmz2 + ((-1.0*r2 + rmz2)*z)/rmz1))/r2,0.0,0.0,0.0,(-0.25*b*Em*(-1.0 + 2.0*nu)*z*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/(r1*rmz1*rmz2),(0.5*b*Em*(-1.0 + 2.0*nu)*rmz1)/r1,
   (0.25*b*Em*(-1.0 + 2.0*nu)*(-1.0*r22*rmz1 + (r12 + d*rmz1)*rmz2 + 2.0*(rmz1 - 1.0*rmz2)*z2))/(rmz1*rmz2),0.0,(-0.25*b*Em*(-1.0 + 2.0*nu)*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(r2*rmz1*rmz2),(-0.5*b*Em*(-1.0 + 2.0*nu)*rmz2)/r2,
   (0.25*b*Em*(-1.0 + 2.0*nu)*(r22*rmz1 + (-1.0*r12 + d*rmz1)*rmz2 - 2.0*(rmz1 - 1.0*rmz2)*z2))/(rmz1*rmz2)},
                { 0.0,(0.25*b*Em*(-1.0 + 2.0*nu)*(((-1.0*d + r2 + rmz1)*z)/rmz2 + ((-1.0*r2 + rmz2)*z)/rmz1))/r2,0.0,0.0,0.0,(-0.25*b*Em*(-1.0 + 2.0*nu)*z*(r12 + r22 - 1.0*d*rmz2 - 2.0*z2))/(r1*rmz1*rmz2),0.0,0.0,0.0,
   (-0.25*b*Em*(-1.0 + 2.0*nu)*z*(r12 + r22 - 1.0*d*rmz1 - 2.0*z2))/(r2*rmz1*rmz2),0.0,0.0,0.0},
                {0.0,(0.5*b*Em*(-1.0 + 2.0*nu)*(-1.0*r2 + rmz2))/r2,0.0,0.0,0.0,(0.5*b*Em*(-1.0 + 2.0*nu)*rmz1)/r1,0.0,0.0,0.0,(-0.5*b*Em*(-1.0 + 2.0*nu)*rmz2)/r2,0.0,0.0,0.0 }
                };

            return intBD_dA;
        }

        protected override double[,] BMatrixForStrain(double x, double y, double z)
        {
            double z2 = z * z;
            double z3 = z2 * z;
            double z4 = z3 * z;
            double z5 = z4 * z;
            double z6 = z5 * z;
            double z7 = z6 * z;
            double z8 = z7 * z;

            double rmz2 = Math.Sqrt(r22 - z2);
            double rmz1 = Math.Sqrt(r12 - z2);
            double rmz22 = rmz2 * rmz2;
            double rmz12 = rmz1 * rmz1;

            double c1 = Math.Pow(-d + rmz1 + rmz2, 2.0);
            double c21 = Math.Pow(r12 - z2, 1.5);
            double c22 = Math.Pow(r22 - z2, 1.5);
            double c3 = Math.Sqrt((-r12 + z2) * (-r22 + z2));
            double c4 = Math.Sqrt((r12 - z2) / (r22 - z2));
            double c52 = Math.Pow(r2 - rmz2, 2.0);
            double c6 = Math.Pow(-d + r2 + rmz1, 2.0);
            double c7 = Math.Sqrt((r12 - z2) * (r24 - r22 * z2));
            double c81 = Math.Pow(d - rmz2, 2.0);
            double c82 = Math.Pow(d - rmz1, 2.0);
            double c9 = Math.Pow(-d + rmz1 + rmz2, -3.0);


            double[,] B = {
               { 0,0,0,0,1.0/b,0,0,0,0,0,0,0,0},

               { 0.0, 0.0, (-1.0*r2 + rmz2)/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, rmz1/(r1*(-1.0*d + rmz1 + rmz2)),0.0, (-1.0*z)/(-1.0*d + rmz1 + rmz2),0.0, (-1.0*rmz2)/(r2*(-1.0*d + rmz1 + rmz2)),0.0, z/(-1.0*d + rmz1 + rmz2) },
               
               { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, (z*(-1.0*d2*rmz2 - 1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(2.0*r22 + rmz2*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r1),
   (z*(-1.0*d2*rmz2 - 1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(2.0*r22 + rmz2*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3),0.0, 0.0, (z*(-1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(r12 + r22 - 1.0*rmz1*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r2),
   (z*(r22*rmz2 + r12*(rmz1 - 1.0*y) + r22*y - 1.0*d*(r12 + r22 - 1.0*rmz1*y - 2.0*z2) - 1.0*rmz1*z2 - 1.0*rmz2*z2))/(c1*c3)},

               { 0.0, 0.0, (-1.0*z*(r23 - 1.0*r22*rmz2 - 1.0*r12*(r2 + rmz1 - 1.0*y) - 1.0*r22*y + r2*rmz1*y + r2*rmz2*y + d*(r12 + r22 - 1.0*r2*rmz2 - 1.0*rmz1*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r2),0.0, 0.0, 0.0, 
   (z*(-1.0*d2*rmz2 - 1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(2.0*r22 + rmz2*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r1),rmz1/(r1*(-1.0*d + rmz1 + rmz2)),
   (c3*d*(-1.0*d + y) + d*(-2.0*rmz1 + rmz2)*z2 - 1.0*r22*(c3 - 2.0*d*rmz1 + rmz1*y + z2) + r12*(c3 - 1.0*rmz2*y + z2))/(c1*c3),0.0, (z*(-1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(r12 + r22 - 1.0*rmz1*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r2),
   (-1.0*rmz2)/(r2*(-1.0*d + rmz1 + rmz2)),(-1.0*c3*d*y + d*(rmz1 - 2.0*rmz2)*z2 + r22*(c3 - 1.0*d*rmz1 + rmz1*y + z2) - 1.0*r12*(c3 - 1.0*d*rmz2 - 1.0*rmz2*y + z2))/(c1*c3)},

               { 0.0, (-1.0*z*(r23 - 1.0*r22*rmz2 - 1.0*r12*(r2 + rmz1 - 1.0*y) - 1.0*r22*y + r2*rmz1*y + r2*rmz2*y + d*(r12 + r22 - 1.0*r2*rmz2 - 1.0*rmz1*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r2),0.0, 0.0, 0.0, 
   (z*(-1.0*d2*rmz2 - 1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(2.0*r22 + rmz2*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r1),0.0, 0.0, 0.0, 
   (z*(-1.0*r22*rmz2 - 1.0*r22*y + r12*(-1.0*rmz1 + y) + d*(r12 + r22 - 1.0*rmz1*y - 2.0*z2) + rmz1*z2 + rmz2*z2))/(c1*c3*r2),0.0, 0.0, 0.0},

               { 0.0, (-1.0*r2 + rmz2)/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, rmz1/(r1*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, (-1.0*rmz2)/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0}
            };
            return B;
        }

        protected override double[,] NMatrixForDisplacements(double x, double y, double z)
        {
            double z2 = z * z;
            double rmz2 = Math.Sqrt(r22 - z2);
            double rmz1 = Math.Sqrt(r12 - z2);


            double[,] N = {
                {0.0, ((r2 - 1.0*rmz2)*(rmz1 - 1.0*y))/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, x/b,(rmz1*(-1.0*d + rmz2 + y))/(r1*(-1.0*d + rmz1 + rmz2)),
                    0.0, 0.0, 0.0, (rmz2*(rmz1 - 1.0*y))/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0},
                { 0.0, 0.0, ((r2 - 1.0*rmz2)*(rmz1 - 1.0*y))/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 0.0, 0.0, 
                    (rmz1*(-1.0*d + rmz2 + y))/(r1*(-1.0*d + rmz1 + rmz2)),0.0, 
                    (-1.0*(-1.0*d + rmz2 + y)*z)/(-1.0*d + rmz1 + rmz2),0.0, (rmz2*(rmz1 - 1.0*y))/(r2*(-1.0*d + rmz1 + rmz2)),0.0, 
                    ((-1.0*rmz1 + y)*z)/(-1.0*d + rmz1 + rmz2) },
                { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, (rmz1*(-1.0*d + rmz2 + y))/(r1*(-1.0*d + rmz1 + rmz2)),(rmz1*(-1.0*d + rmz2 + y))/(-1.0*d + rmz1 + rmz2),
                    0.0, 0.0, (rmz2*(rmz1 - 1.0*y))/(r2*(-1.0*d + rmz1 + rmz2)), (rmz2*(-1.0*rmz1 + y))/(-1.0*d + rmz1 + rmz2)}
            };
            return N;
        }

        protected override double[,] CalculateMaterialStiffness(double[] intPtStateVariables)
        {
            //pull out the state variables that belong to the node

            double Damage = intPtStateVariables.Length == 0 ? 0 : intPtStateVariables[0];
            double Ep = E0 * (1.0 - Damage) / ((1.0 + nu) * (1.0 - 2.0 * nu));
            double[,] D = {
                { 1.0 - nu, nu, nu, 0, 0, 0},
                {nu, 1.0 - nu, C33Coefficient * nu, 0, 0, 0},
                {C33Coefficient * nu, nu, 1.0 - nu, 0, 0, 0},
                {0, 0, 0, (1.0 - 2.0*nu)/ 2.0, 0, 0},
                { 0, 0, 0, 0, (1.0 - 2.0*nu)/ 2.0, 0},
                {0, 0, 0, 0, 0, (1.0 - 2.0* nu)/ 2.0}
            };
            return MatrixMath.ScalarMultiply(Ep, D);
        }

        public override void WriteFirstIterationOutput(StreamWriter dataWrite)
        {
            dataWrite.Write( E0 + "," + nu + "," + (zIntPts.Length-1));
            dataWrite.Write(",");
            failureCriteria.WriteOutput(dataWrite);

        }

        public static MatrixModel1[] ReadFirstIterationOutput(string totalString, double r1, double r2, double d, double b, double[] zBoundsTopToBottom)
        {
            string[] splitString = totalString.Split(',');

            double E = Convert.ToDouble(splitString[0]);
            double nu = Convert.ToDouble(splitString[1]);
            int nIntPts = Convert.ToInt32(splitString[2]);
            string failureTypeNam = splitString[3];
            string failureConst = splitString[4];

            failureConst += "/" + E.ToString();
            IFailureCriteria failureCrit = FailureTheories.CreateFailureCriteria.CreateFailureCriteriaFromInput(failureTypeNam, failureConst);
            FailureCritForZIntegratedMatrix izfc = (FailureCritForZIntegratedMatrix)failureCrit;

            MatrixModel1 topMatrix = new MatrixModel1(E, nu, r1, r2, d, b, zBoundsTopToBottom[0], zBoundsTopToBottom[1], true, nIntPts, izfc);
            MatrixModel1 botMatrix = new MatrixModel1(E, nu, r1, r2, d, b, zBoundsTopToBottom[2], zBoundsTopToBottom[3], false, nIntPts, izfc);
            return new MatrixModel1[2] {topMatrix, botMatrix};
        }
        

        #endregion

        #region Private Methods

        #endregion

        #region Static Methods

        
        #endregion

    }
}
