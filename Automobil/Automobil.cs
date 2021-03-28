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

            // Console.WriteLine($"Automobil ID={id}, Direction={direction}");

            new Automobil(id, direction).Run();

            return 0;
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

                if (!queue.TryReceive(ref message, MessageType.BeginLeft + _direction))
                    return;
                // Console.WriteLine($"> {message}");
                Console.WriteLine($"Automobil {_id,3} (smjer {_direction}) se popeo na most");

                if (!queue.TryReceive(ref message, MessageType.EndLeft + _direction))
                    return;
                // Console.WriteLine($"> {message}");
                Console.WriteLine($"Automobil {_id,3} (smjer {_direction}) je prešao most");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
