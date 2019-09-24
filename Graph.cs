using System;
using System.Collections.Generic;
using System.Linq;

namespace Yoshimura
{
    class Graph
    {
        IReadOnlyList<(int upper, int lower)> channel;
        readonly int MaxTerminalNumber;

        readonly int[,] VCGAdjacencyMatrix;
        readonly SortedSet<int>[] zone;

        readonly int[,] HCGAdjacencyMatrix;

        public Graph(IReadOnlyList<int> upper, IReadOnlyList<int> lower)
        {

            if (upper.Count == lower.Count)
            {
                var channel = new List<(int upper, int lower)>();
                var maxTerminalNumber = Math.Max(upper.Max(), lower.Max());
                this.MaxTerminalNumber = maxTerminalNumber;
                for (int i = 0; i < upper.Count; i++)
                {
                    channel.Add((upper[i], lower[i]));
                }
                this.channel = channel;
                this.VCGAdjacencyMatrix = GetVCGAdjacencyMatrix(upper, lower);
            }
            else
                throw new ArgumentException("Size of Lists are different");
        }


        ///Adjacency matrix
        private int[,] GetVCGAdjacencyMatrix(IReadOnlyList<int> upper, IReadOnlyList<int> lower)
        {
            //initialize
            var result = new int[upper.Count, lower.Count];
            for (int i = 0; i < result.Length; i++)
            {
                for (int j = 0; j < result.GetLength(i); j++)
                {
                    result[i, j] = 0;
                }
            }
            if (upper.Count == lower.Count)
            {
                for (int i = 0; i < upper.Count; i++)
                {
                    result[upper[i], lower[i]] = 1;
                }
            }
            return result;
        }

        private SortedSet<int>[] GetZoneRepresentation(IReadOnlyList<int> upper, IReadOnlyList<int> lower)
        {
            var lis = new List<SortedSet<int>>(MaxTerminalNumber);
            var result = new SortedSet<int>[upper.Count];
            // net が占有する範囲
            var rangeList = new List<(int left, int right)>();
            foreach (var item in upper.Select((a, index) => (a, index)))
            {
                if (item.a != 0)
                    lis[item.a - 1].Add(item.index);
            }
            foreach (var item in lower.Select((a, index) => (a, index)))
            {
                if (item.a != 0)
                    lis[item.a - 1].Add(item.index);
            }
            foreach (var item in lis)
            {
                var foo = (item.Min, item.Max);
                rangeList.Add(foo);
            }
            //各 Colと干渉するのをさがす(Sの探索)
            for (int i = 0; i < upper.Count; i++)
            {
                var foo = new SortedSet<int>();
                foreach (var item in rangeList.Select((a, index) => (a, index + 1)).
                Where(item => item.a.left <= i && i <= item.a.right))
                {
                    foo.Add(item.Item2);
                }
            }
            return result;
        }
    }
}