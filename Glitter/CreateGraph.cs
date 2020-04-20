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

    internal class CreateGraph
    {
        internal Graph verticalGraph { get; }
        internal Graph horizontalGraph { get; }
        private Dictionary<string, int> wires;
        private IEnumerable<Terminal> upper;
        private IEnumerable<Terminal> lower;
        internal double maxDensity;
        internal CreateGraph(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, int> wires)
        {
            this.upper = upper;
            this.lower = lower;
            this.wires = wires;
            verticalGraph = new Graph();
            horizontalGraph = new Graph();
            CreateVerticalGraph();
            CreateHorizontalGraph();
            MaximumDensity();
        }

        private void CreateVerticalGraph()
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
                verticalGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {

                "VCG".WriteLine();
                verticalGraph.Edges.ToString<Edge>().Write();
                Console.WriteLine("This is non-DAG graph. By LEA, there is no solution.");
                Environment.Exit(1);

            }
        }
        private void CreateHorizontalGraph()
        {
            var nets = new HashSet<string>(upper.Select(a => a.net).Concat(lower.Select(b => b.net)));
            horizontalGraph.AddVertexRange(nets);
            var terminalSections = new List<(string net, int min, int max)>();
            foreach (var net in nets)
            {
                var terminalPositions = upper.Concat(lower).Where(a => a.net == net).Select(a => a.xAxis);
                var temp = (net, terminalPositions.Min(), terminalPositions.Max());
                if (temp.Item2 != temp.Item3)
                    terminalSections.Add(temp);
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
        private void MaximumDensity()
        {
            var boundaryClearance = Constant.boundaryClearance;
            var verticalWireWidth = Constant.VerticalWireWidth;
            var temp = upper.Concat(lower);
            var element = new HashSet<string>(temp.Select(a => a.net));
            var IMOS = new Dictionary<double, double>();
            foreach (var net in element)
            {
                var foo = temp.Where(a => a.net == net);
                var min = foo.Min(a => a.xAxis) - verticalWireWidth / 2;
                var max = foo.Max(a => a.xAxis) + verticalWireWidth / 2;
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

            maxDensity = boundaryClearance * 2 + sum;
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
    }


}


