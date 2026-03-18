/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 9/1/2011
 * Time: 2:34 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace PlotFDEM.MatrixContinuum.ContourPlot
{
    partial class ContourPlotForm
    {
        /// <summary>
        /// Designer variable used to keep track of non-visual components.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Disposes resources used by the form.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// This method is required for Windows Forms designer support.
        /// Do not change the method contents inside the source code editor. The Forms designer might
        /// not be able to load this method if it was changed manually.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            spContourPlot = new System.Windows.Forms.SplitContainer();
            bFiberColor = new System.Windows.Forms.Button();
            cbProjections = new System.Windows.Forms.CheckBox();
            gb1 = new System.Windows.Forms.GroupBox();
            lLS = new System.Windows.Forms.Label();
            nudLS = new System.Windows.Forms.NumericUpDown();
            nudZ = new System.Windows.Forms.NumericUpDown();
            gbAll = new System.Windows.Forms.GroupBox();
            rbAllZ = new System.Windows.Forms.RadioButton();
            rbAllY = new System.Windows.Forms.RadioButton();
            rbField = new System.Windows.Forms.RadioButton();
            rbHistory = new System.Windows.Forms.RadioButton();
            label16 = new System.Windows.Forms.Label();
            cbZPointQuery = new System.Windows.Forms.ComboBox();
            label15 = new System.Windows.Forms.Label();
            cbYPointQuery = new System.Windows.Forms.ComboBox();
            label14 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            cbFiberQueryPair = new System.Windows.Forms.ComboBox();
            cbXPointQuery = new System.Windows.Forms.ComboBox();
            bQueryPoint = new System.Windows.Forms.Button();
            cbFiberNumbers = new System.Windows.Forms.CheckBox();
            cbRotations = new System.Windows.Forms.CheckBox();
            cbShowCrack = new System.Windows.Forms.CheckBox();
            cbConnections = new System.Windows.Forms.CheckBox();
            label11 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            bBoundaryColor = new System.Windows.Forms.Button();
            bProjFiberColor = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            cbAutomaticRange = new System.Windows.Forms.CheckBox();
            tbLowRange = new System.Windows.Forms.TextBox();
            tbHighRange = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            cbPlotComponent = new System.Windows.Forms.ComboBox();
            cbPlotType = new System.Windows.Forms.ComboBox();
            cbColorSchemes = new System.Windows.Forms.ComboBox();
            nudIsoLines = new System.Windows.Forms.NumericUpDown();
            nudGridPtsX = new System.Windows.Forms.NumericUpDown();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            cbIsoLines = new System.Windows.Forms.CheckBox();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            bHighColor = new System.Windows.Forms.Button();
            bLowColor = new System.Windows.Forms.Button();
            bUpdate = new System.Windows.Forms.Button();
            tbMatrixTransparancy = new System.Windows.Forms.TrackBar();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)spContourPlot).BeginInit();
            spContourPlot.Panel1.SuspendLayout();
            spContourPlot.SuspendLayout();
            gb1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudLS).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZ).BeginInit();
            gbAll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudIsoLines).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudGridPtsX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tbMatrixTransparancy).BeginInit();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // spContourPlot
            // 
            spContourPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            spContourPlot.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            spContourPlot.Location = new System.Drawing.Point(0, 0);
            spContourPlot.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            spContourPlot.Name = "spContourPlot";
            // 
            // spContourPlot.Panel1
            // 
            spContourPlot.Panel1.Controls.Add(bFiberColor);
            spContourPlot.Panel1.Controls.Add(cbProjections);
            spContourPlot.Panel1.Controls.Add(gb1);
            spContourPlot.Panel1.Controls.Add(cbFiberNumbers);
            spContourPlot.Panel1.Controls.Add(cbRotations);
            spContourPlot.Panel1.Controls.Add(cbShowCrack);
            spContourPlot.Panel1.Controls.Add(cbConnections);
            spContourPlot.Panel1.Controls.Add(label11);
            spContourPlot.Panel1.Controls.Add(label10);
            spContourPlot.Panel1.Controls.Add(label7);
            spContourPlot.Panel1.Controls.Add(bBoundaryColor);
            spContourPlot.Panel1.Controls.Add(bProjFiberColor);
            spContourPlot.Panel1.Controls.Add(label2);
            spContourPlot.Panel1.Controls.Add(cbAutomaticRange);
            spContourPlot.Panel1.Controls.Add(tbLowRange);
            spContourPlot.Panel1.Controls.Add(tbHighRange);
            spContourPlot.Panel1.Controls.Add(label9);
            spContourPlot.Panel1.Controls.Add(label8);
            spContourPlot.Panel1.Controls.Add(cbPlotComponent);
            spContourPlot.Panel1.Controls.Add(cbPlotType);
            spContourPlot.Panel1.Controls.Add(cbColorSchemes);
            spContourPlot.Panel1.Controls.Add(nudIsoLines);
            spContourPlot.Panel1.Controls.Add(nudGridPtsX);
            spContourPlot.Panel1.Controls.Add(label6);
            spContourPlot.Panel1.Controls.Add(label5);
            spContourPlot.Panel1.Controls.Add(cbIsoLines);
            spContourPlot.Panel1.Controls.Add(label4);
            spContourPlot.Panel1.Controls.Add(label3);
            spContourPlot.Panel1.Controls.Add(label1);
            spContourPlot.Panel1.Controls.Add(bHighColor);
            spContourPlot.Panel1.Controls.Add(bLowColor);
            spContourPlot.Panel1.Controls.Add(bUpdate);
            spContourPlot.Panel1.Controls.Add(tbMatrixTransparancy);
            // 
            // spContourPlot.Panel2
            // 
            spContourPlot.Panel2.AutoScroll = true;
            spContourPlot.Panel2.AutoScrollMinSize = new System.Drawing.Size(0, 10);
            spContourPlot.Panel2.ContextMenuStrip = contextMenuStrip1;
            spContourPlot.Panel2.Paint += scContourPlot_Panel2_Paint;
            spContourPlot.Size = new System.Drawing.Size(524, 553);
            spContourPlot.SplitterDistance = 262;
            spContourPlot.SplitterWidth = 13;
            spContourPlot.TabIndex = 0;
            // 
            // bFiberColor
            // 
            bFiberColor.Location = new System.Drawing.Point(226, 292);
            bFiberColor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bFiberColor.Name = "bFiberColor";
            bFiberColor.Size = new System.Drawing.Size(28, 27);
            bFiberColor.TabIndex = 30;
            bFiberColor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            bFiberColor.UseVisualStyleBackColor = true;
            bFiberColor.Click += bFiberColor_Click;
            // 
            // cbProjections
            // 
            cbProjections.Location = new System.Drawing.Point(16, 271);
            cbProjections.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbProjections.Name = "cbProjections";
            cbProjections.Size = new System.Drawing.Size(108, 28);
            cbProjections.TabIndex = 43;
            cbProjections.Text = "Projections";
            cbProjections.UseVisualStyleBackColor = true;
            // 
            // gb1
            // 
            gb1.Controls.Add(lLS);
            gb1.Controls.Add(nudLS);
            gb1.Controls.Add(nudZ);
            gb1.Controls.Add(gbAll);
            gb1.Controls.Add(rbField);
            gb1.Controls.Add(rbHistory);
            gb1.Controls.Add(label16);
            gb1.Controls.Add(cbZPointQuery);
            gb1.Controls.Add(label15);
            gb1.Controls.Add(cbYPointQuery);
            gb1.Controls.Add(label14);
            gb1.Controls.Add(label13);
            gb1.Controls.Add(cbFiberQueryPair);
            gb1.Controls.Add(cbXPointQuery);
            gb1.Controls.Add(bQueryPoint);
            gb1.Location = new System.Drawing.Point(9, 392);
            gb1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            gb1.Name = "gb1";
            gb1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            gb1.Size = new System.Drawing.Size(245, 150);
            gb1.TabIndex = 42;
            gb1.TabStop = false;
            gb1.Text = "Pair Query";
            // 
            // lLS
            // 
            lLS.AutoSize = true;
            lLS.ForeColor = System.Drawing.SystemColors.ControlText;
            lLS.Location = new System.Drawing.Point(1, 67);
            lLS.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lLS.Name = "lLS";
            lLS.Size = new System.Drawing.Size(59, 15);
            lLS.TabIndex = 57;
            lLS.Text = "Load Step";
            lLS.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            lLS.Visible = false;
            // 
            // nudLS
            // 
            nudLS.Location = new System.Drawing.Point(9, 85);
            nudLS.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudLS.Name = "nudLS";
            nudLS.Size = new System.Drawing.Size(37, 23);
            nudLS.TabIndex = 56;
            nudLS.Value = new decimal(new int[] { 1, 0, 0, 0 });
            nudLS.Visible = false;
            // 
            // nudZ
            // 
            nudZ.DecimalPlaces = 3;
            nudZ.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZ.Location = new System.Drawing.Point(91, 85);
            nudZ.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudZ.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            nudZ.Name = "nudZ";
            nudZ.Size = new System.Drawing.Size(89, 23);
            nudZ.TabIndex = 55;
            nudZ.Visible = false;
            // 
            // gbAll
            // 
            gbAll.Controls.Add(rbAllZ);
            gbAll.Controls.Add(rbAllY);
            gbAll.Location = new System.Drawing.Point(185, 13);
            gbAll.Name = "gbAll";
            gbAll.Size = new System.Drawing.Size(53, 99);
            gbAll.TabIndex = 54;
            gbAll.TabStop = false;
            gbAll.Visible = false;
            // 
            // rbAllZ
            // 
            rbAllZ.AutoSize = true;
            rbAllZ.Location = new System.Drawing.Point(8, 71);
            rbAllZ.Name = "rbAllZ";
            rbAllZ.Size = new System.Drawing.Size(39, 19);
            rbAllZ.TabIndex = 52;
            rbAllZ.Text = "All";
            rbAllZ.UseVisualStyleBackColor = true;
            // 
            // rbAllY
            // 
            rbAllY.AutoSize = true;
            rbAllY.Checked = true;
            rbAllY.Location = new System.Drawing.Point(8, 43);
            rbAllY.Name = "rbAllY";
            rbAllY.Size = new System.Drawing.Size(39, 19);
            rbAllY.TabIndex = 51;
            rbAllY.TabStop = true;
            rbAllY.Text = "All";
            rbAllY.UseVisualStyleBackColor = true;
            rbAllY.CheckedChanged += rbAllY_CheckedChanged;
            // 
            // rbField
            // 
            rbField.AutoSize = true;
            rbField.Location = new System.Drawing.Point(1, 45);
            rbField.Name = "rbField";
            rbField.Size = new System.Drawing.Size(50, 19);
            rbField.TabIndex = 50;
            rbField.Text = "Field";
            rbField.UseVisualStyleBackColor = true;
            rbField.CheckedChanged += rbField_CheckedChanged;
            // 
            // rbHistory
            // 
            rbHistory.AutoSize = true;
            rbHistory.Checked = true;
            rbHistory.Location = new System.Drawing.Point(1, 24);
            rbHistory.Name = "rbHistory";
            rbHistory.Size = new System.Drawing.Size(63, 19);
            rbHistory.TabIndex = 49;
            rbHistory.TabStop = true;
            rbHistory.Text = "History";
            rbHistory.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(73, 88);
            label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(12, 15);
            label16.TabIndex = 48;
            label16.Text = "z";
            label16.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbZPointQuery
            // 
            cbZPointQuery.AllowDrop = true;
            cbZPointQuery.FormattingEnabled = true;
            cbZPointQuery.Items.AddRange(new object[] { "Top", "Center-Top", "Center-Bottom", "Bottom" });
            cbZPointQuery.Location = new System.Drawing.Point(90, 84);
            cbZPointQuery.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbZPointQuery.Name = "cbZPointQuery";
            cbZPointQuery.Size = new System.Drawing.Size(91, 23);
            cbZPointQuery.TabIndex = 47;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(73, 57);
            label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(13, 15);
            label15.TabIndex = 46;
            label15.Text = "y";
            label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbYPointQuery
            // 
            cbYPointQuery.AllowDrop = true;
            cbYPointQuery.FormattingEnabled = true;
            cbYPointQuery.Items.AddRange(new object[] { "Left", "Center", "Right" });
            cbYPointQuery.Location = new System.Drawing.Point(90, 53);
            cbYPointQuery.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbYPointQuery.Name = "cbYPointQuery";
            cbYPointQuery.Size = new System.Drawing.Size(91, 23);
            cbYPointQuery.TabIndex = 45;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(72, 25);
            label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(13, 15);
            label14.TabIndex = 44;
            label14.Text = "x";
            label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(4, 119);
            label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(56, 15);
            label13.TabIndex = 43;
            label13.Text = "Fiber Pair";
            label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbFiberQueryPair
            // 
            cbFiberQueryPair.AllowDrop = true;
            cbFiberQueryPair.FormattingEnabled = true;
            cbFiberQueryPair.Location = new System.Drawing.Point(65, 114);
            cbFiberQueryPair.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbFiberQueryPair.Name = "cbFiberQueryPair";
            cbFiberQueryPair.Size = new System.Drawing.Size(92, 23);
            cbFiberQueryPair.TabIndex = 40;
            // 
            // cbXPointQuery
            // 
            cbXPointQuery.AllowDrop = true;
            cbXPointQuery.FormattingEnabled = true;
            cbXPointQuery.Items.AddRange(new object[] { "Front", "Center", "Back" });
            cbXPointQuery.Location = new System.Drawing.Point(90, 22);
            cbXPointQuery.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbXPointQuery.Name = "cbXPointQuery";
            cbXPointQuery.Size = new System.Drawing.Size(91, 23);
            cbXPointQuery.TabIndex = 41;
            // 
            // bQueryPoint
            // 
            bQueryPoint.Location = new System.Drawing.Point(169, 118);
            bQueryPoint.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bQueryPoint.Name = "bQueryPoint";
            bQueryPoint.Size = new System.Drawing.Size(69, 27);
            bQueryPoint.TabIndex = 38;
            bQueryPoint.Text = "Query";
            bQueryPoint.UseVisualStyleBackColor = true;
            bQueryPoint.Click += bQueryPoint_Click;
            // 
            // cbFiberNumbers
            // 
            cbFiberNumbers.BackColor = System.Drawing.Color.Transparent;
            cbFiberNumbers.Location = new System.Drawing.Point(122, 245);
            cbFiberNumbers.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbFiberNumbers.Name = "cbFiberNumbers";
            cbFiberNumbers.Size = new System.Drawing.Size(79, 28);
            cbFiberNumbers.TabIndex = 39;
            cbFiberNumbers.Text = "Fiber #s";
            cbFiberNumbers.UseVisualStyleBackColor = false;
            // 
            // cbRotations
            // 
            cbRotations.Location = new System.Drawing.Point(18, 247);
            cbRotations.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbRotations.Name = "cbRotations";
            cbRotations.Size = new System.Drawing.Size(108, 28);
            cbRotations.TabIndex = 37;
            cbRotations.Text = "Rotations";
            cbRotations.UseVisualStyleBackColor = true;
            // 
            // cbShowCrack
            // 
            cbShowCrack.Location = new System.Drawing.Point(122, 224);
            cbShowCrack.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbShowCrack.Name = "cbShowCrack";
            cbShowCrack.Size = new System.Drawing.Size(135, 28);
            cbShowCrack.TabIndex = 36;
            cbShowCrack.Text = "Crack";
            cbShowCrack.UseVisualStyleBackColor = true;
            // 
            // cbConnections
            // 
            cbConnections.Location = new System.Drawing.Point(18, 224);
            cbConnections.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbConnections.Name = "cbConnections";
            cbConnections.Size = new System.Drawing.Size(108, 28);
            cbConnections.TabIndex = 35;
            cbConnections.Text = "Connections";
            cbConnections.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.BackColor = System.Drawing.Color.Transparent;
            label11.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            label11.Location = new System.Drawing.Point(160, 357);
            label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(65, 27);
            label11.TabIndex = 34;
            label11.Text = "Boundary";
            label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            label10.BackColor = System.Drawing.Color.Transparent;
            label10.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            label10.Location = new System.Drawing.Point(160, 325);
            label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(65, 27);
            label10.TabIndex = 33;
            label10.Text = "Proj Fiber";
            label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.BackColor = System.Drawing.Color.Transparent;
            label7.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            label7.Location = new System.Drawing.Point(160, 292);
            label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(65, 27);
            label7.TabIndex = 32;
            label7.Text = "Fiber";
            label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // bBoundaryColor
            // 
            bBoundaryColor.Location = new System.Drawing.Point(226, 357);
            bBoundaryColor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bBoundaryColor.Name = "bBoundaryColor";
            bBoundaryColor.Size = new System.Drawing.Size(28, 27);
            bBoundaryColor.TabIndex = 31;
            bBoundaryColor.UseVisualStyleBackColor = true;
            bBoundaryColor.Click += bBoundaryColor_Click;
            // 
            // bProjFiberColor
            // 
            bProjFiberColor.Location = new System.Drawing.Point(226, 325);
            bProjFiberColor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bProjFiberColor.Name = "bProjFiberColor";
            bProjFiberColor.Size = new System.Drawing.Size(28, 27);
            bProjFiberColor.TabIndex = 29;
            bProjFiberColor.UseVisualStyleBackColor = true;
            bProjFiberColor.Click += bProjFiberColor_Click;
            // 
            // label2
            // 
            label2.Location = new System.Drawing.Point(14, 302);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(133, 27);
            label2.TabIndex = 28;
            label2.Text = "Matrix Transparancy";
            label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbAutomaticRange
            // 
            cbAutomaticRange.Location = new System.Drawing.Point(122, 201);
            cbAutomaticRange.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbAutomaticRange.Name = "cbAutomaticRange";
            cbAutomaticRange.Size = new System.Drawing.Size(135, 28);
            cbAutomaticRange.TabIndex = 26;
            cbAutomaticRange.Text = "Automatic Range";
            cbAutomaticRange.UseVisualStyleBackColor = true;
            cbAutomaticRange.CheckedChanged += cbAutomaticRange_CheckedChanged;
            // 
            // tbLowRange
            // 
            tbLowRange.Location = new System.Drawing.Point(119, 76);
            tbLowRange.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tbLowRange.Name = "tbLowRange";
            tbLowRange.Size = new System.Drawing.Size(135, 23);
            tbLowRange.TabIndex = 8;
            // 
            // tbHighRange
            // 
            tbHighRange.Location = new System.Drawing.Point(119, 44);
            tbHighRange.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tbHighRange.Name = "tbHighRange";
            tbHighRange.Size = new System.Drawing.Size(135, 23);
            tbHighRange.TabIndex = 6;
            // 
            // label9
            // 
            label9.Location = new System.Drawing.Point(7, 141);
            label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(105, 27);
            label9.TabIndex = 25;
            label9.Text = "Plot Component";
            label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.Location = new System.Drawing.Point(7, 108);
            label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(105, 27);
            label8.TabIndex = 24;
            label8.Text = "Plot Type";
            label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbPlotComponent
            // 
            cbPlotComponent.FormattingEnabled = true;
            cbPlotComponent.Location = new System.Drawing.Point(119, 143);
            cbPlotComponent.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbPlotComponent.Name = "cbPlotComponent";
            cbPlotComponent.Size = new System.Drawing.Size(135, 23);
            cbPlotComponent.TabIndex = 23;
            cbPlotComponent.SelectedIndexChanged += CbPlotComponentSelectedIndexChanged;
            // 
            // cbPlotType
            // 
            cbPlotType.FormattingEnabled = true;
            cbPlotType.Items.AddRange(new object[] { "Stress", "Strain", "Local Displacements", "Global Displacements" });
            cbPlotType.Location = new System.Drawing.Point(119, 111);
            cbPlotType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbPlotType.Name = "cbPlotType";
            cbPlotType.Size = new System.Drawing.Size(135, 23);
            cbPlotType.TabIndex = 22;
            cbPlotType.SelectedIndexChanged += CbPlotTypeSelectedIndexChanged;
            // 
            // cbColorSchemes
            // 
            cbColorSchemes.FormattingEnabled = true;
            cbColorSchemes.Location = new System.Drawing.Point(119, 13);
            cbColorSchemes.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbColorSchemes.Name = "cbColorSchemes";
            cbColorSchemes.Size = new System.Drawing.Size(135, 23);
            cbColorSchemes.TabIndex = 21;
            cbColorSchemes.SelectedIndexChanged += CbColorSchemesSelectedIndexChanged;
            // 
            // nudIsoLines
            // 
            nudIsoLines.Location = new System.Drawing.Point(198, 174);
            nudIsoLines.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            nudIsoLines.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudIsoLines.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudIsoLines.Name = "nudIsoLines";
            nudIsoLines.Size = new System.Drawing.Size(56, 23);
            nudIsoLines.TabIndex = 18;
            nudIsoLines.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudGridPtsX
            // 
            nudGridPtsX.Location = new System.Drawing.Point(70, 174);
            nudGridPtsX.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            nudGridPtsX.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudGridPtsX.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudGridPtsX.Name = "nudGridPtsX";
            nudGridPtsX.Size = new System.Drawing.Size(56, 23);
            nudGridPtsX.TabIndex = 17;
            nudGridPtsX.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label6
            // 
            label6.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label6.Location = new System.Drawing.Point(133, 171);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(98, 27);
            label6.TabIndex = 12;
            label6.Text = "# IsoLines";
            label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            label5.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label5.Location = new System.Drawing.Point(6, 171);
            label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(98, 27);
            label5.TabIndex = 11;
            label5.Text = "# Grid Pts";
            label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbIsoLines
            // 
            cbIsoLines.Location = new System.Drawing.Point(18, 201);
            cbIsoLines.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cbIsoLines.Name = "cbIsoLines";
            cbIsoLines.Size = new System.Drawing.Size(98, 28);
            cbIsoLines.TabIndex = 10;
            cbIsoLines.Text = "Iso Lines";
            cbIsoLines.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label4.Location = new System.Drawing.Point(42, 74);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(98, 27);
            label4.TabIndex = 9;
            label4.Text = "Low Range";
            label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            label3.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label3.Location = new System.Drawing.Point(42, 40);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(98, 27);
            label3.TabIndex = 7;
            label3.Text = "High Range";
            label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            label1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label1.Location = new System.Drawing.Point(14, 10);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(98, 27);
            label1.TabIndex = 4;
            label1.Text = "Color Scheme";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // bHighColor
            // 
            bHighColor.Location = new System.Drawing.Point(10, 40);
            bHighColor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bHighColor.Name = "bHighColor";
            bHighColor.Size = new System.Drawing.Size(28, 27);
            bHighColor.TabIndex = 3;
            bHighColor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            bHighColor.UseVisualStyleBackColor = true;
            bHighColor.Click += BHighColorClick;
            // 
            // bLowColor
            // 
            bLowColor.Location = new System.Drawing.Point(10, 74);
            bLowColor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bLowColor.Name = "bLowColor";
            bLowColor.Size = new System.Drawing.Size(28, 27);
            bLowColor.TabIndex = 2;
            bLowColor.UseVisualStyleBackColor = true;
            bLowColor.Click += BLowColorClick;
            // 
            // bUpdate
            // 
            bUpdate.Location = new System.Drawing.Point(10, 355);
            bUpdate.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            bUpdate.Name = "bUpdate";
            bUpdate.Size = new System.Drawing.Size(88, 27);
            bUpdate.TabIndex = 0;
            bUpdate.Text = "Update";
            bUpdate.UseVisualStyleBackColor = true;
            bUpdate.Click += BUpdateClick;
            // 
            // tbMatrixTransparancy
            // 
            tbMatrixTransparancy.BackColor = System.Drawing.SystemColors.Control;
            tbMatrixTransparancy.LargeChange = 20;
            tbMatrixTransparancy.Location = new System.Drawing.Point(9, 331);
            tbMatrixTransparancy.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tbMatrixTransparancy.Maximum = 255;
            tbMatrixTransparancy.Name = "tbMatrixTransparancy";
            tbMatrixTransparancy.Size = new System.Drawing.Size(158, 45);
            tbMatrixTransparancy.TabIndex = 27;
            tbMatrixTransparancy.TickStyle = System.Windows.Forms.TickStyle.None;
            tbMatrixTransparancy.Value = 255;
            tbMatrixTransparancy.ValueChanged += trackBar1_ValueChanged;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { copyToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(103, 26);
            contextMenuStrip1.ItemClicked += contextMenuStrip1_ItemClicked;
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            copyToolStripMenuItem.Text = "Copy";
            // 
            // ContourPlotForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(524, 553);
            Controls.Add(spContourPlot);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "ContourPlotForm";
            Text = "ContourPlotForm";
            spContourPlot.Panel1.ResumeLayout(false);
            spContourPlot.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)spContourPlot).EndInit();
            spContourPlot.ResumeLayout(false);
            gb1.ResumeLayout(false);
            gb1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudLS).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZ).EndInit();
            gbAll.ResumeLayout(false);
            gbAll.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudIsoLines).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudGridPtsX).EndInit();
            ((System.ComponentModel.ISupportInitialize)tbMatrixTransparancy).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.ComboBox cbColorSchemes;
        private System.Windows.Forms.Button bUpdate;
        private System.Windows.Forms.Button bLowColor;
        private System.Windows.Forms.Button bHighColor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbHighRange;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbLowRange;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbIsoLines;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown nudGridPtsX;
        private System.Windows.Forms.NumericUpDown nudIsoLines;
        private System.Windows.Forms.SplitContainer spContourPlot;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cbPlotComponent;
        protected System.Windows.Forms.ComboBox cbPlotType;
        private System.Windows.Forms.CheckBox cbAutomaticRange;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TrackBar tbMatrixTransparancy;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button bBoundaryColor;
        private System.Windows.Forms.Button bFiberColor;
        private System.Windows.Forms.Button bProjFiberColor;
        private System.Windows.Forms.CheckBox cbConnections;
        private System.Windows.Forms.CheckBox cbShowCrack;
        private System.Windows.Forms.CheckBox cbRotations;
        private System.Windows.Forms.Button bQueryPoint;
        private System.Windows.Forms.CheckBox cbFiberNumbers;
        private System.Windows.Forms.GroupBox gb1;
        private System.Windows.Forms.ComboBox cbFiberQueryPair;
        private System.Windows.Forms.ComboBox cbXPointQuery;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox cbZPointQuery;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox cbYPointQuery;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.CheckBox cbProjections;
        private System.Windows.Forms.GroupBox gbAll;
        private System.Windows.Forms.RadioButton rbAllZ;
        private System.Windows.Forms.RadioButton rbAllY;
        private System.Windows.Forms.RadioButton rbField;
        private System.Windows.Forms.RadioButton rbHistory;
        private System.Windows.Forms.NumericUpDown nudZ;
        private System.Windows.Forms.Label lLS;
        private System.Windows.Forms.NumericUpDown nudLS;
    }
}
