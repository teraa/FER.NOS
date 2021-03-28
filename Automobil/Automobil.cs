using System;

namespace NOS.Lab1
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"args: <id> <direction=(0|1)");
                return;
            }

            var id = int.Parse(args[0]);
            var direction = int.Parse(args[1]);
            if (direction is not (0 or 1))
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction value must be 0 or 1");

            new Automobil(id, direction).Run();
        }
    }

    class Automobil
    {
        public const int QUEUE_KEY = 5000;

        readonly int _id;
        readonly int _direction;

        public Automobil(int id, int direction)
        {
            _id = id;
            _direction = direction;
        }

        public void Run()
        {
            var queue = MessageQueue.GetOrCreate(QUEUE_KEY, Permissions.UserReadWrite);

            // Pošalji zahtjev
            var message = new Message(
                type: MessageType.Request,
                carId: _id,
                direction: _direction
            );
            queue.Send(message);
            Console.WriteLine($"Automobil {_id,3} (smjer {_direction}) čeka na prelazak preko mosta");

            // Čekaj dozvolu za prijelaz
            if (!queue.TryReceive(ref message, MessageType.BeginLeft + _direction))
                return;
            Console.WriteLine($"Automobil {_id,3} (smjer {_direction}) se popeo na most");

            // Čekaj završetak prijelaza
            if (!queue.TryReceive(ref message, MessageType.EndLeft + _direction))
                return;
            Console.WriteLine($"Automobil {_id,3} (smjer {_direction}) je prešao most");
        }
    }
}
