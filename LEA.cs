using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Yoshimura {
    public class LeftEdgeAlgorithm {
        Graph verticalGraph;
        Graph horizontalGraph;

        List < (string net, int track) > routingResult;

        public LeftEdgeAlgorithm (IEnumerable<Terminal> upper, IEnumerable<Terminal> lower) {
            verticalGraph = new Graph ();
            horizontalGraph = new Graph ();
            CreateHorizontalGraph (upper, lower);
            CreateVerticalGraph (upper, lower);
            Console.WriteLine ("\n");
            var piyo = LeafVerticalList ();
            foreach (var item in piyo) {
                Console.WriteLine (item);
            }

        }

        private void Routing () {
            
        }

        private List<string> LeafVerticalList () {

            var result = new List<string> ();
            Console.WriteLine (verticalGraph.Vertices.Count ());
            Console.WriteLine (verticalGraph.Edges.Count ());
            foreach (var vertical in verticalGraph.Vertices) {
                var flag = true;
                foreach (var edge in verticalGraph.Edges) {
                    if (edge.Source == vertical) flag = false;
                }
                if (flag) {
                    //This vertical is not source of all edges.
                    result.Add (vertical);
                }
            }
            return result;
        }

        private void CreateVerticalGraph (IEnumerable<Terminal> upper, IEnumerable<Terminal> lower) {
            var nets = new HashSet<string> (upper.Select (a => a.net).Concat (lower.Select (b => b.net)));
            verticalGraph.AddVertexRange (nets);
            foreach (var item in upper) {
                var verticalColisionList = lower.Where (a => a.xAxis == item.xAxis).ToList ();
                if (verticalColisionList.Count == 0) continue;
                else if (verticalColisionList.Count > 1) {
                    throw new InvalidDataException ("Invalid input data");
                } else {
                    if (item.net == verticalColisionList[0].net) continue; //avoid self-loops
                    var temp = new Edge ("net" + item.net, item.net, verticalColisionList[0].net, 1);
                    verticalGraph.AddEdge (temp);
                }
            }
#if DEBUG
            foreach (var item in verticalGraph.Edges) {
                Console.WriteLine (item);
            }
#endif
            //To checking graph is DAG or not, we are using exception of topologicalsort.
            //try-catch is very slow.
            try {
                var hoge = verticalGraph.TopologicalSort ();
            } catch (NonAcyclicGraphException) {

                Console.WriteLine ("This is non-DAG graph. By LEA, there is no solution.");
                Environment.Exit (1);
            }

        }
        private void CreateHorizontalGraph (IEnumerable<Terminal> upper, IEnumerable<Terminal> lower) {
            var nets = new HashSet<string> (upper.Select (a => a.net).Concat (lower.Select (b => b.net)));
            horizontalGraph.AddVertexRange (nets);
            var hoge = new List<Terminal> (upper.Concat (lower));
            var terminalSections = new List < (string net, int begin, int end) > ();
            foreach (var item in nets) {
                var terminalPositions = hoge.Where (a => a.net == item);
                terminalSections.Add ((item, terminalPositions.Select (a => a.xAxis).Min (), terminalPositions.Select (a => a.xAxis).Max ()));
            }
            for (var i = 0; i < terminalSections.Count; i++) {
                for (int j = i + 1; j < terminalSections.Count; j++) {
                    if (terminalSections[i].begin <= terminalSections[j].begin && terminalSections[i].begin <= terminalSections[j].end) {
                        var temp = new Edge ("net" + terminalSections[i].net + terminalSections[j].net, terminalSections[i].net, terminalSections[j].net, 1);
                        horizontalGraph.AddUndirectedEdge (temp);
                    }
                }
            }
        }
    }

    class Graph : AdjacencyGraph<string, Edge> {
        public void AddUndirectedEdge (Edge edge) {
            AddEdge (new Edge (edge.Name + "a", edge.Target, edge.Source, edge.Weight));
            AddEdge (new Edge (edge.Name + "b", edge.Source, edge.Target, edge.Weight));

        }
    }
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

        public override string ToString () {
            return $"{Name},{Source},{Target},{Weight.ToString()}";
        }
    }

    public class Channel {
        string CSVPath;
        IEnumerable<Terminal> upperSide, lowerSide;
        public Channel (string CSVPath) {
            this.CSVPath = CSVPath;
            upperSide = new List<Terminal> ();
            lowerSide = new List<Terminal> ();
            using (var streamReader = new StreamReader (CSVPath))
            using (var csv = new CsvReader (streamReader, CultureInfo.InvariantCulture)) {
                csv.Configuration.HasHeaderRecord = false; // When csv has no index row.
                var result = csv.GetRecords<CSVStruct> ().ToList ();
                upperSide = result.Where (a => a.ul == "u").Select (b => new Terminal (b));
                lowerSide = result.Where (a => a.ul == "l").Select (b => new Terminal (b));

            }
            //Console.WriteLine (upperSide.Count ());
            //Console.WriteLine (lowerSide.Count ());
            var hoge = new LeftEdgeAlgorithm (upperSide, lowerSide);

        }
    }
    public struct Terminal {
        public string net;
        public int xAxis;
        public Terminal (string net, int xAxis) {
            this.net = net;
            this.xAxis = xAxis;
        }
        public Terminal (CSVStruct cSVStruct) {
            this.net = cSVStruct.net;
            this.xAxis = cSVStruct.xAxis;
        }
        public override string ToString () {
            return $"{net},{xAxis.ToString()}";
        }
    }

    public class CSVStruct {
        public string ul { get; set; }
        public string net { get; set; }
        public int xAxis { get; set; }
        public override string ToString () {
            return $"{ul},{net},{xAxis.ToString()}";
        }
    }
}