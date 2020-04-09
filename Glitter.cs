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

namespace Yoshimura
{

    public static class Constant
    {
        public const int minSpacing = 3;
    }
    public class Glitter
    {
        Graph verticalGraph;
        Graph horizontalGraph;

        ///Directed graph とUndirected graphは別のグラフに持ったほうが良さそう
        Graph weightedDirectedGraph;
        Graph weightedUndirectedGraph;
        Dictionary<string, int> wires;
        public Glitter(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, int> wires)
        {
            verticalGraph = new Graph();
            horizontalGraph = new Graph();
            this.wires = wires;
            CreateVerticalGraph(upper, lower);
            CreateHorizontalGraph(upper, lower);
            var sweepIndex = upper.Concat(lower).OrderBy(b => b.xAxis).Distinct(a => a.net).Select(c => c.net).ToList();
            "Creating Graph is done.".WriteLine();
            Console.WriteLine("VCG");
            verticalGraph.Edges.ToString<Edge>().Write();
            Console.WriteLine("HCG");
            horizontalGraph.Edges.ToString<Edge>().Write();

            Console.WriteLine("MAX density");
            Console.WriteLine(MaximumDensity(upper, lower));
            CreateWeightedGraphs();

            Console.Write("HCG Leaf");
            GetHCGLeaf().ToString<string>().Write();
            Console.Write("HCG Root");
            GetHCGRoot().ToString<string>().Write();
            AddBoundary(wires);
            Console.WriteLine("WCG");
            weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
            Console.WriteLine("ANCW");
            CreateAnsestorWeights().ToString<string, double>().WriteLine();
            Console.WriteLine("DECW");
            CreateDeansestorWeights().ToString<string, double>().WriteLine();

            Environment.Exit(1);
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
        private List<string> HorizontalAdjacentVertexList(string source)
        {
            var set = new HashSet<string>();
            foreach (var item in horizontalGraph.Edges.Where(a => a.Source == source || a.Target == source))
            {
                set.Add(item.Source);
                set.Add(item.Target);
            }
            return set.ToList();
        }

        private void CreateVerticalGraph(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower)
        {
            var nets = new HashSet<string>(upper.Select(a => a.net).Concat(lower.Select(b => b.net)));
            verticalGraph.AddVertexRange(nets);
            foreach (var item in upper)
            {
                var verticalColisionList = lower.Where(a => a.xAxis == item.xAxis).ToList();
                if (verticalColisionList.Count == 0) continue;
                else if (verticalColisionList.Count > 1)
                {
                    throw new InvalidDataException("Invalid input data");
                }
                else
                {
                    if (item.net == verticalColisionList[0].net) continue; //avoid self-loops
                    var temp = new Edge("net" + item.net, item.net, verticalColisionList[0].net, 1);
                    verticalGraph.AddEdge(temp);
                }
            }
            //To checking graph is DAG or not, we are using exception of topologicalsort.
            //try-catch is very slow.
            try
            {
                var hoge = verticalGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {

                "VCG".WriteLine();
                verticalGraph.Edges.ToString<Edge>().Write();
                Console.WriteLine("This is non-DAG graph. By LEA, there is no solution.");
                Environment.Exit(1);

            }
        }
        private void CreateHorizontalGraph(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower)
        {
            var nets = new HashSet<string>(upper.Select(a => a.net).Concat(lower.Select(b => b.net)));
            horizontalGraph.AddVertexRange(nets);
            var terminalSections = new List<(string net, int min, int max)>();
            foreach (var net in nets)
            {
                var terminalPositions = upper.Concat(lower).Where(a => a.net == net).Select(a => a.xAxis);
                var hoge = (net, terminalPositions.Min(), terminalPositions.Max());
                if (hoge.Item2 != hoge.Item3)
                    terminalSections.Add(hoge);
            }
            for (var i = 0; i < terminalSections.Count; i++)
            {
                var source = terminalSections[i];
                for (int j = i + 1; j < terminalSections.Count; j++)
                {
                    var target = terminalSections[j];
                    var weight = Constant.minSpacing + wires[source.net] / 2 + wires[target.net] / 2;
                    if (IsInside((target.min, target.max), (source.min, source.max)))
                    {
                        var temp =
                            new Edge("net" + source.net + target.net, source.net, target.net, weight);
                        horizontalGraph.AddEdge(temp);
                    }
                }
            }
        }

        private static bool IsInside<T>((T min, T max) a, (T min, T max) b) where T : IComparable<T>
        {
            var flag = false;
            flag |= (a.min.CompareTo(b.min) <= 0 && a.max.CompareTo(b.min) >= 0);
            flag |= (a.min.CompareTo(b.max) <= 0 && a.max.CompareTo(b.max) >= 0);
            flag |= (b.min.CompareTo(a.min) <= 0 && b.max.CompareTo(a.min) >= 0);
            flag |= (b.min.CompareTo(a.max) <= 0 && b.max.CompareTo(a.max) >= 0);
            return flag;
        }
        ///Longest path from Top to vertex
        private Dictionary<string, double> CreateAnsestorWeights()
        {
            var tempGraph = new Graph();
            var resultDictionary = new Dictionary<string, double>();
            //Create negative weighted directed graph
            tempGraph.AddVertexRange(weightedDirectedGraph.Vertices);
            tempGraph.AddEdgeRange(weightedDirectedGraph.Edges.Select(x => new Edge(x.Name, x.Source, x.Target, -x.Weight)));
            //Solve shortest path of Top -> vertex
            var algo = new BellmanFordShortestPathAlgorithm<string, Edge>(tempGraph, e => e.Weight);
            var pred = new VertexPredecessorRecorderObserver<string, Edge>();
            pred.Attach(algo);
            foreach (var vertex in tempGraph.Vertices.Where(a => a != "Top" && a != "Bot"))
            {
                algo.Compute("Top");
                IEnumerable<Edge> path;
                pred.TryGetPath(vertex, out path);
                if (path != null)
                    resultDictionary[vertex] = -path.Sum(a => a.Weight);
            }
            return resultDictionary;
        }

        ///Longest path vetex to Bottom
        private Dictionary<string, double> CreateDeansestorWeights()
        {
            var tempGraph = new Graph();
            var resultDictionary = new Dictionary<string, double>();
            //Create negative weighted directed graph
            tempGraph.AddVertexRange(weightedDirectedGraph.Vertices);
            tempGraph.AddEdgeRange(weightedDirectedGraph.Edges.Select(x => new Edge(x.Name, x.Source, x.Target, -x.Weight)));
            //Solve shortest path of vetex -> Top
            var algo = new BellmanFordShortestPathAlgorithm<string, Edge>(tempGraph, e => e.Weight);
            var pred = new VertexPredecessorRecorderObserver<string, Edge>();
            pred.Attach(algo);
            foreach (var vertex in tempGraph.Vertices.Where(a => a != "Top" && a != "Bot"))
            {
                algo.Compute(vertex);
                IEnumerable<Edge> path;
                pred.TryGetPath("Bot", out path);
                if (path != null)
                    resultDictionary[vertex] = -path.Sum(a => a.Weight);
            }
            return resultDictionary;
        }
        private double MinSeparation(string i, string j)
        {
            return Constant.minSpacing + wires[i] / 2 + wires[j] / 2;
        }

        private double CalcLabel(string i, string j, Dictionary<string, double> ancw, Dictionary<string, double> decw)
        {
            ///論文にはサイクルができないようにするって書いてあるけど、無向グラフがあったらどうやってもサイクルできちゃうので、
            ///(VCG+すでに向きを割り当てたEdgeで、)サイクルができないようにするってことでいいのか？？？？多分そう。
            var tempGraph = new Graph();
            tempGraph.AddVertexRange(weightedDirectedGraph.Vertices);
            tempGraph.AddEdgeRange(weightedDirectedGraph.Edges);
            var guid = Guid.NewGuid();
            var EdgeIJ = weightedUndirectedGraph.Edges.Where(a => a.Target == i && a.Source == j).First();
            //i->jが行けるか試す。
            var edge = new Edge(guid.ToString(), i, j, EdgeIJ.Weight);
            tempGraph.AddEdge(edge);
            try
            {
                var hoge = tempGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                return double.PositiveInfinity;
            }
            tempGraph.RemoveEdge(edge);
            //j->iが行けるか試す。
            edge = new Edge(guid.ToString(), j, i, EdgeIJ.Weight);
            tempGraph.AddEdge(edge);
            try
            {
                var hoge = tempGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                return double.PositiveInfinity;
            }
            tempGraph.RemoveEdge(edge);
            //どっちもできるので、実数を投げる。
            var left = ancw[i] + decw[j] + MinSeparation(i, j);
            var right = ancw[j] + decw[i] + MinSeparation(i, j);

            return Math.Max(left, right);
        }


        private List<string> GetHCGRoot()
        {
            var result = new List<string>();
            return weightedDirectedGraph.Vertices.Except<string>(verticalGraph.Edges.Select(a => a.Target)).ToList();
        }
        private List<string> GetHCGLeaf()
        {
            var result = new List<string>();
            return weightedDirectedGraph.Vertices.Except<string>(verticalGraph.Edges.Select(a => a.Source)).ToList();
        }
        private void AddBoundary(Dictionary<string, int> wires)
        {

            var HCGRoot = GetHCGRoot();
            var HCGLeaf = GetHCGLeaf();
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

        private void CreateWeightedGraphs()
        {
            if (horizontalGraph == null || verticalGraph == null) throw new Exception("Create HCG/VGC first.");
            weightedDirectedGraph = new Graph();
            weightedUndirectedGraph = new Graph();
            weightedDirectedGraph.AddVertexRange(verticalGraph.Vertices);
            weightedDirectedGraph.AddEdgeRange(verticalGraph.Edges);

            weightedUndirectedGraph.AddVertexRange(horizontalGraph.Vertices);
            weightedUndirectedGraph.AddEdgeRange(horizontalGraph.Edges);
        }

        private double MaximumDensity(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower)
        {
            const double boundaryClearance = 3;
            const double VerticalWireWidth = 10;
            var temp = upper.Concat(lower);
            var youso = new HashSet<string>(temp.Select(a => a.net));
            var IMOS = new Dictionary<double, double>();
            foreach (var net in youso)
            {
                var foo = temp.Where(a => a.net == net);
                var min = foo.Min(a => a.xAxis) - VerticalWireWidth / 2;
                var max = foo.Max(a => a.xAxis) + VerticalWireWidth / 2;
                if (IMOS.ContainsKey(min))
                {
                    IMOS[min] += wires[net];
                }
                else
                {
                    IMOS[min] = wires[net];
                }

                if (IMOS.ContainsKey(max))
                {
                    IMOS[max] += -wires[net];
                }
                else
                {
                    IMOS[max] = -wires[net];
                }
            }
            var sum = 0.0;
            var current = 0.0;
            foreach (var item in IMOS)
            {
                current += item.Value;
                sum = Math.Max(sum, current);
            }

            return boundaryClearance * 2 + sum;
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