using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace NOS.Lab1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Klijent");

            await RunClientAsync();
        }

        static async Task RunClientAsync()
        {
            using var pipeClient = new NamedPipeClientStream(".", "testpipe", PipeDirection.Out);
            await pipeClient.ConnectAsync();
            Console.WriteLine("Connected to server.");

            using var sw = new StreamWriter(pipeClient)
            {
                AutoFlush = true,
            };
            string? line;
            while ((line = Console.ReadLine()) is not null)
            {
                await sw.WriteLineAsync(line);
            }
        }
    }
}
