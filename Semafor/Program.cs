﻿using System;

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
        readonly Random _rnd;
        Direction _direction;

        public Semaphore()
        {
            _rnd = new Random();
            _direction = _rnd.Next(0, 2) == 0 ? Direction.North : Direction.South;
        }

        public void Run()
        {
            var queue = MessageQueue.GetOrCreate(Consts.QUEUE_KEY, Permissions.UserReadWrite);
            Console.CancelKeyPress += (_, _) => queue.Delete();

            try
            {
                Message message = default;

                while(true)
                {
                    Console.WriteLine($"Direction: {_direction}");
                    Console.Write("> ");
                    queue.Receive(ref message, (long)MessageType.Request | (long)_direction);
                    Console.WriteLine(message);

                    message = new Message(
                        type: (long)MessageType.Begin | (long)_direction,
                        text: $"Sem: Begin {_direction}"
                    );
                    queue.Send(ref message);


                    message = new Message(
                        type: (long)MessageType.End | (long)_direction,
                        text: $"Sem: End {_direction}"
                    );
                    queue.Send(ref message);

                    if (_direction == Direction.North)
                        _direction = Direction.South;
                    else
                        _direction = Direction.North;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}