using System;
using System.IO;
using System.Threading.Tasks;

namespace NOS.Lab1.Zad1b
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            const string dbFilePath = "data.db";
            using var db = new MMFDatabase(dbFilePath, 1024);
            var nodes = new Node[3];
            var relay = new Relay(nodes);
            for (int i = 0; i < nodes.Length; i++)
                nodes[i] = new Node(i, nodes.Length - 1, db, relay);

            var tasks = new Task[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                tasks[i] = nodes[i].StartAsync();

            await Task.WhenAll(tasks);

            File.Delete(dbFilePath);

            Console.WriteLine("Done");
        }
    }
}
