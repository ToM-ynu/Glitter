using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickGraph.Algorithms;

namespace Glitter
{


    public class Glitter
    {
        internal CreateGraph graphs;
        Dictionary<string, (int upper, int horizontal, int lower)> wires;
        public double channelHeight;
        private List<(string, double)> result;
        public List<(string, double)> HeightOfHorizontalWire { get => result; private set => result = value; }
        private IEnumerable<Terminal> upper, lower;
        internal CalcLength calc;

        public bool IsDAG = false;

        public Glitter(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, (int upper, int horizontal, int lower)> wires)
        {
            this.upper = upper;
            this.lower = lower;
            this.wires = wires;
            graphs = new CreateGraph(upper, lower, wires);
            IsDAG = graphs.IsDAG;
        }

        public void Calc(bool ConsoleOut = true)
        {
            var weightedGraphs = new CreateWeightedGraph(graphs.VerticalGraph, graphs.HorizontalGraph, wires);
            var selection = new WeightedGraphSelection(weightedGraphs.weightedDirectedGraph, weightedGraphs.weightedUndirectedGraph, graphs.LocalMaximumDensity, wires, graphs.HorizontalGraph);
            var glitter_result = selection.Selection();
            var routingOrder = selection.WeightedDirectedGraph.TopologicalSort().Where(a => a != "Top" && a != "Bot");
            HeightOfHorizontalWire = new List<(string, double)>(routingOrder.Select(a => (a, CreateChainWeight.Ancestor(selection.WeightedDirectedGraph)[a])).ToList());
            var channelHeight = CreateChainWeight.Ancestor(selection.WeightedDirectedGraph)["Bot"];
            this.channelHeight = channelHeight;
            calc = new CalcLength(upper, lower, HeightOfHorizontalWire, channelHeight, wires);

            if (ConsoleOut == true)
            {
                Console.WriteLine("Returned WCG");
                selection.WeightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                Console.WriteLine("Routing Order is");
                routingOrder.ToString<string>().Write();
                Console.WriteLine("Routing Height(from top boundary) is");
                HeightOfHorizontalWire.Select(a => a.Item2).ToString<double>().Write();
                Console.WriteLine("Channel Height");
                Console.WriteLine(channelHeight);
                foreach (var (a, b) in HeightOfHorizontalWire)
                {
                    Console.Write(calc.GetLength(a));
                    Console.WriteLine(calc.GetInductance(a));
                }
                Console.WriteLine("VCG");
                graphs.VerticalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                Console.WriteLine("HCG");
                graphs.HorizontalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                Console.WriteLine("Init WCG(Directed)");
                weightedGraphs.weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                Console.WriteLine("Init WCG(unDirected)");
                weightedGraphs.weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            }
        }

        public void WriteGlitterCSV()
        {
            using (var streamWriter = new StreamWriter("glitterResult.csv"))
            {
                foreach (var (net, hight) in HeightOfHorizontalWire)
                {
                    streamWriter.Write($"{net},{hight}\n");
                }
            }
        }

        public IEnumerable<(string, (double upper, double horizontal, double lower))> GetInductanceCSV(bool WriteCSV = false)
        {
            var temp = wires.Select(a => (a.Key, calc.GetInductance(a.Key)));
            if (WriteCSV)
            {
                using (var streamWriter = new StreamWriter("InductanceResult.csv"))
                {
                    foreach (var (net, (upper, horizontal, lower)) in temp)
                    {
                        streamWriter.Write($"{net},{upper},{horizontal},{lower}\n");
                    }
                }
            }
            return temp;
        }

        public IEnumerable<(string net, (double upper, double horizontal, double lower) length)> GetSegmentLengthCSV(bool WriteCSV = false)
        {
            var temp = wires.Select(a => (a.Key, calc.GetLength(a.Key)));
            if (WriteCSV)
            {
                using (var streamWriter = new StreamWriter("SegmentLength.csv"))
                {
                    foreach (var (net, (upper, horizontal, lower)) in temp)
                    {
                        streamWriter.Write($"{net},{upper},{horizontal},{lower}\n");
                    }
                }
            }
            return temp;
        }

    }
}