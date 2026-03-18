/*
 * Created by SharpDevelop.
 * User: Admin
 * Date: 9/1/2009
 * Time: 11:09 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using ZedGraph;

using System.Linq;

namespace SinglePlot
{
	/// <summary>
	/// Description of Class1.
	/// </summary>
	public class PlotMaker
	{
		private string Title;
		private string XTitle;
		private string YTitle;
		private Color[] PlotColors;
		private int colorNumber = 0;
		
		public PlotMaker(string title, string xTitle, string yTitle)
		{
			Title = title;
			XTitle = xTitle;
			YTitle = yTitle;
			
			PlotColors = new Color[18]{Color.Blue, Color.Red, Color.SkyBlue,
				Color.Firebrick, Color.DarkBlue, Color.LightCoral,
				Color.ForestGreen, Color.LimeGreen, Color.GreenYellow,
				Color.Purple, Color.DarkViolet, Color.Magenta,
				Color.Black, Color.DimGray, Color.Silver,
				Color.Teal, Color.LightSeaGreen, Color.Turquoise};
		}
		
		///<summary>
		/// The DataArray has the first row x, then the next rows y, then x, then y, etc.
		/// DataLabels should refer to the names of the y curves
		/// </summary>
		public ZedGraph.GraphPane Plot(GraphPane myPane, double [,] DataArray, string [] DataLabels)
		{
			
			myPane.CurveList.Clear();
			
			// Set the title and axis labels
			myPane.Title.Text = Title;
			myPane.XAxis.Title.Text = XTitle;
			myPane.YAxis.Title.Text = YTitle;
			
			int n = DataArray.GetLength(0)/2;
			
			for (int i = 0; i < n; i++) {
				
				PointPairList list = new PointPairList();;
				
				for (int j = 0; j < DataArray.GetLength(1); j++) {
					
					
					list.Add(new PointPair(DataArray[2*i,j],DataArray[2*i+1,j]));
					
				}
				
				LineItem curve = myPane.AddCurve(DataLabels[i],
				                                 list,
				                                 PlotColors[i]);
				
				//curve.Symbol.IsVisible = true;
				curve.Symbol.Size = 3.0F;
				curve.Line.Width = 2.0F;
			}
			
			
			// Fill the background of the chart rect and pane
			myPane.Chart.Fill = new Fill( Color.White);

			// Show the legend
			myPane.Legend.IsVisible = true;
			myPane.Legend.Border.IsVisible = false;
			myPane.Legend.Position = ZedGraph.LegendPos.InsideTopLeft;
			myPane.Legend.Fill.IsVisible = false;
			
			// turn off the opposite tics so the Y tics don't show up on the Y2 axis
			myPane.YAxis.MajorTic.IsOpposite = false;
			myPane.YAxis.MinorTic.IsOpposite = false;
			myPane.XAxis.MajorTic.IsOpposite = false;
			myPane.XAxis.MinorTic.IsOpposite = false;
			
			// Hide the axis grid lines
			myPane.YAxis.MajorGrid.IsVisible = false;
			myPane.XAxis.MajorGrid.IsVisible = false;
			myPane.YAxis.MajorTic.IsOutside = false;
			myPane.XAxis.MajorTic.IsOutside = false;
			myPane.YAxis.MinorTic.IsAllTics = false;
			myPane.XAxis.MinorTic.IsAllTics = false;
			
			myPane.AxisChange();
			
			return myPane;
		}
		
		///<summary>
		/// The DataArray has the first row x, then the next rows y, then x, then y, etc.
		/// DataLabels should refer to the names of the y curves
		/// </summary>
		public ZedGraph.GraphPane Plot(GraphPane myPane, double [] X, double [] Y, string Label)
		{
			myPane.CurveList.Clear();
			
			// Set the title and axis labels
			myPane.Title.Text = Title;
			myPane.XAxis.Title.Text = XTitle;
			myPane.YAxis.Title.Text = YTitle;
			
			// Fill the background of the chart rect and pane
			myPane.Chart.Fill = new Fill( Color.White);

			// Show the legend
			myPane.Legend.IsVisible = true;
			myPane.Legend.Border.IsVisible = false;
			myPane.Legend.Position = ZedGraph.LegendPos.InsideTopLeft;
			myPane.Legend.Fill.IsVisible = false;
			
			// turn off the opposite tics so the Y tics don't show up on the Y2 axis
			myPane.YAxis.MajorTic.IsOpposite = false;
			myPane.YAxis.MinorTic.IsOpposite = false;
			myPane.XAxis.MajorTic.IsOpposite = false;
			myPane.XAxis.MinorTic.IsOpposite = false;
			
			// Hide the axis grid lines
			myPane.YAxis.MajorGrid.IsVisible = false;
			myPane.XAxis.MajorGrid.IsVisible = false;
			myPane.YAxis.MajorTic.IsOutside = false;
			myPane.XAxis.MajorTic.IsOutside = false;
			myPane.YAxis.MinorTic.IsAllTics = false;
			myPane.XAxis.MinorTic.IsAllTics = false;
			
			return Plot(myPane, X, Y, Label, true);
		}
		
		public ZedGraph.GraphPane Plot(GraphPane myPane, double [] X, double [] Y, string Label, bool NotFirst)
		{
			int n = X.Length;
			
			PointPairList list = new PointPairList();;
			
			for (int j = 0; j < n; j++) {
				
				list.Add(new PointPair(X[j],Y[j]));
			}
			if (colorNumber >= 17) {
				colorNumber = 0;
			}
			LineItem curve = myPane.AddCurve(Label, list, PlotColors[colorNumber]);
			
			//curve.Symbol.IsVisible = true;
			curve.Symbol.Size = 3.0F;
			curve.Line.Width = 2.0F;
			
			myPane.AxisChange();
			
			colorNumber++;
			
			return myPane;
		}
		
		public ZedGraph.GraphPane PlotPointwiseContour(GraphPane myPane, double [] X, double [] Y, double [] Z, string Label,
		                                               Color[] ContourColors){
			
			myPane.CurveList.Clear();
			
			// Set the title and axis labels
			myPane.Title.Text = Title;
			myPane.XAxis.Title.Text = XTitle;
			myPane.YAxis.Title.Text = YTitle;
			
			// Fill the background of the chart rect and pane
			myPane.Chart.Fill = new Fill( Color.White);
			
			// turn off the opposite tics so the Y tics don't show up on the Y2 axis
			myPane.YAxis.MajorTic.IsOpposite = false;
			myPane.YAxis.MinorTic.IsOpposite = false;
			myPane.XAxis.MajorTic.IsOpposite = false;
			myPane.XAxis.MinorTic.IsOpposite = false;
			
			// hide the legend
			myPane.Legend.IsVisible = false;
			
			// Hide the axis grid lines
			myPane.YAxis.MajorGrid.IsVisible = false;
			myPane.XAxis.MajorGrid.IsVisible = false;
			myPane.YAxis.MajorTic.IsOutside = false;
			myPane.XAxis.MajorTic.IsOutside = false;
			myPane.YAxis.MinorTic.IsAllTics = false;
			myPane.XAxis.MinorTic.IsAllTics = false;
			
			int n = X.Length;
			double min = Z.Min();
			double max = Z.Max();
            if (min.Equals(max))
            {
                if (min > 0)
                {
					min = 0.0;
                }
                else
                {
					max = 0.0;
                }
            }

			int nColors = ContourColors.Length;
			
			Color colorAbove = Color.White;
			
			//Plot each point as a seperate curve, with the color corresponding to the z-scale
			for (int i = 0; i < n; i++) {
				
				//Add the x and y locations as the coordinates of a curve
				PointPairList list = new PointPairList();
				list.Add(new PointPair(X[i],Y[i]));
				
				//Find out which color it is between colors
				double zNormalized = (Z[i] - min) / (max - min);
				double zColorExact = zNormalized * (double)(nColors-1);
				int zColorLower = (int)(Math.Floor(zColorExact));
				double zColorRemainder = zColorExact - zColorLower;
				
				//This is just so that it doesn't call an index out of range for the top color
				Color colorAboveThisOne = (zColorLower == (nColors-1)) ? colorAbove : ContourColors[zColorLower+1];
				
				//Interpolate the color by interpolating the RGB value of the color above and below the value
				int R = (int)(ContourColors[zColorLower].R + zColorRemainder * (colorAboveThisOne.R - ContourColors[zColorLower].R));
				int G = (int)(ContourColors[zColorLower].G + zColorRemainder * (colorAboveThisOne.G - ContourColors[zColorLower].G));
				int B = (int)(ContourColors[zColorLower].B + zColorRemainder * (colorAboveThisOne.B - ContourColors[zColorLower].B));
				
				//Now add the curve, using the new color I made
				LineItem curve = myPane.AddCurve(Label, list, Color.FromArgb(R,G,B));
				
				//Set some characteristics of the point
				curve.Symbol.Size = 10.0F;
				curve.Symbol.IsVisible = true;
				curve.Symbol.Type = SymbolType.Square;
				curve.Symbol.Fill.IsVisible = true;
				curve.Symbol.Fill.Type = FillType.Solid;
				curve.Line.IsVisible = false;
				
			}
			
			myPane.AxisChange();
			
			return myPane;
		}
		public ZedGraph.GraphPane Plot(GraphPane myPane, double[] Y, string Label)
		{
			double[] X = new double[Y.Length];
			for (int i = 0; i < Y.Length; i++)
			{
				X[i] = i + 1;
			}
			return Plot(myPane, X, Y, Label);
		}

	}
}
