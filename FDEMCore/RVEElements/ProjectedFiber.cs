/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 5/13/2014
 * Time: 1:21 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RandomMath;

namespace FDEMCore
{
	/// <summary>
	/// Description of ProjectedFiber.
	/// </summary>

	[SerializableAttribute] //This allows to make a deep copy fast
	public class ProjectedFiber:IPoint
	{
		private double [] position;
		private double [] velocity;
		private int [] cellWallIndices;


		public int[] CellWallIndices{
            get { return cellWallIndices; }
			set { cellWallIndices = value; }
        }
		public double [] CurrentPosition{
			get { return position; }
			set { position = value; }
		}
		public double [] CurrentVelocity {
			get { return velocity; }
			set { velocity = value; }
		}
		public double [] OPeriodicProjection;
		
		
		public ProjectedFiber(double [] oPeriodicProjection, Fiber f, CellBoundary cb, int[] cellWallIndices)
		{
			this.cellWallIndices = cellWallIndices;
			OPeriodicProjection = oPeriodicProjection;
			position = VectorMath.Add(f.CurrentPosition, cb.UndefXtoDefx(oPeriodicProjection));
			velocity = VectorMath.Add(f.CurrentVelocity, cb.UndefVtoDefv(oPeriodicProjection));
		}
		
		public ProjectedFiber(double [] inPosition, double [] inVelocity, double [] oPeriodicProjection, int[] cellWallIndices)
		{
			this.cellWallIndices = cellWallIndices;
			OPeriodicProjection = oPeriodicProjection;
			position = inPosition;
			velocity = inVelocity;
		}
	}
}
