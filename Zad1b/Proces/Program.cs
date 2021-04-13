using System;

namespace NOS.Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2
                || !int.TryParse(args[0], out var n)
                || !int.TryParse(args[1], out var id))
            {
                Usage();
                return;
            }

            if (n < 2)
            {
                Console.WriteLine("Error: N must be greater than 1");
                Usage();
                return;
            }

            if (id < 0)
            {
                Console.WriteLine("Error: ID must be a positive number.");
                Usage();
                return;
            }

            if (id >= n)
            {
                Console.WriteLine("Error: ID must be less than N");
                Usage();
                return;
            }

            Run(n, id);
        }

        static void Usage()
        {
            Console.WriteLine("Usage: <N> <ID>");
        }

        static void Run(int n, int id)
        {
            Console.WriteLine($"N={n}, ID={id}");
        }
    }
}
