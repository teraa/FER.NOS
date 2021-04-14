using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace NOS.Lab1
{
    class Program
    {
        private const int INSTANCES = 2;

        static async Task Main(string[] args)
        {
            Console.WriteLine($"Baza x{INSTANCES}");

            var tasks = new Task[INSTANCES];
            for (int i = 0; i < INSTANCES; i++)
            {
                tasks[i] = RunServerAsync(i);
            }

            await Task.WhenAll(tasks);
        }

        static async Task RunServerAsync(int id)
        {
            using var pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.In, INSTANCES);
            await pipeServer.WaitForConnectionAsync();
            Console.WriteLine($"Client connected to {id} server");

            using var sr = new StreamReader(pipeServer);
            string? line;
            while ((line = await sr.ReadLineAsync()) is not null)
            {
                Console.WriteLine($"[{id}] > {line}");
            }

            Console.WriteLine($"[{id}] Done");
        }
    }
}
