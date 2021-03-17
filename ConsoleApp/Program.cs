using System;

namespace NOS.Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Spock();
        }

        static void Kirk()
        {
            var message = new Message(1L, "Kirk: We are attacked. Spock, send reinforcement.");
            int key = 12345;
            var queue = MessageQueue.GetOrCreate(key, Permissions.UserReadWrite);
            queue.Send(ref message);
            Console.WriteLine("Sent");
        }

        static void Spock()
        {
            Message message = default;
            int key = 12345;

            var queue = MessageQueue.GetOrCreate(key, Permissions.UserReadWrite);
            Console.CancelKeyPress += (_, _) => queue.Delete();

            try
            {
                while(true)
                {
                    queue.Receive(ref message, 0, 0);
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
