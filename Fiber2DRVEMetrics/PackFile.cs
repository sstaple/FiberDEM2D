using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MicroCluster
{
    internal class PackFile
    {
        #region Public Members
        // File Path
        public string? FilePath { get; set; }
        // Pack file name
        public string? PackFileName { get; set; }
        // Save Directory
        public string? SaveDirectory { get; set; }
        // Y vals
        public List<double> Y { get; private set; } = new List<double>();
        // Z vals
        public List<double> Z { get; private set; } = new List<double>();
        // R vals
        public List<double> R { get; private set; } = new List<double>();
        // Y bound
        public double YBoundary { get; private set; }
        // Z bound
        public double ZBoundary { get; private set; }
        public Microstructure? Microstructure { get; private set; }

        #endregion

        #region Constructor
        public PackFile(string filePath, string saveDir) 
        {
            // Store file path infor in easily accessible way
            FilePath = filePath; 
            PackFileName = System.IO.Path.GetFileName(filePath);
            SaveDirectory = saveDir;
        }

        #endregion

        
        public void Initiate(OutputOptions outputOptions)
        {
            // Read csv and store data
            ReadCsv();

            // Create the microstructure and start that whole process
            Microstructure = new Microstructure(outputOptions,FilePath,PackFileName,SaveDirectory,Y,Z,R,YBoundary,ZBoundary);

        }
        
        private void ReadCsv()
        {
            var lines = File.ReadAllLines(FilePath);
            if (lines.Length == 0)
            {
                Console.WriteLine("CSV File is Empty!");
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    continue;  // skip blank lines

                var cols = raw.Split(',');

                // Check once for a header row containing either length Y or length Z
                bool hasYHdr = cols.Any(c => c.Contains("length Y", StringComparison.OrdinalIgnoreCase));
                bool hasZHdr = cols.Any(c => c.Contains("length Z", StringComparison.OrdinalIgnoreCase));
                if (hasYHdr || hasZHdr)
                {
                    // Read both boundaries from the very next line (if present)
                    if (i + 1 < lines.Length)
                    {
                        var valCols = lines[i + 1].Split(',');

                        if (hasYHdr)
                        {
                            int yIdx = Array.FindIndex(
                                cols,
                                c => c.Contains("length Y", StringComparison.OrdinalIgnoreCase)
                            );
                            if (yIdx >= 0 && yIdx < valCols.Length
                                && double.TryParse(
                                       valCols[yIdx],
                                       NumberStyles.Any,
                                       CultureInfo.InvariantCulture,
                                       out double yVal))
                            {
                                YBoundary = yVal;
                            }
                        }

                        if (hasZHdr)
                        {
                            int zIdx = Array.FindIndex(
                                cols,
                                c => c.Contains("length Z", StringComparison.OrdinalIgnoreCase)
                            );
                            if (zIdx >= 0 && zIdx < valCols.Length
                                && double.TryParse(
                                       valCols[zIdx],
                                       NumberStyles.Any,
                                       CultureInfo.InvariantCulture,
                                       out double zVal))
                            {
                                ZBoundary = zVal;
                            }
                        }
                    }
                    i++;  // skip the boundary‐value row
                    continue;
                }

                // --- Coordinate rows: at least 3 columns, all numeric ---
                if (cols.Length >= 3
                    && double.TryParse(cols[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double y)
                    && double.TryParse(cols[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double z)
                    && double.TryParse(cols[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double r))
                {
                    Y.Add(y);
                    Z.Add(z);
                    R.Add(r);
                }
                // else skip any non-(y,z,r) line
            }

        }

    }
}
