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

    internal class WeightedGraphSelection
    {
        Graph weightedDirectedGraph;
        Graph weightedUndirectedGraph;
        Dictionary<string, int> wires;
        internal double maxDensity { get; set; }
        WeightedGraphSelection(Graph weightedDirectedGraph, Graph weightedUndirectedGraph, double maxDensity)
        {
            this.weightedDirectedGraph = weightedDirectedGraph;
            this.weightedUndirectedGraph = weightedUndirectedGraph;
            this.maxDensity = maxDensity;
            Selection();
        }

        private void Selection()
        {
            while (true)
            {
                var node = NodeSelection();
                var edge = EdgeSelection();
                if (node == edge == true) break;
            }

        }


        //false で帰ってきたらNodeSelectionがいる。
        //trueで帰ってきたら、Unprocessed nodeがないので終了。
        private bool NodeSelection()
        {
            int count = 0;
            while (count++ < 1000)
            {
                var ancw = CreateChainWeight.Ancestor(weightedDirectedGraph);
                var decw = CreateChainWeight.Deanestor(weightedDirectedGraph);
                var LabelList = weightedUndirectedGraph.Edges.
                Select(a => (a, CalcLabel(a.Source, a.Target, ancw, decw))).
                OrderByDescending(a => a.Item2.Item1).ToList();
                //wire の割当が終了
                if (LabelList.Count == 0)
                    return true;

                //Goto NodeSelection
                if (LabelList.First().Item2.Item1 <= maxDensity)
                    return false;
                if (LabelList.Select(a => a.Item2.Item1).Contains((double)-1))
                    return false;
                AddEdgeWeightedDirectedGraph(LabelList.First().Item1, LabelList.First().Item2);
#if DEBUG
                Console.Write("Labels\t");
                LabelList.Select(a => a.Item2).ToString<(double, string, string)>().Write();
                Console.Write("ANCW\t");
                ancw.ToString<string, double>().Write();
                Console.Write("DECW\t");
                decw.ToString<string, double>().Write();
                weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
#endif
            }
            throw new Exception($"Beyond safety loop count {count}. If this value is not enough. Please change this limit");

        }

        bool EdgeSelection()
        {
            throw new NotImplementedException();
        }


        private (double, string, string) CalcLabel(string Source, string Target, Dictionary<string, double> ancw, Dictionary<string, double> decw)
        {
            ///論文にはサイクルができないようにするって書いてあるけど、無向グラフがあったらどうやってもサイクルできちゃうので、
            ///(VCG+すでに向きを割り当てたEdgeで、)サイクルができないようにするってことでいいのか？？？？多分そう。

            //VCGとHCGの両方で辺がはられている場合は、VCGの向きで有効辺をはり、重さはHCGから取る。
            //DAGを維持するためには、トポロジカルソートができなかったらその逆向きに貼れば良い
            var tempGraph = new Graph();
            tempGraph.AddVertexRange(weightedDirectedGraph.Vertices);
            tempGraph.AddEdgeRange(weightedDirectedGraph.Edges);
            var guid = Guid.NewGuid();
            var EdgeIJ = weightedUndirectedGraph.Edges.Where(a => a.Target == Target && a.Source == Source).First();
            //i->jが行けるか試す。
            var edge = new Edge(guid.ToString(), Source, Target, EdgeIJ.Weight);
            tempGraph.AddEdge(edge);
            try
            {
                tempGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                return (double.PositiveInfinity, Target, Source);
            }
            tempGraph.RemoveEdge(edge);
            //j->iが行けるか試す。
            edge = new Edge(guid.ToString(), Target, Source, EdgeIJ.Weight);
            tempGraph.AddEdge(edge);
            try
            {
                tempGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                return (double.PositiveInfinity, Source, Target);
            }
            tempGraph.RemoveEdge(edge);
            //どっちもできるので、実数を投げる。
            var left = ancw[Source] + decw[Target] + MinSeparation(Source, Target);
            var right = ancw[Target] + decw[Source] + MinSeparation(Source, Target);
            //left==rightのときは、枝刈り終了を意味するはず
            if (left == right)
            {
                return (Math.Max(left, right), "-1", "-1");
                //Node Selection行き
            }
            else if (left > right)
            {
                return (Math.Max(left, right), Source, Target);
            }
            else
            {
                return (Math.Max(left, right), Target, Source);
            }
        }

        private void AddEdgeWeightedDirectedGraph(Edge removeEdge, (double Label, string Source, string Target) addedge)//いい名前がない
        {
            weightedUndirectedGraph.RemoveEdge(removeEdge);
            Edge edge;
            weightedDirectedGraph.TryGetEdge(addedge.Source, addedge.Target, out edge);
            if (edge == null)//this is new edge
            {
                edge = new Edge($"net{addedge.Source}{addedge.Target}", addedge.Source, addedge.Target, removeEdge.Weight);
            }
            else
            {
                weightedDirectedGraph.RemoveEdge(edge);
                edge.Weight = Math.Max(edge.Weight, removeEdge.Weight);
            }
            weightedDirectedGraph.AddEdge(edge);
        }


        private double MinSeparation(string i, string j)
        {
            return Constant.minSpacing + wires[i] / 2 + wires[j] / 2;
        }


    }
}