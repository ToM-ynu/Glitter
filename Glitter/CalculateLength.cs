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

        UnitInductance unitInductance;

        double channelHight;

        public IEnumerable<Terminal> Upper { get; }
        public IEnumerable<Terminal> Lower { get; }

        internal CalcLength(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, IEnumerable<(string net, double hight)> horizontalHight, double channelHight, UnitInductance unitInductance)
        {
            this.upper = new Dictionary<string, int>(upper.Select(a => new KeyValuePair<string, int>(a.net, a.xAxis)));
            this.lower = new Dictionary<string, int>(lower.Select(a => new KeyValuePair<string, int>(a.net, a.xAxis)));
            this.horizontalHight = new Dictionary<string, double>(horizontalHight.Select(a => new KeyValuePair<string, double>(a.net, a.hight)));
            this.channelHight = channelHight;
            this.unitInductance = unitInductance;
        }

        internal (double upper, double horizontal, double lower) GetLength(string net)
        {
            double a, b, c;
            a = horizontalHight[net];
            c = channelHight - a;
            b = Math.Abs(upper[net] - lower[net]);
            return (a, b, c);
        }
        internal (double upper, double horizontal, double lower) GetInductance(string net)
        {
            var result = GetLength(net);
            return (result.upper * unitInductance.Upper, result.horizontal * unitInductance.Horizontal, result.lower * unitInductance.Lower);


        }

    }
}