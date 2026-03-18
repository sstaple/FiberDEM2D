/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 25-Jul-13
 * Time: 3:55 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using RandomMath;
using System.IO;
using System.Runtime.InteropServices;

namespace FDEMCore
{
	/// <summary>
	/// Description of CellWall.
               	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class CellWall
	{
		#region Private Members
		private double [] oNormDir;
		private double [] currNormDir;
		private double [] oCenterPoint;  //Original center point
		private double [] currCenterPoint; 
		private double [] oPeriodicProjection;//Original Vector that transfers a point to a projection on the opposite wall in the reference frame
		private CellBoundary cb; //This is the cell boundary so that one can access the undefToDef function
		private List<double []> outCenterPt;
		private List<double []> outNormal;
		private int cellWallIndex;
		private BoundaryType boundaryType;
        #endregion

        #region Public Members
        public double [] NormDir {
			get { return currNormDir; }
		}
		public double [] PeriodicProjection {
			get { return oPeriodicProjection; }
		}
		public BoundaryType BoundaryType {
			get { return boundaryType; }
        }

        #endregion

        #region Constructors
        public CellWall(double [] initialCenter, double [] initialNorm, double [] initialProjection, CellBoundary currBoundary, int cellWallIndex, BoundaryType boundaryType)
		{
			this.boundaryType = boundaryType;
            this.cellWallIndex = cellWallIndex;
			cb = currBoundary;
			oCenterPoint = initialCenter;
			oNormDir = initialNorm;
			oPeriodicProjection = initialProjection;
			outCenterPt = new List<double[]>();
			outNormal = new List<double[]>();
			
			Update();
			SaveTimeStep(0); //Save the first step!!!
		}
		#endregion
		
		#region Public Methods
		//This is really for debugging: make sure the center stuff lines up with the def gradient
		
		public void Update(){
			
			currCenterPoint = cb.UndefXtoDefx(oCenterPoint);
			
			currNormDir = cb.RotateNormals(oNormDir);

			
			//VectorMath.NormalizeVector(ref currNormDir);
		}

		public void SaveTimeStep(int i){
            if (i == 0 && outCenterPt.Count >= 1)
            {
				outCenterPt[0] = VectorMath.DeepCopy(currCenterPoint);
				outNormal[0] = VectorMath.DeepCopy(currNormDir);
			}
            else { 
			outCenterPt.Add(VectorMath.DeepCopy(currCenterPoint));
			outNormal.Add(VectorMath.DeepCopy(currNormDir));
			}
		}
		
		public bool CheckContact(Fiber f){
			
			bool isThereContact = false;
			//first, get the fiber coor in the system where origin is boundary center
			double [] wallToFiber = VectorMath.Subtract(f.CurrentPosition, currCenterPoint);
			
			//Take the dot product, wihich will give you the distance to the wall
			double d = VectorMath.Dot(wallToFiber, currNormDir);
			
			if (d < f.Radius) { //Contacting the wall
				
				isThereContact = true;

				if(boundaryType == BoundaryType.Periodic){

                    //If the boundary is periodic, then we need to project the fiber to the opposite wall
                    bool switchOriginalAndProjected = (d < 0.0);
                    f.AddProjectedFiber(oPeriodicProjection, switchOriginalAndProjected, new int[] { cellWallIndex });
                }
				else
                { //Solid wall boundary
                  // penalty + optional normal damping
                    double penetration = f.Radius - d; // > 0 here
                    
					if (penetration <= 0.0) return isThereContact; // no contact, return early
                    
					double[] n = currNormDir;          // already normalized by boundary updates
                    double E2 = f.Modulus2;
                    double nu23 = f.Nu23;
                    double Estar = 1.0 / (((1.0 - nu23 * nu23) / E2) + ((1.0 - nu23 * nu23) / E2));

                    double l = f.CurrentLength;     // current axial length used for your contact spring
                    double kWall = Math.PI * 0.25 * Estar * l;

                    // If you'd rather make the wall behave like a rigid plane (vs "another fiber"):
                    // kWall *= 2.0;

                    // Elastic penalty
                    double[] F = VectorMath.ScalarMultiply(kWall * penetration, n);

                    // Optional normal damping (kept exactly as your current pattern)
                    double vN = VectorMath.Dot(f.CurrentVelocity, n);
					double C = f.GlobalDampingCoeff;
                    double[] D = VectorMath.ScalarMultiply(-C * vN, n);
                    F = VectorMath.Add(F, D);

                    // Push into the usual force accumulator/integrator
                    f.CurrentForces.Add(F);
                }
            }
            return isThereContact;
        }
		
		public double [] FindCoordinateOnWall(double dFromCenter, double [] dir){
			
			double [] coor;
			//First, find the normal vector on the plane in the given direction
			double [] inNormDir = VectorMath.ScalarMultiply(VectorMath.Dot(dir, currNormDir), currNormDir);
			double [] projectionOnPlane = VectorMath.Subtract(dir, inNormDir);
			double [] normOnPlane = VectorMath.DeepCopy(projectionOnPlane);
			VectorMath.NormalizeVector(ref normOnPlane);
			coor = VectorMath.Add(VectorMath.ScalarMultiply(dFromCenter, normOnPlane), currCenterPoint);
			
			return coor;
		}
		
		public void WriteOutput(int i, StreamWriter dataWrite)
		{
			dataWrite.WriteLine(outCenterPt[i][0] + "," + outCenterPt[i][1] + "," + outCenterPt[i][2]
			                    + "," +  outNormal[i][0] + "," + outNormal[i][1] + "," + outNormal[i][2]);
		}
		
		#endregion

		#region Private Methods
		
		#endregion
	}
}
