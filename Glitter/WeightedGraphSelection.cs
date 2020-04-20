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
        internal double maxDensity { get;set; }
        WeightedGraphSelection(Graph weightedDirectedGraph, Graph weightedUndirectedGraph,double maxDensity)
        {
            this.weightedDirectedGraph = weightedDirectedGraph;
            this.weightedUndirectedGraph = weightedUndirectedGraph;
            this.maxDensity = maxDensity;
        }

        private void NodeSelection()
        {
            var count = 0;
            while (count != 10)
            {
                Console.Write("ANCW\t");
                var ancw = CreateChainWeight.Ancestor(weightedDirectedGraph);
                ancw.ToString<string, double>().Write();
                Console.Write("DECW\t");
                var decw = CreateChainWeight.Deanestor(weightedDirectedGraph);
                decw.ToString<string, double>().Write();
                var LabelList = weightedUndirectedGraph.Edges.
                Select(a => (a, CalcLabel(a.Source, a.Target, ancw, decw))).
                OrderByDescending(a => a.Item2.Item1).ToList();
                Console.Write("Labels\t");
                LabelList.Select(a => a.Item2).ToString<(double, string, string)>().Write();
                if (LabelList.Count == 0 || LabelList.First().Item2.Item1 < maxDensity) break;
                AddEdgeWeightedDirectedGraph(LabelList.First().Item1, LabelList.First().Item2);
                Console.WriteLine(count);
                weightedDirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                weightedUndirectedGraph.Edges.ToString<Edge>(format: "{0}\n", end: "", begin: "").Write();
                count++;
            }

        }

        void EdgeSelection()
        {

        }


        private (double, string, string) CalcLabel(string Source, string Target, Dictionary<string, double> ancw, Dictionary<string, double> decw)
        {
            ///論文にはサイクルができないようにするって書いてあるけど、無向グラフがあったらどうやってもサイクルできちゃうので、
            ///(VCG+すでに向きを割り当てたEdgeで、)サイクルができないようにするってことでいいのか？？？？多分そう。

            //VCGとHCGの両方で辺がはられている場合は、VCGの向きで有効辺をはり、重さはHCGから取る。
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