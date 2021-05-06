using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace NOS.Lab1.Zad1b
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand()
            {
                new Argument<int>(name: "id", description: "Node ID"),
                new Argument<int>(name: "peers", description: "Number of peer nodes"),
                new Argument<int>(name: "runs", description: "Number of runs"),
                new Argument<string>(name: "dbFile", description: "Database file path"),
            };

            rootCommand.Handler = CommandHandler.Create<int, int, int, string>(RunAsync);

            await rootCommand.InvokeAsync(args);
        }

        static async Task RunAsync(int id, int peers, int runs, string dbFile)
        {
            var db = new MMFDatabase(dbFile, 1024);
            var node = new Node(id, peers, runs, db);
            await node.StartAsync();

            Console.WriteLine($"{id}: EXIT");
        }
    }
}
