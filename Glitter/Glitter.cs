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
        Graph verticalGraph;
        Graph horizontalGraph;

        ///Directed graph とUndirected graphは別のグラフに持ったほうが良さそう
        Dictionary<string, int> wires;

        public double channelHight;

        List<(string, double)> result;

        public Glitter(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, int> wires)
        {
            this.wires = wires;
            var graphs = new CreateGraph(upper, lower, wires);
            verticalGraph = graphs.VerticalGraph;
            horizontalGraph = graphs.HorizontalGraph;
            var temp = graphs.LocalMaximumDensity;
            "Creating Graph is done.".WriteLine();

            Console.WriteLine("VCG");
            verticalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            Console.WriteLine("HCG");
            horizontalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();

            var weightedGraphs = new CreateWeightedGraph(verticalGraph, horizontalGraph, wires);
            var selection = new WeightedGraphSelection(weightedGraphs.weightedDirectedGraph, weightedGraphs.weightedUndirectedGraph, graphs.LocalMaximumDensity, wires, horizontalGraph);
            var glitter_result = selection.Selection();

            Console.WriteLine("Returned WCG");
            selection.WeightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();

            Console.WriteLine("Channel Hight");
            var channelHight = CreateChainWeight.Ancestor(selection.WeightedDirectedGraph)["Bot"];
            this.channelHight = channelHight;
            Console.WriteLine(channelHight);

            Console.WriteLine("Routing Order Glitter (net, direction from top)");
            result = new List<(string, double)>();
            result = glitter_result.Select(a => (a.Item1, a.Item2 == "CB" ? channelHight - a.Item3 : a.Item3)).ToList();
            result.ToString<(string, double)>().Write();
#if DEBUG
            Console.WriteLine("Init WCG(Directed)");
            weightedGraphs.weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            Console.WriteLine("Init WCG(unDirected)");
            weightedGraphs.weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
#endif

            CreateGlitterCSV(result);
        }

        private List<string> VerticalLeafList()
        {

            var result = new List<string>();
            foreach (var vertical in verticalGraph.Vertices)
            {
                var flag = true;
                foreach (var edge in verticalGraph.Edges)
                {
                    if (edge.Source == vertical) flag = false;
                }
                if (flag)
                {
                    //This vertical is not source of all edges.
                    result.Add(vertical);
                }
            }
            return result;
        }

        private bool IsDirectPassExist(string source, List<string> target)
        {
            return horizontalGraph.Edges.Where(a => a.Source == source).Select(a => a.Target).Intersect(target).Count() != 0;
        }

        private bool IsStraightWire(string net, IEnumerable<Terminal> upper, IEnumerable<Terminal> lower)
        {
            return upper.First(a => a.net == net).xAxis == lower.First(a => a.net == net).xAxis;
        }

        private void OutputResult(Dictionary<int, List<string>> dir)
        {
            foreach (var item in dir)
            {
                item.Key.Write();
                (":" + item.Value.ToString<string>()).Write();
            }
        }

        private void CreateGlitterCSV(IEnumerable<(string, double)> list)
        {

            using (var streamWriter = new StreamWriter("glitterResult.csv"))
            {
                foreach (var (net, hight) in list)
                {
                    streamWriter.Write($"{net},{hight}\n");
                }
            }
        }



    }

    public class WireWidth
    {
        public string ul { get; set; }
        public string net { get; set; }
        public int xAxis { get; set; }
        public override string ToString()
        {
            return $"{ul},{net},{xAxis.ToString()}";
        }
    }

}