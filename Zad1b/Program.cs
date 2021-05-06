using System;
using System.IO;
using System.Threading.Tasks;

namespace NOS.Lab1.Zad1b
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 4
                || !int.TryParse(args[0], out var id)
                || !int.TryParse(args[1], out var peers)
                || !int.TryParse(args[2], out var runCount))
            {
                Usage();
                return;
            }

            if (id < 0)
            {
                Console.WriteLine("Error: pid < 0");
                Usage();
                return;
            }

            if (peers < 1)
            {
                Console.WriteLine("Error: peers < 1");
                Usage();
                return;
            }

            if (id > peers)
            {
                Console.WriteLine("Error: pid > peers");
                Usage();
                return;
            }

            if (runCount < 1)
            {
                Console.WriteLine("Error: runCount < 1");
                Usage();
                return;
            }

            var dbFilePath = args[3];

            static void Usage() => Console.WriteLine("Usage: <id> <peers> <runs> <dbFilePath>");

            var db = new MMFDatabase(dbFilePath, 1024);
            var node = new Node(id, peers, runCount, db);
            await node.StartAsync();

            Console.WriteLine($"{id}: EXIT");
        }
    }
}
