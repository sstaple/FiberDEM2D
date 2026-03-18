using RandomMath;
using System;
using FDEMCore.Contact.MatrixModels;
using System.IO;

namespace FDEMCore.Contact.FailureTheories
{
    public interface IFailureCriteria
    {
        public int NStateVariables { get; }
        /// <summary>
        /// This returns the failure function F, which is 0 at failure, >0 past failure, <0 before failure.  For single parameter failure theories like 
        /// stress, at current stress S and with failure stress Sf, F=S/Sf.
        /// </summary>
        /// <returns>F</returns>
        double FailureFunction( double x, double y, double z, double[] qm, ref double[] stateVariables, MaterialModel materialModel);
        void WriteOutput(StreamWriter dataWrite);
    }

    public static class CreateFailureCriteria
    {
        public static IFailureCriteria CreateFailureCriteriaFromInput(string criteriaName, string constants)
        {
            IFailureCriteria failureCriteria = new VonMises(0.0); //Have to initialize it to something, but this is just dummy
            string[] constantsArray = constants.Split('/');

            switch (criteriaName)
            {
                case VonMises.Name:
                    failureCriteria = new VonMises(Convert.ToDouble(constantsArray[0]));
                    break;
                case MaxPrincipalStrain.Name:
                    failureCriteria = new MaxPrincipalStrain(Convert.ToDouble(constantsArray[0]));
                    break;
                case MaxPrincipalStress.Name:
                    failureCriteria = new MaxPrincipalStress(Convert.ToDouble(constantsArray[0]));
                    break;
                case FractureEnergyPrincipalStress.Name:
                    failureCriteria = new FractureEnergyPrincipalStress(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]));
                    break;
                case MaxPrincStressWithDamage.Name:
                    if (constantsArray.Length == 3)
                    {
                        failureCriteria = new MaxPrincStressWithDamage(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]));
                    }
                    else
                    {
                        failureCriteria = new MaxPrincStressWithDamage(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]), Convert.ToDouble(constantsArray[2]));
                    }

                    break;
                case MaxPrincStressWithDamage2.Name:
                    if (constantsArray.Length == 3)
                    {
                        failureCriteria = new MaxPrincStressWithDamage2(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]));
                    }
                    else
                    {
                        failureCriteria = new MaxPrincStressWithDamage2(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]), Convert.ToDouble(constantsArray[2]));
                    }

                    break;
                case MaxPrincStrainWithDamage.Name:
                    if (constantsArray.Length == 3)
                    {
                        failureCriteria = new MaxPrincStrainWithDamage(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]), Convert.ToDouble(constantsArray[2]));
                    }
                    else
                    {
                        failureCriteria = new MaxPrincStrainWithDamage(Convert.ToDouble(constantsArray[0]), Convert.ToDouble(constantsArray[1]), Convert.ToDouble(constantsArray[3]), Convert.ToDouble(constantsArray[2]));
                    }
                    break;
                case MaxPrincStressZIntDNoDamage.Name:
                     failureCriteria = new MaxPrincStressZIntDNoDamage(Convert.ToDouble(constantsArray[0]));
                    break;
                case NoFailure.Name:
                    failureCriteria = new NoFailure();
                    break;
                default:
                    throw new Exception($"Failure theory {criteriaName} not recognized");

            }

            return failureCriteria;
        }
    }

    public class VonMises : IFailureCriteria
    {
        int IFailureCriteria.NStateVariables { get { return 0; } }
        private double critStress;
        public const string Name = "VonMises";

        public VonMises(double criticalStress)
        {
            critStress = criticalStress;
            
        }

        public double FailureFunction(double x, double y, double z, double[] qTotal,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double[] s = materialModel.CalculateStress(x, y, z, qTotal, stateVariables);

            double vm = Math.Sqrt(0.5 * (Math.Pow((s[0] - s[1]), 2) + Math.Pow((s[1] - s[2]), 2)
               + Math.Pow((s[2] - s[0]), 2) + 6.0 * (s[3] * s[3] + s[4] * s[4] + s[5] * s[5])));
            double f = (vm / critStress - 1);

            return f;
        }
    public void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + critStress);

        }
    }

    public class MaxPrincipalStrain : IFailureCriteria
    {
        int IFailureCriteria.NStateVariables { get { return 0; } }
        private double critStrain;
        public const string Name = "MaxPrincipalStrain";

        public MaxPrincipalStrain(double criticalStrain)
        {
            critStrain = criticalStrain;
        }

        public double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double[] s = materialModel.CalculateStrain(x, y, z, qm, stateVariables);

            //put strain in tensor form: Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY:
            double[,] strainTensor = new double[3, 3] { { s[0], s[5] / 2.0, s[4] / 2.0 }, { s[5] / 2.0, s[1], s[3] / 2.0 }, { s[4] / 2.0, s[3] / 2.0, s[2] } };

            double[] princStrains = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(strainTensor);
            double maxPrincStrain = princStrains[0];

            double f = (maxPrincStrain / critStrain - 1);

            return f;
        }

        public void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + critStrain);

        }
    }

    public class MaxPrincipalStress : IFailureCriteria
    {
        int IFailureCriteria.NStateVariables { get { return 0; } }
        private double criticalStress;
        public const string Name = "MaxPrincipalStress";

        public MaxPrincipalStress(double criticalStress)
        {
            this.criticalStress = criticalStress;
        }

        public double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double[] s = materialModel.CalculateStress(x, y, z, qm, stateVariables);

            //put strain in tensor form: Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY:
            double[,] stressTensor = new double[3, 3] { { s[0], s[5], s[4] }, { s[5], s[1], s[3] }, { s[4], s[3], s[2] } };

            double[] princStresses = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(stressTensor);
            double maxPrincStress = princStresses[0];

            double f = (maxPrincStress / criticalStress - 1);

            return f;
        }
        public void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + criticalStress);

        }
    }

    public class NoFailure : IFailureCriteria
    {
        int IFailureCriteria.NStateVariables { get { return 0; } }
        public const string Name = "NoFailure";
        public NoFailure()
        {
        }

        public double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            return 0;
        }
        public void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name);

        }
    }

    /// <summary>
    /// This takes in the fracture energy, then scales the priniciple stress to the location between two fibers.
    /// </summary>
    public class FractureEnergyPrincipalStress : IFailureCriteria
    {
        int IFailureCriteria.NStateVariables { get { return 0; } }
        private double criticalFractureEnergy;
        private double modulus;
        public const string Name = "FractureEnergyPrincipalStress";

        public FractureEnergyPrincipalStress(double criticalFractureEnergy, double modulus)
        {
            this.criticalFractureEnergy = criticalFractureEnergy;
            this.modulus = modulus;
        }

        public double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double width = materialModel.CalculateLengthBetweenFibers(z);

            double sCritical = Math.Sqrt(2.0 * criticalFractureEnergy * modulus / width);

            MaxPrincipalStress myMaxPStress = new MaxPrincipalStress(sCritical);

            double f = myMaxPStress.FailureFunction(x, y, z, qm, ref stateVariables, materialModel);

            return f;
        }
        public void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + criticalFractureEnergy + "/" + modulus);

        }
    }


    public abstract class FailureCritForZIntegratedMatrix : IFailureCriteria
    {
        protected int nStateVariables;
        int IFailureCriteria.NStateVariables { get { return nStateVariables; } }
        public int CurrentIntPt;
        public int CurrZ_0Left_1Right_2Middle;

        public FailureCritForZIntegratedMatrix(){}
        public abstract double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel);
        public abstract void WriteOutput(StreamWriter dataWrite);
    }
    public class MaxPrincStressZIntDNoDamage : FailureCritForZIntegratedMatrix
    {
        public double Strength;
        public const string Name = "MaxPrincStressZIntDNoDamage";

        public MaxPrincStressZIntDNoDamage(double strength)
        {
            nStateVariables = 0;
            this.Strength = strength;
        }

        public override double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            ZIntegratedMatrixModel zMatModel = materialModel as ZIntegratedMatrixModel;
            double[] s = zMatModel.CalculateStress(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm, stateVariables);

            //put stress in tensor form:Tensor Form:
            double[,] stressTensor = MatrixMath.VoigtVectorToTensor(s);

            //find max principle (assumes max is in the first spot
            double[] princStress = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(stressTensor);
            double maxPrincStress = princStress[0];
            if (maxPrincStress >= Strength)
            {
                stateVariables[0] = 1.0;
            }
            return (maxPrincStress / Strength - 1); ;
        }
        public override void  WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + Strength);
        }
    }

    public class MaxPrincStressWithDamage : FailureCritForZIntegratedMatrix
    {
        public const string Name = "MaxPrincStressWithDamage";
        private double criticalFractureEnergy;
        private double criticalStress;
        private double damageAccelerationCoefficient; //1 = no crack acceleration, 2 = crack is doubled, 0.5 = crack is halved

        public MaxPrincStressWithDamage(double criticalFractureEnergy, double strength):this(criticalFractureEnergy, strength, 1.0)
        {
        }
        public MaxPrincStressWithDamage(double criticalFractureEnergy, double strength, double damageAccelerationCoefficient)
        {
            this.criticalFractureEnergy = criticalFractureEnergy;
            this.criticalStress = strength;
            this.damageAccelerationCoefficient = damageAccelerationCoefficient;
            nStateVariables = 2;
        }

        public override double FailureFunction(double x, double y, double z, double[] qm, 
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double D = stateVariables[0];
            double criticalStrain = stateVariables[1];

            if (D >= 1.0)
            {
                //Don't do any of this garbage if it's already dead!
            }
            else
            {
                //Cast the material model:
                ZIntegratedMatrixModel zMatModel = materialModel as ZIntegratedMatrixModel;

                double length = zMatModel.CalculateLengthBetweenFibers(CurrentIntPt);
                double currCriticalStress = criticalStress * (1 - D);
                double failureStrain = 2.0 * criticalFractureEnergy / (criticalStress * length);
                //double Dinit = D;

                #region calculate max principal stress and strain

                double[] e = zMatModel.CalculateStrain(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm);
                double[] s = zMatModel.CalculateStress(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm, stateVariables);

                //put strain in tensor form: Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY:
                double[,] stressTensor = MatrixMath.VoigtVectorToTensor(s);
                double[,] strainTensor = MatrixMath.VoigtVectorToTensorStrain(e);

                //find max principle (assumes max is in the first spot
                double maxPrincStress = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(stressTensor)[0];
                double maxPrincStrain = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(strainTensor)[0];

                #endregion

                //If it is pre-failure
                if (maxPrincStress <= currCriticalStress) 
                {
                    //If the fibers are so far apart that they use up the energy before reaching the critical stress...
                    if (D.Equals(0.0) && criticalFractureEnergy < (0.5 * maxPrincStrain * maxPrincStress * length))
                    {
                        D = 1.0; //kill it!!!
                    }
                }
                //If it is failed....
                else
                {
                    //If it hasn't started failing yet, save the max principal strain
                    if (D.Equals( 0.0))
                    {
                        criticalStrain = maxPrincStrain * criticalStress / maxPrincStress;
                    }

                    //uble DTemp1= (maxPrincStrain - criticalStrain) / (failureStrain - criticalStrain);
                    //uble E110 = criticalStress / criticalStrain;
                    //uble EIT = 1.0 / (1.0 / E110 - failureStrain / criticalStress);
                    //uble DTemp = 1.0 + EIT * (criticalStrain - maxPrincStrain) / (E110 * maxPrincStrain);
                    double DTemp = 1.0 - (1.0 - failureStrain / maxPrincStrain) / (criticalStrain - failureStrain) * criticalStrain;
                    //Set it to 1 if it's over
                    //Grow the damage if there is new damage.  Also, accelerate the crack by the damage coefficient
                    D = D > DTemp ? D : (DTemp - D) * damageAccelerationCoefficient + D;
                    D = D > 1.0 ? 1.0 : D;

                    /*double[] dumbTempStress = zMatModel.CalculateStress(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm, new double[] { D, criticalStrain });
                    double[,] dumbstressTensor = MatrixMath.VoigtVectorToTensor(dumbTempStress);
                    double dumbmaxPrincStress = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(dumbstressTensor)[0];
                    bool t = true;*/
                }

            }

            stateVariables[0] = D;
            stateVariables[1] = criticalStrain;
            return D;
        }
        public override void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + criticalFractureEnergy + "/" + criticalStress + "/" + damageAccelerationCoefficient);
        }
    }

    public class MaxPrincStrainWithDamage : FailureCritForZIntegratedMatrix
    {
        public const string Name = "MaxPrincStrainWithDamage";
        public double CriticalFractureEnergy;
        public double CriticalStrain;
        public double Strength;
        public double Modulus;
        public double DamageAccelerationCoefficient; //1 = no crack acceleration, 2 = crack is doubled, 0.5 = crack is halved

        public MaxPrincStrainWithDamage(double criticalFractureEnergy, double criticalStrain, double modulus)
        {
            this.CriticalFractureEnergy = criticalFractureEnergy;
            this.CriticalStrain = criticalStrain;
            this.Strength = modulus * criticalStrain;
            this.Modulus = modulus;
            DamageAccelerationCoefficient = 1.0;
            nStateVariables = 1;
        }
        public MaxPrincStrainWithDamage(double criticalFractureEnergy, double criticalStrain, double modulus, double damageAccelerationCoefficient)
        {
            this.CriticalFractureEnergy = criticalFractureEnergy;
            this.CriticalStrain = criticalStrain;
            this.Strength = modulus * criticalStrain;
            this.Modulus = modulus;
            this.DamageAccelerationCoefficient = damageAccelerationCoefficient;
            nStateVariables = 1;

        }

        public override double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double D = stateVariables[0];

            if (D.Equals(1.0))
            {
                //Don't do any of this garbage if it's already dead!
            }
            else
            {
                //Cast the material model as:
                ZIntegratedMatrixModel zMatModel = materialModel as ZIntegratedMatrixModel;

                double E0 = Modulus;
                double fractureEnergy0 = CriticalFractureEnergy;
                double strength0 = Strength;

                double Dinitial = D;
                double length = zMatModel.CalculateLengthBetweenFibers(CurrentIntPt);
                double tempStrength0 = strength0;

                // check if the softening slope is negative.If it is, scale the strength

                if (length > (2.0 * E0 * fractureEnergy0 / Math.Pow(strength0, 2.0)))
                {
                    //try not changing the strength if it's too long...
                    tempStrength0 = Math.Sqrt(2.0 * fractureEnergy0 * E0 / length);
                }

                //First, Calculate initial Values
                double criticalStrain0 = tempStrength0 / E0;
                double maximumStrain = 2.0 * fractureEnergy0 / (length * tempStrength0);

                //First, update the strength, critical strain, and energy
                double E = E0 * (1.0 - D);
                double criticalStrain = maximumStrain / (1.0 - E / tempStrength0 * (criticalStrain0 - maximumStrain));

                #region max principal strain
                
                // find the maximum principle strain
                double[] s = zMatModel.CalculateStrain(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm);

                //put strain in tensor form: Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY:
                double[,] strainTensor = MatrixMath.VoigtVectorToTensorStrain(s);
                

                //find max principle (assumes max is in the first spot
                double[] princStrain = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(strainTensor);
                double maxPrincStrain = princStrain[0];
                
                #endregion


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
                    D = D > DTemp ? D : (DTemp - D) * DamageAccelerationCoefficient + D;
                    D = D > 1.0 ? 1.0 : D;

                }
                else
                {
                    // it's dead!
                    D = 1.0;
                }
            }

            stateVariables[0] = D;
            return D;
        }
        public override void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + CriticalFractureEnergy + "/" + CriticalStrain + "/" + Modulus + "/" + DamageAccelerationCoefficient);
        }
    }

    /// <summary>
    /// Just like MaxPrincStressWithDamage, but where the strength is not scaled for fibers that are far apart
    /// </summary>
    public class MaxPrincStressWithDamage2 : FailureCritForZIntegratedMatrix
    {
        public const string Name = "MaxPrincStressWithDamage2";
        private double criticalFractureEnergy;
        private double criticalStress;
        private double damageAccelerationCoefficient; //1 = no crack acceleration, 2 = crack is doubled, 0.5 = crack is halved

        public MaxPrincStressWithDamage2(double criticalFractureEnergy, double strength) : this(criticalFractureEnergy, strength, 1.0)
        {
        }
        public MaxPrincStressWithDamage2(double criticalFractureEnergy, double strength, double damageAccelerationCoefficient)
        {
            this.criticalFractureEnergy = criticalFractureEnergy;
            this.criticalStress = strength;
            this.damageAccelerationCoefficient = damageAccelerationCoefficient;
            nStateVariables = 2;
        }

        public override double FailureFunction(double x, double y, double z, double[] qm,
            ref double[] stateVariables, MaterialModel materialModel)
        {
            double D = stateVariables[0];
            double criticalStrain = stateVariables[1];

            if (D >= 1.0)
            {
                //Don't do any of this garbage if it's already dead!
            }
            else
            {
                //Cast the material model:
                ZIntegratedMatrixModel zMatModel = materialModel as ZIntegratedMatrixModel;

                double length = zMatModel.CalculateLengthBetweenFibers(CurrentIntPt);
                double currCriticalStress = criticalStress * (1 - D);
                double failureStrain = 2.0 * criticalFractureEnergy / (criticalStress * length);
                //double Dinit = D;

                #region calculate max principal stress and strain

                double[] e = zMatModel.CalculateStrain(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm);
                double[] s = zMatModel.CalculateStress(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm, stateVariables);

                //put strain in tensor form: Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY:
                double[,] stressTensor = MatrixMath.VoigtVectorToTensor(s);
                double[,] strainTensor = MatrixMath.VoigtVectorToTensorStrain(e);

                //find max principle (assumes max is in the first spot
                double maxPrincStress = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(stressTensor)[0];
                double maxPrincStrain = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(strainTensor)[0];

                #endregion

                
                //If it is failed....
                if (maxPrincStress > currCriticalStress)
                {
                    //If it hasn't started failing yet, save the max principal strain
                    if (D.Equals(0.0))
                    {
                        criticalStrain = maxPrincStrain * criticalStress / maxPrincStress;
                    }

                    //uble DTemp1= (maxPrincStrain - criticalStrain) / (failureStrain - criticalStrain);
                    //uble E110 = criticalStress / criticalStrain;
                    //uble EIT = 1.0 / (1.0 / E110 - failureStrain / criticalStress);
                    //uble DTemp = 1.0 + EIT * (criticalStrain - maxPrincStrain) / (E110 * maxPrincStrain);
                    double DTemp = 1.0 - (1.0 - failureStrain / maxPrincStrain) / (criticalStrain - failureStrain) * criticalStrain;
                    //Set it to 1 if it's over
                    //Grow the damage if there is new damage.  Also, accelerate the crack by the damage coefficient
                    D = D > DTemp ? D : (DTemp - D) * damageAccelerationCoefficient + D;
                    D = D > 1.0 ? 1.0 : D;

                    /*double[] dumbTempStress = zMatModel.CalculateStress(CurrentIntPt, CurrZ_0Left_1Right_2Middle, qm, new double[] { D, criticalStrain });
                    double[,] dumbstressTensor = MatrixMath.VoigtVectorToTensor(dumbTempStress);
                    double dumbmaxPrincStress = MatrixMath.EigenvaluesOf3by3SymmetricMatrix(dumbstressTensor)[0];
                    bool t = true;*/
                }

            }

            stateVariables[0] = D;
            stateVariables[1] = criticalStrain;
            return D;
        }
        public override void WriteOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(Name + "," + criticalFractureEnergy + "/" + criticalStress + "/" + damageAccelerationCoefficient);
        }
    }

}
