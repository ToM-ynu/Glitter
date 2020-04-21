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
    public class LeftEdgeAlgorithm
    {
        Graph verticalGraph;
        Graph horizontalGraph;
        public LeftEdgeAlgorithm(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower)
        {
            verticalGraph = new Graph();
            horizontalGraph = new Graph();
            CreateVerticalGraph(upper, lower);
            CreateHorizontalGraph(upper, lower);
            var sweepIndex = upper.Concat(lower).OrderBy(b => b.xAxis).Distinct(a => a.net).Select(c => c.net).ToList();
            "Creating Graph is done.".WriteLine();
            Console.WriteLine("VCG");
            verticalGraph.Edges.ToString<Edge>().Write();
            Console.WriteLine("HCG");
            horizontalGraph.Edges.ToString<Edge>().Write();
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
                    if (source.min <= target.min && target.min <= source.max)
                    {
                        var temp =
                            new Edge("net" + terminalSections[i].net + target.net, terminalSections[i].net, target.net, 1);
                        horizontalGraph.AddUndirectedEdge(temp);
                    }
                    else if (source.min <= target.max && target.max <= source.max)
                    {
                        var temp =
                            new Edge("net" + terminalSections[i].net + target.net, terminalSections[i].net, target.net, 1);
                        horizontalGraph.AddUndirectedEdge(temp);
                    }
                }
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
    }

    class Graph : AdjacencyGraph<string, Edge>
    {
        public void AddUndirectedEdge(Edge edge)
        {
            AddEdge(new Edge(edge.Name + "a", edge.Target, edge.Source, edge.Weight));
            AddEdge(new Edge(edge.Name + "b", edge.Source, edge.Target, edge.Weight));

        }
        public Graph()
        {

        }
        public Graph(Graph graph)
        {
            AddVertexRange(graph.Vertices);
            AddEdgeRange(graph.Edges.Select(a => new Edge(a)));
        }

    }
    class Edge : IEdge<string>
    {
        public Edge(string n, string s, string t, double w)
        {
            Name = n;
            Source = s;
            Target = t;
            Weight = w;
        }
        public Edge(Edge edge)
        {
            Name = edge.Name;
            Source = edge.Source;
            Target = edge.Target;
            Weight = edge.Weight;
        }

        public string Name { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public double Weight { get; set; }

        // public override string ToString () {
        //     return $"{Name},{Source},{Target},{Weight.ToString()}";
        // }
        public override string ToString()
        {
            return $"{Source}->{Target} [label = {Weight}]";
        }
    }
}



static class Extension
{
    public static void WriteLine(this object obj, string text = "{0}")
    {
        Console.WriteLine(text, obj);
    }

    public static void Write(this object obj, string text = "{0}")
    {
        Console.Write(text, obj);
    }

    public static string ToString<T>(this IEnumerable<T> list, Func<T, string> toStr = null, string format = "{0},", string begin = "[", string end = "]")
    {
        //default
        if (toStr == null) toStr = (t) => t.ToString();
        var sb = new StringBuilder(begin);
        foreach (var str in list.Select(a => a.ToString()))
        {
            sb.AppendFormat(format, str);
        }
        sb.AppendLine(end);
        return sb.ToString();
    }

    public static string ToString<TKey, TValue>(this Dictionary<TKey, TValue> dir, Func<TKey, string> toStr = null, string format = "{0},", string begin = "[", string end = "]")
    {
        //default
        if (toStr == null) toStr = (t) => t.ToString();
        var sb = new StringBuilder(begin);
        foreach (var str in dir.Select(a => a.ToString()))
        {
            sb.AppendFormat(format, str);
        }
        sb.AppendLine(end);
        return sb.ToString();
    }

    //from https://qiita.com/wipiano/items/4437e80dc99a30f303f0
    public static IEnumerable<TItem> Distinct<TItem, TKey>(this IEnumerable<TItem> source, Func<TItem, TKey> keySelector)
    {
        IEnumerable<TItem> Enumerate()
        {
            var set = new HashSet<TKey>();
            foreach (var item in source)
                if (set.Add(keySelector(item))) yield return item;
        }

        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        return Enumerate();
    }

}

