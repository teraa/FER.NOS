using System;
using System.IO;
using System.Threading.Tasks;

namespace NOS.Lab1.Zad1b
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 2
                || !int.TryParse(args[0], out var peers)
                || !int.TryParse(args[1], out var id))
            {
                Usage();
                return;
            }

            if (peers < 1)
            {
                Console.WriteLine("Error: peers < 1");
                Usage();
                return;
            }

            if (id < 0)
            {
                Console.WriteLine("Error: pid < 0");
                Usage();
                return;
            }

            if (id > peers)
            {
                Console.WriteLine("Error: pid > peers");
                Usage();
                return;
            }

            static void Usage() => Console.WriteLine("Usage: <peers> <id>");

            var db = new MMFDatabase("data.db", 1024);
            var node = new Node(id, peers, db);
            await node.StartAsync();
        }
    }
}
