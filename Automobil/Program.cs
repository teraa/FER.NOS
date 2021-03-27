using System;

namespace NOS.Lab1
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"args: <id> <direction=(0|1)");
                return 1;
            }

            var id = int.Parse(args[0]);
            var direction = int.Parse(args[1]);
            if (direction is not (0 or 1))
                throw new ArgumentOutOfRangeException();

            Console.WriteLine($"Automobil ID={id}, Direction={direction}");

            new Car(id, direction).Run();

            return 0;
        }
    }

    class Car
    {
        readonly int _id;
        readonly int _direction;

        public Car(int id, int direction)
        {
            _id = id;
            _direction = direction;
        }

        public void Run()
        {
            var queue = MessageQueue.GetOrCreate(Consts.QUEUE_KEY, Permissions.UserReadWrite);
            try
            {
                TextMessage message = new TextMessage(
                    type: (long)MessageType.Request | (long)_direction,
                    text: $"{_id}: Request {_direction}"
                );

                queue.Send(ref message);
                Console.WriteLine($"Automobil {_id} čeka na prelazak preko mosta");

                queue.Receive(ref message, (long)MessageType.Begin | (long)_direction);
                // Console.WriteLine($"> {message}");
                Console.WriteLine($"Automobil {_id} se popeo na most");

                queue.Receive(ref message, (long)MessageType.End | (long)_direction);
                // Console.WriteLine($"> {message}");
                Console.WriteLine($"Automobil {_id} je prešao most");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
