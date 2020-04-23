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

    internal static class Constant
    {

        internal const double boundaryClearance = 3;
        internal const double VerticalWireWidth = 10;

        internal const int minSpacing = 3;
    }

}