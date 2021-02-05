using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        Dictionary<string, (int upper, int horizontal, int lower)> wires;
        private IEnumerable<Terminal> upper;
        private IEnumerable<Terminal> lower;
        internal double MaxDensity { get => maxDensity; private set => maxDensity = value; }

        internal Dictionary<string, double> LocalMaximumDensity { get => localMaximumDensity; private set => localMaximumDensity = value; }
        private Graph horizontalGraph;
        private Graph verticalGraph;
        private double maxDensity;
        private Dictionary<string, double> localMaximumDensity;

        internal bool IsDAG = false;

        internal CreateGraph(IEnumerable<Terminal> upper, IEnumerable<Terminal> lower, Dictionary<string, (int upper, int horizonal, int lower)> wires)
        {
            this.upper = upper;
            this.lower = lower;
            this.wires = wires;
            VerticalGraph = new Graph();
            HorizontalGraph = new Graph();
            IsDAG = CreateVerticalGraph();
            if (!IsDAG)
            {
                CreateHorizontalGraph();
                CalcLocalMaximumDensity();
            }
            {
                Console.WriteLine("DGP no VCG problem solver is runnneing !!!!!!!");
                if (horizontalGraph.Edges.Count() != 0)
                {
                    var ioCountDict = new Dictionary<string, int>();
                    foreach (var edge in horizontalGraph.Edges)
                    {
                        ioCountDict[edge.Target] = 0;
                    }
                    //入ってくる量が最大のやつを一番下にするのがいいのではないかという仮説
                    foreach (var edge in horizontalGraph.Edges)
                    {
                        ioCountDict[edge.Target]++;
                    }
                    var inMax = ioCountDict.OrderByDescending(a => a.Value).First().Key;
                    var edges = horizontalGraph.Edges.Where(a => a.Target == inMax).ToList();
                    foreach (var maxEdge in edges)
                    {
                        verticalGraph.AddEdge(maxEdge);
                        horizontalGraph.RemoveEdge(maxEdge);
                    }

                }
            }
        }

        private bool CreateVerticalGraph()
        {
            var nets = new HashSet<string>(upper.Select(a => a.net).Concat(lower.Select(b => b.net)));
            VerticalGraph.AddVertexRange(nets);

            //position of terminal ± (width of Vertical wire +2* clearance)/2 is
            foreach (var upperTerminal in upper)
            {
                var verticalColisionList = lower.Where(a => IsVerticalColision(upperTerminal, a)).ToList();
                if (verticalColisionList.Count == 0) continue;
                else
                {
                    var hoge = IsVerticalColision(upperTerminal, verticalColisionList.First());
                    foreach (var colisionUpper in verticalColisionList)
                    {
                        if (upperTerminal.net == colisionUpper.net) continue; //avoid self-loops
                        var temp = new Edge("net" + upperTerminal.net, upperTerminal.net, colisionUpper.net, 1);
                        VerticalGraph.AddEdge(temp);
                    }

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
                Console.WriteLine("VCG");
                VerticalGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                Console.WriteLine("This is non-DAG graph. By LEA, there is no solution.");
                return true;
            }
            return false;
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
                    var weight = Constant.minSpacing + wires[source.net].horizontal / 2.0 + wires[target.net].horizontal / 2.0;
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
            var minSpacing = Constant.minSpacing;
            var terminals = upper.Concat(lower);
            var IMOS = new Dictionary<double, double>();
            foreach (var net in new HashSet<string>(terminals.Select(a => a.net)))
            {
                var horizontalWireWidth = wires[net].horizontal + minSpacing;
                var leftVerticalWireWidth = (wires[net].upper + minSpacing) / 2.0;
                var rightVerticalWireWidth = (wires[net].lower + minSpacing) / 2.0;
                var foo = terminals.Where(a => a.net == net);
                var left = foo.Min(a => a.xAxis) - leftVerticalWireWidth;
                var right = foo.Max(a => a.xAxis) + rightVerticalWireWidth;
                if (IMOS.ContainsKey(left))
                {
                    IMOS[left] += horizontalWireWidth;
                }
                else
                {
                    IMOS[left] = horizontalWireWidth;
                }

                if (IMOS.ContainsKey(right))
                {
                    IMOS[right] += -horizontalWireWidth;
                }
                else
                {
                    IMOS[right] = -horizontalWireWidth;
                }
            }

            var maxmumDensityList = new List<(double Key, double Value)>();
            IMOS.Select(a => (a.Key, a.Value)).OrderBy(a => a.Key).ToList();
            //累積和取らないといかんでしょ
            var value = 0.0;
            foreach (var (Key, Value) in IMOS.Select(a => (a.Key, a.Value)).OrderBy(a => a.Key))
            {
                value += Value;
                maxmumDensityList.Add((Key, value));
            }
            LocalMaximumDensity = new Dictionary<string, double>();
            foreach (var net in new HashSet<string>(terminals.Select(a => a.net)))
            {
                var leftVerticalWireWidth = (wires[net].upper + minSpacing) / 2.0;
                var rightVerticalWireWidth = (wires[net].lower + minSpacing) / 2.0;
                var terminal = terminals.Where(a => a.net == net);
                var left = terminal.Min(a => a.xAxis) - leftVerticalWireWidth;
                var leftIndex = maxmumDensityList.FindIndex(a => a.Key == left);
                var right = terminal.Max(a => a.xAxis) + rightVerticalWireWidth;
                var rightIndex = maxmumDensityList.FindIndex(a => a.Key == right);
                //left~right間での最大Valueを探せば良い。
                var density = maxmumDensityList.GetRange(leftIndex, Math.Abs(rightIndex - leftIndex)).Select(a => a.Value).Max();
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

        private bool IsVerticalColision(Terminal upper, Terminal lower)
        {
            var temp = new List<(decimal, decimal)>();
            temp.Add((upper.xAxis - (Constant.boundaryClearanceDecimal + wires[upper.net].upper) / 2, upper.xAxis + (Constant.boundaryClearanceDecimal + wires[upper.net].upper) / 2));
            temp.Add((lower.xAxis - (Constant.boundaryClearanceDecimal + wires[lower.net].lower) / 2, lower.xAxis + (Constant.boundaryClearanceDecimal + wires[lower.net].lower) / 2));
            temp.Sort();
            return temp[0].Item1 < temp[1].Item1 && temp[1].Item1 < temp[0].Item2;
        }
    }


}


