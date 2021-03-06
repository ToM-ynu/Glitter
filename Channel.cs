using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Glitter
{

    public class Channel
    {
        string channelCSVPath;
        string wireWidthCSVPath;
        IEnumerable<Terminal> upperSide, lowerSide;
        Dictionary<string, (int upper, int horizonal, int lower)> wires;

        public IEnumerable<(string, (double upper, double horizontal, double lower))> WireLenghtResult { get; private set; }
        public IEnumerable<(string, (double upper, double horizontal, double lower))> InductanceResult { get; private set; }


        ///for glitter
        public Channel(string channelCSVPath, string wireWidthCSVPath)
        {

            this.channelCSVPath = channelCSVPath;
            this.wireWidthCSVPath = wireWidthCSVPath;
            upperSide = new List<Terminal>();
            lowerSide = new List<Terminal>();
            using (var srChannelCSVPath = new StreamReader(channelCSVPath))
            using (var csv = new CsvReader(srChannelCSVPath, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false; // When csv has no index row.
                var result = csv.GetRecords<CSVStruct>().ToList();
                upperSide = result.Where(a => a.ul == "u").Select(b => new Terminal(b)).OrderBy(c => c.xAxis);
                lowerSide = result.Where(a => a.ul == "l").Select(b => new Terminal(b)).OrderBy(c => c.xAxis);

            }
            using (var srWireWidthCSVPath = new StreamReader(wireWidthCSVPath))
            using (var csv = new CsvReader(srWireWidthCSVPath, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                wires =
                new Dictionary<string, (int upper, int horizonal, int lower)>
                (csv.GetRecords<wireWidth>()
                .Select(a => new KeyValuePair<string, (int upper, int horizonal, int lower)>(a.net, (a.widthUpper, a.widthLower, a.widthHorizontal))));

            }

            var glitter = new Glitter(upperSide, lowerSide, wires);
            glitter.Calc();
            glitter.WriteGlitterCSV();
            InductanceResult = glitter.GetInductanceCSV();
            WireLenghtResult = glitter.GetSegmentLengthCSV();
        }

        // for internal call of Glitter
        public Channel(IEnumerable<string> channel, IEnumerable<string> wires)
        {
            upperSide = new List<Terminal>();
            lowerSide = new List<Terminal>();
            this.wires = new Dictionary<string, (int upper, int horizonal, int lower)>();

            var result = channel.Select(a => new CSVStruct(a));
            upperSide = result.Where(a => a.ul == "u").Select(b => new Terminal(b)).OrderBy(c => c.xAxis);
            lowerSide = result.Where(a => a.ul == "l").Select(b => new Terminal(b)).OrderBy(c => c.xAxis);
            throw new NotImplementedException();
            //   wires.Select(a => new wireWidth(a)).ToList().ForEach(x => this.wires.Add(x.net, x.widthHorizontal));
        }
    }

    public class CSVStruct
    {
        public string ul { get; set; }
        public string net { get; set; }
        public int xAxis { get; set; }
        public override string ToString()
        {
            return $"{ul},{net},{xAxis.ToString()}";
        }

        public CSVStruct(string str)
        {
            var temp = str.Split(',');
            ul = temp[0];
            net = temp[1];
            xAxis = int.Parse(temp[2]);
        }

        public CSVStruct()
        {

        }
    }

    public class wireWidth
    {
        public string net { get; set; }
        public int widthUpper { get; set; }
        public int widthHorizontal { get; set; }
        public int widthLower { get; set; }
        public override string ToString()
        {
            return $"net:{net},{widthHorizontal}";
        }

        public wireWidth(string str)
        {
            var temp = str.Split(',');
            net = temp[0];
            widthHorizontal = int.Parse(temp[1]);
            throw new NotImplementedException();
        }

        public wireWidth()
        {

        }
    }

    public struct Terminal
    {
        public string net;
        public int xAxis;
        public Terminal(string net, int xAxis)
        {
            this.net = net;
            this.xAxis = xAxis;
        }

        public Terminal(string str)
        {

            var temp = str.Split(',');
            net = temp[0];
            xAxis = int.Parse(temp[1]);
        }
        public Terminal(CSVStruct cSVStruct)
        {
            this.net = cSVStruct.net;
            this.xAxis = cSVStruct.xAxis;
        }
        public override string ToString()
        {
            return $"{net},{xAxis.ToString()}";
        }
    }
}