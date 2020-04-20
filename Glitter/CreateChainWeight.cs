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

    internal static class CreateChainWeight
    {


        internal static Dictionary<string, double> Deanestor(Graph weightedDirectedGraph)
        {
            var tempGraph = new Graph();
            var resultDictionary = new Dictionary<string, double>();
            //Create negative weighted directed graph
            tempGraph.AddVertexRange(weightedDirectedGraph.Vertices);
            tempGraph.AddEdgeRange(weightedDirectedGraph.Edges.Select(x => new Edge(x.Name, x.Source, x.Target, -x.Weight)));
            //Solve shortest path of vertex -> Top
            var algorithm = new BellmanFordShortestPathAlgorithm<string, Edge>(tempGraph, e => e.Weight);
            var pred = new VertexPredecessorRecorderObserver<string, Edge>();
            pred.Attach(algorithm);
            foreach (var vertex in tempGraph.Vertices.Where(a => a != "Top" && a != "Bot"))
            {
                algorithm.Compute(vertex);
                IEnumerable<Edge> path;
                pred.TryGetPath("Bot", out path);
                if (path != null)
                    resultDictionary[vertex] = -path.Sum(a => a.Weight);
            }
            return resultDictionary;
        }
        internal static Dictionary<string, double> Ancestor(Graph weightedDirectedGraph)
        {
            var tempGraph = new Graph();
            var resultDictionary = new Dictionary<string, double>();
            //Create negative weighted directed graph
            tempGraph.AddVertexRange(weightedDirectedGraph.Vertices);
            tempGraph.AddEdgeRange(weightedDirectedGraph.Edges.Select(x => new Edge(x.Name, x.Source, x.Target, -x.Weight)));
            //Solve shortest path of Top -> vertex
            var algorithm = new BellmanFordShortestPathAlgorithm<string, Edge>(tempGraph, e => e.Weight);
            var pred = new VertexPredecessorRecorderObserver<string, Edge>();
            pred.Attach(algorithm);
            foreach (var vertex in tempGraph.Vertices.Where(a => a != "Top" && a != "Bot"))
            {
                algorithm.Compute("Top");
                IEnumerable<Edge> path;
                pred.TryGetPath(vertex, out path);
                if (path != null)
                    resultDictionary[vertex] = -path.Sum(a => a.Weight);
            }
            return resultDictionary;
        }

    }
}