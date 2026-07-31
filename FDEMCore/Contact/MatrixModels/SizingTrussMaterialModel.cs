/*
 * Thin MaterialModel adapter that lets FToFSizingSpring_EqArea reuse the same
 * IFailureCriteria library as FToFWithMatrix, without any of the z-integrated
 * continuum/closed-form machinery. This element is a simple equivalent-area
 * truss with a single axial stress and a single shear stress (no bending).
 */
using System;
using System.IO;
using FDEMCore.Contact.FailureTheories;

namespace FDEMCore.Contact.MatrixModels
{
    public class SizingTrussMaterialModel : MaterialModel
    {
        public const string Name = "SizingTrussEqArea";

        private readonly double xSectArea;

        /// <param name="xSectArea">Equivalent cross-sectional area of the sizing truss</param>
        /// <param name="failureCriteria">Failure criterion used to evaluate breakage</param>
        public SizingTrussMaterialModel(double xSectArea, IFailureCriteria failureCriteria)
            : base(0, 0, 0, 0, 0, 0, failureCriteria)
        {
            this.xSectArea = xSectArea;
        }

        /// <summary>
        /// q = {normForceMag, tanForceMag}
        /// </summary>
        public override double[] CalculateStress(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double sAxial = q[0] / xSectArea;
            double sShear = q[1] / xSectArea;

            //Sxx, Syy, Szz, Syz, Sxz, Sxy
            return new double[6] { sAxial, 0, 0, 0, 0, sShear };
        }

        public override double[] CalculateStrain(double x, double y, double z, double[] q, double[] stateVariables)
        {
            throw new NotSupportedException("Strain is not used by the equivalent-area sizing spring failure check.");
        }

        public override double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables)
        {
            return new double[3];
        }

        public override double[,] CalculateStiffness(double[] stateVariables)
        {
            throw new NotSupportedException("Stiffness for the sizing spring is computed analytically by CalculateSizingKF.");
        }

        /// <summary>
        /// q = {normForceMag, tanForceMag}
        /// </summary>
        public override bool IsThereFailure(double[] q, ref double[] stateVariables)
        {
            if (failureCriteria is NoFailure)
            {
                return false;
            }

            double f = failureCriteria.FailureFunction(0, 0, 0, q, ref stateVariables, this);
            return f >= 0.0;
        }

        //Brittle for now: any failure means totally broken. When progressive damage is
        //added, this should inspect the damage state variable(s) instead.
        public override bool IsItTotallyBroken(double[] stateVariables)
        {
            return false;
        }

        public override double[] CalculateIntegralOfStressOverVolume(double[] q, double[] stateVariables)
        {
            return new double[6];
        }

        public override void WriteFirstIterationOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + ",");
            failureCriteria.WriteOutput(dataWrite);
        }
    }
}
