using System;
using System.IO;
using System.IO.Pipes;

namespace NOS.Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Baza");

            using var pipeServer = new NamedPipeServerStream("baza", PipeDirection.InOut);
            pipeServer.WaitForConnection();
            Console.WriteLine("Connected");

            Read(pipeServer);
            Console.WriteLine("done");
        }

        static void Read(Stream stream)
        {
            using var sr = new StreamReader(stream);
            string? line;
            while ((line = sr.ReadLine()) is not null)
            {
                Console.WriteLine($"> {line}");
            }
        }
    }
}
