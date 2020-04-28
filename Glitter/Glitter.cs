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


    public class Glitter
    {
        CreateGraph graphs;
        Dictionary<string, (int upper, int lower, int horizontal)> wires;
        public double channelHight;
        private List<(string, double)> result;
        public List<(string, double)> Result { get => result; private set => result = value; }
        private IEnumerable<Terminal> upper, lower;
        internal CalcLength calc;


        public Glitter(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, (int upper, int lower, int horizontal)> wires)
        {
            this.upper = upper;
            this.lower = lower;
            this.wires = wires;
            graphs = new CreateGraph(upper, lower, wires);

        }

        public void Calc()
        {
            var weightedGraphs = new CreateWeightedGraph(graphs.VerticalGraph, graphs.HorizontalGraph, wires);
            var selection = new WeightedGraphSelection(weightedGraphs.weightedDirectedGraph, weightedGraphs.weightedUndirectedGraph, graphs.LocalMaximumDensity, wires, graphs.HorizontalGraph);
            var glitter_result = selection.Selection();

            Console.WriteLine("Returned WCG");
            selection.WeightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();

            Console.WriteLine("Channel Hight");
            var channelHight = CreateChainWeight.Ancestor(selection.WeightedDirectedGraph)["Bot"];
            this.channelHight = channelHight;
            Console.WriteLine(channelHight);

            Result = new List<(string, double)>();
            Result = glitter_result.Select(a => (a.Item1, a.Item2 == "CB" ? channelHight - a.Item3 : a.Item3)).ToList();

            calc = new CalcLength(upper, lower, Result, channelHight, wires);
            foreach (var (a, b) in Result)
            {
                Console.Write(calc.GetLength(a));
                Console.WriteLine(calc.GetInductance(a));
            }
#if DEBUG
            Console.WriteLine("VCG");
            graphs.VerticalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            Console.WriteLine("HCG");
            graphs.HorizontalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();

            Console.WriteLine("Init WCG(Directed)");
            weightedGraphs.weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            Console.WriteLine("Init WCG(unDirected)");
            weightedGraphs.weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
#endif
        }

        public void WriteGlitterCSV()
        {
            using (var streamWriter = new StreamWriter("glitterResult.csv"))
            {
                foreach (var (net, hight) in Result)
                {
                    streamWriter.Write($"{net},{hight}\n");
                }
            }
        }

        public void WriteInductanceCSV()
        {
            using (var streamWriter = new StreamWriter("InductanceResult.csv"))
            {
                foreach (var (net, (upper, horizontal, lower)) in Result.OrderBy(a => a.Item1).Select(a => (a.Item1, calc.GetInductance(a.Item1))))
                {
                    streamWriter.Write($"{net},{upper},{horizontal},{lower}\n");
                }
            }
        }
    }
}