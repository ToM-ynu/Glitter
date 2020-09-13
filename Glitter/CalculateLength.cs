using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;


namespace Glitter
{


    internal class CalcLength
    {

        Dictionary<string, int> upper, lower;
        Dictionary<string, double> horizontalHight;

        Dictionary<string, (int upper, int horizontal, int lower)> wires;


        double channelHight;

        public IEnumerable<Terminal> Upper { get; }
        public IEnumerable<Terminal> Lower { get; }

        Dictionary<int, double> verticalInductanceDictionary { get; set; }
        Dictionary<int, double> horizontalInductanceDictionary { get; set; }
        public CalcLength(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, IEnumerable<(string net, double hight)> horizontalHight, double channelHight, Dictionary<string, (int upper, int horizontal, int lower)> wires)
        {
            this.upper = new Dictionary<string, int>(upper.Select(a => new KeyValuePair<string, int>(a.net, a.xAxis)));
            this.lower = new Dictionary<string, int>(lower.Select(a => new KeyValuePair<string, int>(a.net, a.xAxis)));
            this.horizontalHight = new Dictionary<string, double>(horizontalHight.Select(a => new KeyValuePair<string, double>(a.net, a.hight)));
            this.channelHight = channelHight;
            this.wires = wires;

            using (var srHorizontalInductace = new StreamReader(@"C:\Users\tomo8\Source\Repos\yoshikawa-laboratory\aqfp-topdown\Glitter\Reference\HorizontalInductanceTable.csv"))
            using (var csv = new CsvReader(srHorizontalInductace, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false; // When csv has no index row.
                horizontalInductanceDictionary = new Dictionary<int, double>(csv.GetRecords<Inductance>().Select(a => new KeyValuePair<int, double>(a.Width, a.Value)));
            }

            using (var srHorizontalInductace = new StreamReader(@"C:\Users\tomo8\Source\Repos\yoshikawa-laboratory\aqfp-topdown\Glitter\Reference\VerticalInductanceTable.csv"))
            using (var csv = new CsvReader(srHorizontalInductace, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false; // When csv has no index row.
                verticalInductanceDictionary = new Dictionary<int, double>(csv.GetRecords<Inductance>().Select(a => new KeyValuePair<int, double>(a.Width, a.Value)));
            }


        }

        internal (double upper, double horizontal, double lower) GetLength(string net)
        {
            double a, b, c;
            a = horizontalHight[net];
            c = channelHight - a;
            b = Math.Abs(upper[net] - lower[net]);
            return (a, b, c);
        }
        public (double upper, double horizontal, double lower) GetInductance(string net)
        {
            var result = GetLength(net);
            var upper = result.upper * verticalInductanceDictionary[wires[net].upper];
            var horizontal = result.horizontal * horizontalInductanceDictionary[wires[net].horizontal];
            var lower = result.lower * verticalInductanceDictionary[wires[net].lower];
            return (upper, horizontal, lower);
        }


    }

    public class Inductance
    {
        int width;
        double value;

        public int Width { get => width; set => width = value; }
        public double Value { get => value; set => this.value = value; }

    }

    public class InductCeRange
    {
        string netName;
        double min;
        double max;

        public string Width { get => netName; set => netName = value; }
        public double Min { get => min; set => this.min = value; }
        public double Max { get => max; set => this.max = value; }
    }
}