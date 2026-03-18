/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/11/2013
 * Time: 11:22 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace FDEMCore
{
	/// <summary>
	/// A grid is a 2-D zone where the space is broken up into squares, and x, y coordinates can be translated into squares/zones in the grid.  Make sure to make the zone big enough for the final state of
	/// the RVE (expansion of the walls, rotation, etc).  Grid is numbered 0,0 at the bottom left, and is numbered up from there
	/// </summary>

	[SerializableAttribute] //This allows to make a deep copy fast
	public class Grid
	{
		#region Private Members
		private double xMin;
		private double yMin;
		private double ly;
		private double lx;//total length in x and y
		private int nx;
		private int ny;  //number of cells in x and y
		private double dy; //size of cell in x and y
		private double dx;
		#endregion
		
		#region Public Members
		
		public double YMin {
			get { return yMin; }
		}
		public double XMin {
			get { return xMin; }
		}
		public int Ny {
			get { return ny; }
		}
		public int Nx {
			get { return nx; }
		}
		public double Dy {
			get { return dy; }
		}
		public double Dx {
			get { return dx; }
		}
		#endregion
		
		#region Constructors
		public Grid(double XMin, double YMin, double LengthY, double LengthX, double CellSize)
		{
			xMin = XMin;
			yMin = YMin;
			ly = LengthY;
			lx = LengthX;
			nx = (int)Math.Floor(lx / CellSize);
			ny = (int)Math.Floor(ly / CellSize);
			dy = ly / ny;
			dx = lx / nx;
		}
		#endregion
		
		#region Public Methods
		public int [] ConvertToZoneIndices(double [] x, out bool wasSuccessful){
			int [] xint = new int[2];
			wasSuccessful = false;

			if(x[1] < xMin || x[1] > (xMin + lx) || x[2] < YMin || x[2] > (YMin + ly))
            {
            }
            else
            {
				xint[0] = (int)Math.Floor((x[1] - xMin) / dx); //TODO made this 2 and one because I am not considering the 1-direction of the fibers 
				xint[1] = (int)Math.Floor((x[2] - yMin) / dy);
				wasSuccessful = true;
			}
			
			//if ((xint[0] > (nx - 1)) || (xint[1] > (ny - 1)) || (xint[0] < 0d) || (xint[1] < 0d)) {
			//	//xint = new int[2]{0, 0};
			//	throw new InvalidOperationException("Point Out of Grid");
			//}
			return xint;
		}
		
		#endregion
		
	}
}