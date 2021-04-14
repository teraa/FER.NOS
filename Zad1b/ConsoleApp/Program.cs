using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var mmf = MemoryMappedFile.CreateFromFile("test.data", FileMode.OpenOrCreate, null, 1024);

            string? content;

            using (var stream = mmf.CreateViewStream())
            {
                using var sr = new StreamReader(stream);
                content = sr.ReadToEnd();
            }

            Console.WriteLine("Content:");
            Console.WriteLine(content);


            Console.Write("Enter new content: ");
            content = Console.ReadLine();
            using (var stream = mmf.CreateViewStream())
            {
                using var sw = new StreamWriter(stream)
                {
                    AutoFlush = true,
                };
                sw.Write(content);
            }
        }
    }
}
