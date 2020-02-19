using System;
using System.Collections.Generic;
using System.IO;
using QuickGraph;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;

namespace Yoshimura {
    public class LeftEdgeAlgorithm {
        Graph verticalGraph;
        Graph horizontalGraph;

        public LeftEdgeAlgorithm () {
            verticalGraph = new Graph ();
            horizontalGraph = new Graph (); 
        }
    }

    class Graph : AdjacencyGraph<string, Edge> { }
    class Edge : IEdge<string> {
        public Edge (string n, string s, string t, double w) {
            Name = n;
            Source = s;
            Target = t;
            Weight = w;
        }
        public string Name { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public double Weight { get; set; }
    }
}