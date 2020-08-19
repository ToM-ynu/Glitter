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

        Dictionary<string, List<(string terminalName, int xAxis)>> upper, lower;
        Dictionary<string, double> horizontalHight;

        Dictionary<string, (int upper, int horizontal, int lower)> wires;


        double channelHight;

        public IEnumerable<Terminal> Upper { get; }
        public IEnumerable<Terminal> Lower { get; }

        Dictionary<int, double> verticalInductanceDictionary { get; set; }
        Dictionary<int, double> horizontalInductanceDictionary { get; set; }
        public CalcLength(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, IEnumerable<(string net, double hight)> horizontalHight, double channelHight, Dictionary<string, (int upper, int horizontal, int lower)> wires)
        {
            this.upper = new Dictionary<string, List<(string, int)>>();
            this.lower = new Dictionary<string, List<(string, int)>>();
            this.horizontalHight = new Dictionary<string, double>(horizontalHight.Select(a => new KeyValuePair<string, double>(a.net, a.hight)));
            this.channelHight = channelHight;
            this.wires = wires;

            using (var srHorizontalInductace = new StreamReader(@"Reference\HorizontalInductanceTable.csv"))
            using (var csv = new CsvReader(srHorizontalInductace, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false; // When csv has no index row.
                horizontalInductanceDictionary = new Dictionary<int, double>(csv.GetRecords<Inductance>().Select(a => new KeyValuePair<int, double>(a.Width, a.Value)));
            }

            using (var srHorizontalInductace = new StreamReader(@"Reference\VerticalInductanceTable.csv"))
            using (var csv = new CsvReader(srHorizontalInductace, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false; // When csv has no index row.
                verticalInductanceDictionary = new Dictionary<int, double>(csv.GetRecords<Inductance>().Select(a => new KeyValuePair<int, double>(a.Width, a.Value)));
            }


        }

        internal (double upper, double horizontal, double lower) GetLength(string net, (string upper, string lower) terminalName)
        {
            double upperVertical, horizontal, lowerVertical;
            upperVertical = horizontalHight[net];
            lowerVertical = channelHight - upperVertical;
            var upperTerminal = upper[net].Find(a => a.terminalName == terminalName.upper);
            var lowerTerminal = lower[net].Find(a => a.terminalName == terminalName.lower);
            horizontal = Math.Abs(upperTerminal.xAxis - lowerTerminal.xAxis);
            return (upperVertical, horizontal, lowerVertical);
        }

        internal List<((string upper, string lower), (double upper, double horizontal, double lower))> GetLengths(string net)
        {
            var result = new List<((string upper, string lower), (double upper, double horizontal, double lower))>();
            //Get terminalName from net
            var upperTerminals = upper[net];
            var lowerTerminals = lower[net];
            foreach (var up in upperTerminals)
            {
                foreach (var low in lowerTerminals)
                {
                    result.Add(((up.terminalName, low.terminalName), GetLength(net, (up.terminalName, low.terminalName))));
                }
            }
            return result;
        }
        public (double upper, double horizontal, double lower) GetInductance(string net, (string upper, string lower) terminalName)
        {
            var result = GetLength(net, terminalName);
            var upper = result.upper * verticalInductanceDictionary[wires[net].upper];
            var horizontal = result.horizontal * horizontalInductanceDictionary[wires[net].horizontal];
            var lower = result.lower * verticalInductanceDictionary[wires[net].lower];
            return (upper, horizontal, lower);
        }

        internal List<((string upper, string lower), (double upper, double horizontal, double lower))> GetInductances(string net)
        {
            var result = new List<((string upper, string lower), (double upper, double horizontal, double lower))>();
            //Get terminalName from net
            var upperTerminals = upper[net];
            var lowerTerminals = lower[net];
            foreach (var up in upperTerminals)
            {
                foreach (var low in lowerTerminals)
                {
                    result.Add(((up.terminalName, low.terminalName), GetInductance(net, (up.terminalName, low.terminalName))));
                }
            }
            return result;
        }


    }

    public class Inductance
    {
        int width;
        double value;

        public int Width { get => width; set => width = value; }
        public double Value { get => value; set => this.value = value; }

    }


}