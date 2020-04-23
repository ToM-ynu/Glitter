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
        internal Graph VerticalGraph { get => verticalGraph; private set => verticalGraph = value; }
        internal Graph HorizontalGraph { get => horizontalGraph; private set => horizontalGraph = value; }
        private Dictionary<string, int> wires;
        private IEnumerable<Terminal> upper;
        private IEnumerable<Terminal> lower;
        internal double MaxDensity { get => maxDensity; private set => maxDensity = value; }

        internal Dictionary<string, double> LocalMaximumDensity { get => localMaximumDensity; private set => localMaximumDensity = value; }
        private Graph horizontalGraph;
        private Graph verticalGraph;
        private double maxDensity;
        private Dictionary<string, double> localMaximumDensity;

        internal CreateGraph(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, int> wires)
        {
            this.upper = upper;
            this.lower = lower;
            this.wires = wires;
            VerticalGraph = new Graph();
            HorizontalGraph = new Graph();
            CreateVerticalGraph();
            CreateHorizontalGraph();
            CalcLocalMaximumDensity();
        }

        private void CreateVerticalGraph()
        {
            var nets = new HashSet<string>(upper.Select(a => a.net).Concat(lower.Select(b => b.net)));
            VerticalGraph.AddVertexRange(nets);
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
                    VerticalGraph.AddEdge(temp);
                }
            }
            //To checking graph is DAG or not, we are using exception of topologicalsort.
            //try-catch is very slow.
            try
            {
                VerticalGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {

                "VCG".WriteLine();
                VerticalGraph.Edges.ToString<Edge>().Write();
                Console.WriteLine("This is non-DAG graph. By LEA, there is no solution.");
                Environment.Exit(1);

            }
        }
        private void CreateHorizontalGraph()
        {
            var nets = new HashSet<string>(upper.Select(a => a.net).Concat(lower.Select(b => b.net)));
            HorizontalGraph.AddVertexRange(nets);
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
                        HorizontalGraph.AddEdge(temp);
                    }
                }
            }
        }

        private void CalcLocalMaximumDensity()
        {
            var boundaryClearance = Constant.boundaryClearance;
            var verticalWireWidth = Constant.VerticalWireWidth;
            var terminals = upper.Concat(lower);
            var IMOS = new Dictionary<double, double>();
            foreach (var net in new HashSet<string>(terminals.Select(a => a.net)))
            {
                var foo = terminals.Where(a => a.net == net);
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

            var temp = IMOS.Select(a => (a.Key, a.Value)).OrderBy(a => a.Key).ToList();
            //累積和取らないといかんでしょ
            var value = 0.0;
            for (var i = 0; i < temp.Count; i++)
            {
                value += temp[i].Value;
                temp[i] = (temp[i].Key, value);
            }
            LocalMaximumDensity = new Dictionary<string, double>();
            foreach (var net in new HashSet<string>(terminals.Select(a => a.net)))
            {
                var foo = terminals.Where(a => a.net == net);
                var min = foo.Min(a => a.xAxis) - verticalWireWidth / 2;
                var minIndex = temp.FindIndex(a => a.Key == min);
                var max = foo.Max(a => a.xAxis) + verticalWireWidth / 2;
                var maxIndex = temp.FindIndex(a => a.Key == max);
                //min~max間での最大Valueを探せば良い。
                var density = temp.GetRange(minIndex, Math.Abs(maxIndex - minIndex)).Select(a => a.Value).Max();
                LocalMaximumDensity.Add(net, density + boundaryClearance * 2);
            }
            MaxDensity = LocalMaximumDensity.Select(a => a.Value).Max();
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


