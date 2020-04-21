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

        public Glitter(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, int> wires)
        {
            this.wires = wires;
            var graphs = new CreateGraph(upper, lower, wires);
            verticalGraph = graphs.VerticalGraph;
            horizontalGraph = graphs.HorizontalGraph;
            var hoge = graphs.LocalMaximumDensity;
            "Creating Graph is done.".WriteLine();

            Console.WriteLine("VCG");
            verticalGraph.Edges.ToString<Edge>().Write();
            Console.WriteLine("HCG");
            horizontalGraph.Edges.ToString<Edge>().Write();


            Console.WriteLine("MAX density");
            Console.WriteLine(graphs.MaxDensity);
            var weightedGraphs = new CreateWeightedGraph(verticalGraph, horizontalGraph, wires);


            Console.WriteLine("Init WCG");
            weightedGraphs.weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            weightedGraphs.weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();

            // weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            // weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();

            Environment.Exit(1);

            var sweepIndex = upper.Concat(lower).OrderBy(b => b.xAxis).Distinct(a => a.net).Select(c => c.net).ToList();

#if DEBUG
            "SweepIndex is".Write();
            sweepIndex.ToString<string>().Write();
#endif
            var straightWireList = upper.Where(a => IsStraightWire(a.net, upper, lower)).Select(b => b.net).ToList();
            straightWireList.ForEach(a => verticalGraph.RemoveVertex(a));
#if DEBUG
            "straight wire list is ".Write();
            straightWireList.ToString<string>().Write();
#endif
            var channelInfo = new Dictionary<int, List<string>>(); //Key.Channel number , Value. applied track
            for (int trackNumber = 0; verticalGraph.VertexCount != 0; trackNumber++)
            {
                channelInfo[trackNumber] = new List<string>();
                var leafList = VerticalLeafList().OrderBy(a => sweepIndex.IndexOf(a)).ToList(); // Test is not enough
#if DEBUG
                verticalGraph.Edges.ToString<Edge>().Write();
#endif
                foreach (var leaf in leafList)
                {
                    if (!IsDirectPassExist(leaf, channelInfo[trackNumber]))
                    {
                        channelInfo[trackNumber].Add(leaf);
                        verticalGraph.RemoveVertex(leaf);
                    }
                }
                if (trackNumber == 100) throw new Exception("Routing is failed");
            }
            OutputResult(channelInfo);
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