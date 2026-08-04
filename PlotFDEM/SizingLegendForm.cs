using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PlotFDEM
{
    /// <summary>
    /// Small companion window for the Sizing Plot that shows the current color/thickness
    /// scale and lets the user manually set the low/high bounds, thickness scaling mode,
    /// the color scheme (plus optional above/below-range colors), the fiber color/opacity,
    /// and the boundary/border color (similar to the Matrix Continuum Plot's legend).
    /// </summary>
    public class SizingLegendForm : Form
    {
        private readonly CreatePlot myPlot;

        private CheckBox cbAutomatic;
        private TextBox tbHigh;
        private TextBox tbLow;

        private CheckBox cbScaleThickness;
        private NumericUpDown nudFixedThickness;

        private ComboBox cbColorScheme;

        private CheckBox cbUseAboveColor;
        private Button bAboveColor;
        private CheckBox cbUseBelowColor;
        private Button bBelowColor;

        private Button bFiberColor;
        private NumericUpDown nudFiberAlpha;
        private Button bBoundaryColor;

        private Button bUpdate;
        private Panel scalePanel;

        private readonly List<Color[]> lColorSchemes = new List<Color[]>();
        private readonly List<string> lColorSchemeNames = new List<string>();

        public SizingLegendForm(CreatePlot plot)
        {
            myPlot = plot;

            Text = "Sizing Plot Legend";
            Width = 300;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.Manual;
            MaximizeBox = false;
            MinimizeBox = false;

            PopulateColorSchemes();

            int y = 10;
            const int leftLabel = 10;
            const int leftField = 100;
            const int rowHeight = 27;

            #region Range Controls
            cbAutomatic = new CheckBox
            {
                Text = "Automatic Range",
                Left = leftLabel,
                Top = y,
                Width = 200,
                Checked = myPlot.AutomaticSizingRange
            };
            y += rowHeight;

            Label lHigh = new Label { Text = "High:", Left = leftLabel, Top = y + 3, Width = 45 };
            tbHigh = new TextBox { Left = leftField, Top = y, Width = 150, Text = myPlot.SizingHighRange.ToString("G4") };
            y += rowHeight;

            Label lLow = new Label { Text = "Low:", Left = leftLabel, Top = y + 3, Width = 45 };
            tbLow = new TextBox { Left = leftField, Top = y, Width = 150, Text = myPlot.SizingLowRange.ToString("G4") };
            y += rowHeight + 8;
            #endregion

            #region Thickness Controls
            Label lThicknessHeader = new Label { Text = "Line Thickness", Left = leftLabel, Top = y, Width = 200, Font = new Font(Font, FontStyle.Bold) };
            y += 20;

            cbScaleThickness = new CheckBox
            {
                Text = "Scale Thickness by Force",
                Left = leftLabel,
                Top = y,
                Width = 220,
                Checked = myPlot.ScaleSizingThicknessByForce
            };
            y += rowHeight;

            Label lFixedThickness = new Label { Text = "Thickness:", Left = leftLabel, Top = y + 3, Width = 70 };
            nudFixedThickness = new NumericUpDown
            {
                Left = leftField,
                Top = y,
                Width = 100,
                Minimum = 0.1m,
                Maximum = 50m,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Value = (decimal)myPlot.FixedSizingThickness
            };
            y += rowHeight + 8;
            #endregion

            #region Color Scheme
            Label lSchemeHeader = new Label { Text = "Color Scheme", Left = leftLabel, Top = y, Width = 200, Font = new Font(Font, FontStyle.Bold) };
            y += 20;

            cbColorScheme = new ComboBox
            {
                Left = leftLabel,
                Top = y,
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (string name in lColorSchemeNames)
            {
                cbColorScheme.Items.Add(name);
            }
            cbColorScheme.SelectedIndex = FindInitialSchemeIndex();
            cbColorScheme.SelectedIndexChanged += (s, e) => scalePanel?.Invalidate();
            y += rowHeight;

            cbUseAboveColor = new CheckBox
            {
                Text = "Custom Above-Range Color",
                Left = leftLabel,
                Top = y,
                Width = 200,
                Checked = myPlot.SizingAboveRangeColor.HasValue
            };
            y += rowHeight - 4;

            bAboveColor = new Button
            {
                Left = leftField,
                Top = y,
                Width = 150,
                Height = 24,
                BackColor = myPlot.SizingAboveRangeColor ?? Color.Black,
                Enabled = cbUseAboveColor.Checked
            };
            bAboveColor.Click += (s, e) => PickColor(bAboveColor);
            cbUseAboveColor.CheckedChanged += (s, e) => bAboveColor.Enabled = cbUseAboveColor.Checked;
            y += rowHeight;

            cbUseBelowColor = new CheckBox
            {
                Text = "Custom Below-Range Color",
                Left = leftLabel,
                Top = y,
                Width = 200,
                Checked = myPlot.SizingBelowRangeColor.HasValue
            };
            y += rowHeight - 4;

            bBelowColor = new Button
            {
                Left = leftField,
                Top = y,
                Width = 150,
                Height = 24,
                BackColor = myPlot.SizingBelowRangeColor ?? Color.Gray,
                Enabled = cbUseBelowColor.Checked
            };
            bBelowColor.Click += (s, e) => PickColor(bBelowColor);
            cbUseBelowColor.CheckedChanged += (s, e) => bBelowColor.Enabled = cbUseBelowColor.Checked;
            y += rowHeight + 8;
            #endregion

            #region Fiber / Boundary Colors
            Label lFiberBoundaryHeader = new Label { Text = "Fiber / Boundary", Left = leftLabel, Top = y, Width = 200, Font = new Font(Font, FontStyle.Bold) };
            y += 20;

            Label lFiberColor = new Label { Text = "Fiber Color:", Left = leftLabel, Top = y + 3, Width = 90 };
            bFiberColor = new Button
            {
                Left = leftField,
                Top = y,
                Width = 150,
                Height = 24,
                BackColor = Color.FromArgb(255, myPlot.FiberColor.R, myPlot.FiberColor.G, myPlot.FiberColor.B)
            };
            bFiberColor.Click += (s, e) => PickColor(bFiberColor);
            y += rowHeight;

            Label lFiberAlpha = new Label { Text = "Fiber Opacity:", Left = leftLabel, Top = y + 3, Width = 90 };
            nudFiberAlpha = new NumericUpDown
            {
                Left = leftField,
                Top = y,
                Width = 100,
                Minimum = 0,
                Maximum = 255,
                Value = myPlot.FiberColor.A
            };
            y += rowHeight;

            Label lBoundaryColor = new Label { Text = "Boundary Color:", Left = leftLabel, Top = y + 3, Width = 90 };
            bBoundaryColor = new Button
            {
                Left = leftField,
                Top = y,
                Width = 150,
                Height = 24,
                BackColor = myPlot.BoundaryColor
            };
            bBoundaryColor.Click += (s, e) => PickColor(bBoundaryColor);
            y += rowHeight + 8;
            #endregion

            bUpdate = new Button { Text = "Update", Left = leftLabel, Top = y, Width = 260 };
            bUpdate.Click += BUpdate_Click;
            y += rowHeight + 10;

            scalePanel = new Panel
            {
                Left = leftLabel,
                Top = y,
                Width = 260,
                Height = 180,
                BorderStyle = BorderStyle.FixedSingle
            };
            scalePanel.Paint += ScalePanel_Paint;
            y += scalePanel.Height + 20;

            Controls.AddRange(new Control[]
            {
                cbAutomatic, lHigh, tbHigh, lLow, tbLow,
                lThicknessHeader, cbScaleThickness, lFixedThickness, nudFixedThickness,
                lSchemeHeader, cbColorScheme,
                cbUseAboveColor, bAboveColor, cbUseBelowColor, bBelowColor,
                lFiberBoundaryHeader, lFiberColor, bFiberColor, lFiberAlpha, nudFiberAlpha, lBoundaryColor, bBoundaryColor,
                bUpdate, scalePanel
            });

            Height = y + 40;

            cbAutomatic.CheckedChanged += (s, e) =>
            {
                tbHigh.Enabled = !cbAutomatic.Checked;
                tbLow.Enabled = !cbAutomatic.Checked;
            };
            tbHigh.Enabled = !cbAutomatic.Checked;
            tbLow.Enabled = !cbAutomatic.Checked;
        }

        private int FindInitialSchemeIndex()
        {
            for (int i = 0; i < lColorSchemes.Count; i++)
            {
                if (ReferenceEquals(lColorSchemes[i], myPlot.SizingColorScheme))
                {
                    return i;
                }
            }
            return 0;
        }

        private void PopulateColorSchemes()
        {
            lColorSchemes.Add(Contact.DefaultColorScheme);
            lColorSchemeNames.Add("Default (BtoTtoGtoYtoR)");

            lColorSchemes.Add(new Color[] { Color.Blue, Color.Aqua, Color.LimeGreen, Color.Yellow, Color.Red });
            lColorSchemeNames.Add("BtoGtoYtoR");

            lColorSchemes.Add(new Color[] { Color.Red, Color.Yellow, Color.LimeGreen, Color.Aqua, Color.Blue });
            lColorSchemeNames.Add("RtoYtoGtoB");

            lColorSchemes.Add(new Color[] { Color.Red, Color.White, Color.Blue });
            lColorSchemeNames.Add("RtoWtoB");

            lColorSchemes.Add(new Color[] { Color.Blue, Color.White, Color.Red });
            lColorSchemeNames.Add("BtoWtoR");

            lColorSchemes.Add(new Color[] { Color.Red, Color.White });
            lColorSchemeNames.Add("RedToWhite");

            lColorSchemes.Add(new Color[] { Color.White, Color.Red });
            lColorSchemeNames.Add("WhiteToRed");

            lColorSchemes.Add(new Color[] { Color.Black, Color.White });
            lColorSchemeNames.Add("BlackToWhite");

            lColorSchemes.Add(new Color[] { Color.White, Color.Black });
            lColorSchemeNames.Add("WhiteToBlack");
        }

        private void PickColor(Button targetButton)
        {
            ColorDialog myColor = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                SolidColorOnly = true,
                Color = targetButton.BackColor
            };

            if (myColor.ShowDialog() == DialogResult.OK)
            {
                targetButton.BackColor = myColor.Color;
            }
        }

        private void BUpdate_Click(object sender, EventArgs e)
        {
            myPlot.AutomaticSizingRange = cbAutomatic.Checked;

            if (!cbAutomatic.Checked)
            {
                if (double.TryParse(tbHigh.Text, out double high) &&
                    double.TryParse(tbLow.Text, out double low) &&
                    high > low)
                {
                    myPlot.SizingHighRange = high;
                    myPlot.SizingLowRange = low;
                }
                else
                {
                    MessageBox.Show("Please enter valid numbers for High and Low, with High > Low.",
                        "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            myPlot.ScaleSizingThicknessByForce = cbScaleThickness.Checked;
            myPlot.FixedSizingThickness = (float)nudFixedThickness.Value;

            myPlot.SizingColorScheme = lColorSchemes[cbColorScheme.SelectedIndex];

            myPlot.SizingAboveRangeColor = cbUseAboveColor.Checked ? (Color?)bAboveColor.BackColor : null;
            myPlot.SizingBelowRangeColor = cbUseBelowColor.Checked ? (Color?)bBelowColor.BackColor : null;

            int alpha = (int)nudFiberAlpha.Value;
            myPlot.FiberColor = Color.FromArgb(alpha, bFiberColor.BackColor.R, bFiberColor.BackColor.G, bFiberColor.BackColor.B);
            myPlot.BoundaryColor = bBoundaryColor.BackColor;

            myPlot.RefreshPlot();

            //Reflect current values (in case "Automatic" was just re-checked) back into the boxes
            tbHigh.Text = myPlot.SizingHighRange.ToString("G4");
            tbLow.Text = myPlot.SizingLowRange.ToString("G4");

            scalePanel.Invalidate();
        }

        private void ScalePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            const int nSteps = 60;
            const float barWidth = 40f;
            float top = 10f;
            float barHeight = scalePanel.Height - 20;
            float stepHeight = barHeight / nSteps;

            Color[] scheme = lColorSchemes[cbColorScheme.SelectedIndex];

            for (int i = 0; i < nSteps; i++)
            {
                double frac = (double)i / (nSteps - 1);
                Color c = Contact.ColorFromScheme(frac, scheme);
                using (SolidBrush b = new SolidBrush(c))
                {
                    float rowY = top + (nSteps - 1 - i) * stepHeight;
                    g.FillRectangle(b, 10, rowY, barWidth, stepHeight + 1);
                }
            }

            using (Font f = new Font("Arial", 9))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                double high = myPlot.AutomaticSizingRange ? myPlot.maxSizingForce : myPlot.SizingHighRange;
                double low = myPlot.AutomaticSizingRange ? 0.0 : myPlot.SizingLowRange;
                double mid = (high + low) / 2.0;

                g.DrawString(high.ToString("G4"), f, textBrush, 55, top);
                g.DrawString(mid.ToString("G4"), f, textBrush, 55, top + barHeight / 2 - 6);
                g.DrawString(low.ToString("G4"), f, textBrush, 55, top + barHeight - 12);
            }
        }
    }
}
