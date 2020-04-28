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
    internal class CreateWeightedGraph
    {
        internal Graph weightedDirectedGraph { get; }
        internal Graph weightedUndirectedGraph { get; }

        internal CreateWeightedGraph(Graph verticalGraph, Graph horizontalGraph, Dictionary<string, int> wires)
        {
            weightedDirectedGraph = new Graph();
            weightedUndirectedGraph = new Graph();

            weightedDirectedGraph.AddVertexRange(verticalGraph.Vertices);
            var verticalEdges = verticalGraph.Edges.Select(a => new Edge(a.Name, a.Source, a.Target, (wires[a.Source] + wires[a.Target]) / 2));
            weightedDirectedGraph.AddEdgeRange(verticalEdges);

            weightedUndirectedGraph.AddVertexRange(horizontalGraph.Vertices);
            weightedUndirectedGraph.AddEdgeRange(horizontalGraph.Edges);

            var HCGRoot = GetHCGRoot(verticalGraph);
            var HCGLeaf = GetHCGLeaf(verticalGraph);
            weightedDirectedGraph.AddVertexRange(new string[] { "Top", "Bot" });
            foreach (var item in HCGRoot)
            {
                var weight = Constant.minSpacing + wires[item] / 2;
                var temp =
                           new Edge($"netTop{item}", "Top", item, weight);
                weightedDirectedGraph.AddEdge(temp);

            }
            foreach (var item in HCGLeaf)
            {
                var weight = Constant.minSpacing + wires[item] / 2;
                var temp =
                           new Edge($"net{item}Bot", item, "Bot", weight);
                weightedDirectedGraph.AddEdge(temp);
            }
        }


        private List<string> GetHCGRoot(Graph verticalGraph)
        {
            var result = new List<string>();
            return weightedDirectedGraph.Vertices.Except<string>(verticalGraph.Edges.Select(a => a.Target)).ToList();
        }
        private List<string> GetHCGLeaf(Graph verticalGraph)
        {
            var result = new List<string>();
            return weightedDirectedGraph.Vertices.Except<string>(verticalGraph.Edges.Select(a => a.Source)).ToList();
        }

    }
}