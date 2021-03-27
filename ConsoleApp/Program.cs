using System;
using System.Runtime.InteropServices;

namespace NOS.Lab1
{
    class Program
    {
        const string DLL_NAME = "../shared/rnd.so";
        [DllImport(DLL_NAME)] static extern void print(string message);
        [DllImport(DLL_NAME)] static extern void test_struct(ref TextMessage value);
        [DllImport(DLL_NAME)] static extern void test_chars(string text);

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            print("test");

            Spock();
        }

        static void Kirk()
        {
            var message = new TextMessage(1L, "Kirk: We are attacked. Spock, send reinforcement.");
            int key = 12345;
            var queue = MessageQueue.GetOrCreate(key, Permissions.UserReadWrite);
            queue.Send(ref message);
            Console.WriteLine("Sent");
        }

        static void Spock()
        {
            TextMessage message = default;
            int key = 12345;

            var queue = MessageQueue.GetOrCreate(key, Permissions.UserReadWrite);
            Console.CancelKeyPress += (_, _) => queue.Delete();

            try
            {
                while(true)
                {
                    queue.Receive(ref message);
                    Console.WriteLine($"> {message.Text}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
