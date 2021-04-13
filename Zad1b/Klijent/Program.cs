using System;
using System.IO;
using System.IO.Pipes;

namespace NOS.Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Klijent");

            using var pipeClient = new NamedPipeClientStream(".", "baza", PipeDirection.InOut);
            pipeClient.Connect();
            Console.WriteLine("Connected");

            Write(pipeClient);
            Console.WriteLine("done");
        }

        static void Write(Stream stream)
        {
            using var sw = new StreamWriter(stream)
            {
                AutoFlush = true,
            };

            string? line;
            while ((line = Console.ReadLine()) is not null)
            {
                sw.WriteLine(line);
            }
        }
    }
}