/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 3/19/2011
 * Time: 3:14 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ZedGraph;
using System.Linq;

namespace SinglePlot
{
	/// <summary>
	/// Description of SinglePlotForm.
	/// </summary>
	public partial class SinglePlotForm : Form
	{
		public PlotMaker myPlot;
		public GraphPane myPane;


		public SinglePlotForm(string Title, string xTitle, string yTitle, string Label, double[] x, double[] y)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			myPane = zedGraphControl1.GraphPane;

			myPlot = new PlotMaker(Title, xTitle, yTitle);
			this.Text = "results";

			myPlot.Plot(myPane, x, y, Label);

			string sText = xTitle + ", " + yTitle + " \r";

			for (int i = 0; i < x.Length; i++)
			{

				sText += x[i] + ", " + y[i] + " \r";
			}

			tbResults.Text = sText;

		}

		public SinglePlotForm(string Label, double [] x, double [] y)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			myPane = zedGraphControl1.GraphPane;
			
			myPlot = new PlotMaker(Label, "x", "y");
			this.Text = Label;
			this.Name = Label;
			
			string myLabel = Label;
			
			myPlot.Plot(myPane, x, y, myLabel);
			
			string sText = "x, " + Label + " \r";
			
			for (int i = 0; i < x.Length; i++) {
				
				sText += x[i] + ", " + y[i] + " \r";
			}
			
			tbResults.Text = sText;
			
		}
		
		public SinglePlotForm(string Label, double [] x, double [] y, double [] z, Color [] contourColors)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			myPane = zedGraphControl1.GraphPane;
			
			myPlot = new PlotMaker(Label, "x", "y");
			this.Text = Label;
			this.Name = Label;
			
			string myLabel = Label;
			
			myPlot.PlotPointwiseContour(myPane, x, y, z, myLabel, contourColors);
			
			string sText = "Max = " + z.Max() + "\r Min = " + z.Min();
			tbResults.Text = sText;
			
		}
		public SinglePlotForm(string title, string xTitle, string yTitle, List<string> labels, List<double []> lX, List<double []> lY)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			myPane = zedGraphControl1.GraphPane;
			
			myPlot = new PlotMaker(title, xTitle, yTitle);
			
			myPlot.Plot(myPane, lX[0], lY[0], labels[0]);
			
			for (int i = 1; i < labels.Count; i++) {
				
				myPlot.Plot(myPane, lX[i], lY[i], labels[i], true);
			}	
			#region Populate the text box with the results
			string sText = "";
			
			#region Print titles
			foreach (string s in labels){
				sText +=  xTitle + ", " + s + ", " ;
			}
			sText += "\r";
			#endregion
			
			#region Find the longest series
			int nMax = 0;
			
			for (int j = 0; j < lY.Count; j++) {	
					if (lY[j].GetLength(0) > nMax) {
					
					nMax = lY[j].GetLength(0);
				}
			}
			#endregion
			
			for (int i = 0; i < nMax; i++) {
				
				for (int j = 0; j < lX.Count; j++) {
					
					if (i >= lX[j].GetLength(0)) {
						
						sText += "NaN, NaN,";
					}
					else{
						sText += lX[j][i] + ", " + lY[j][i] + ", " ;
						
					}
					
				}
			sText += "\r";
			}
			
			tbResults.Text = sText;
			#endregion
		
			
			//ShowDialog();
		}
		public void Plot(){
			Activate();
			ShowDialog();
		}
			
	}
}
