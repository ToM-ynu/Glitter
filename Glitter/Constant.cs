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

        internal const double boundaryClearance = 2.5;

        internal const decimal boundaryClearanceDecimal = 2.5M;

        internal const int minSpacing = 4;
    }

}