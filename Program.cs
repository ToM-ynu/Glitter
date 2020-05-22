using System;

namespace Glitter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Environment.Exit(1);
            }
            else
            {
                var arg1 = "Input/" + args[0] ;
                var arg2 = "Input/" + args[1] ;
                var channel = new Channel(arg1, arg2);
            }

        }

    }
}
