using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiber2DRVEMetrics
{
    public class OutputOptions
    {
        public bool SaveParaviewFibers { get; set; } = false;
        public bool SaveParaviewClusters { get; set; } = false;
        public bool SaveParaviewClustersAtEveryStep { get; set; } = false;


        // Load options from a text file
        public static OutputOptions Load(string path)
        {
            var options = new OutputOptions();

            if (!File.Exists(path))
            {
                //Console.WriteLine("No OutputOptions file found. Default options will be used.");
                return options;
            }

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                string trimmed = line.Trim().ToLower();

                if (trimmed == "savefiberpositions") options.SaveParaviewFibers = true;
                if (trimmed == "saveclusters") options.SaveParaviewClusters = true;
                if (trimmed == "saveclustersateverystep") options.SaveParaviewClustersAtEveryStep = true;
                
            }

            return options;
        }
    }
}
