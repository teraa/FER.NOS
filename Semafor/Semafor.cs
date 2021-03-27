using System;

namespace NOS.Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Semafor");

            new Semaphore().Run();
        }
    }

    class Semaphore
    {
        public const int QUEUE_KEY = 5000;

        private readonly Random _rnd;
        private int _direction;

        public Semaphore()
        {
            _rnd = new Random();
            _direction = _rnd.Next(0, 2);
        }

        public void Run()
        {
            var queue = MessageQueue.GetOrCreate(QUEUE_KEY, Permissions.UserReadWrite);
            Console.CancelKeyPress += (_, _) => queue.Delete();

            try
            {
                MyMessage message = new();

                while(true)
                {
                    Console.WriteLine($"Direction: {_direction}");
                    Console.Write("> ");
                    queue.Receive(ref message, MessageType.Request);
                    Console.WriteLine(message);

                    message = new MyMessage(
                        type: MessageType.Begin,
                        carId: message.CarId,
                        direction: _direction
                    );
                    queue.Send(message);


                    message = new MyMessage(
                        type: MessageType.End,
                        carId: message.CarId,
                        direction: _direction
                    );
                    queue.Send(message);

                    _direction ^= 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
