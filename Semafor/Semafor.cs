using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NOS.Lab1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Semafor");

            var semafor = new Semafor();
            await semafor.RunAsync();
        }
    }

    class Semafor
    {
        private const int QUEUE_KEY = 5000;
        private const int REQUESTS_TRESHOLD = 3;

        private readonly Random _rnd;
        private int _direction;
        private Queue<Message>[] _requestQueues;
        private SemaphoreSlim _sem;
        private SemaphoreSlim _processSem;

        public Semafor()
        {
            _rnd = new Random();
            _direction = _rnd.Next(0, 2);
            _requestQueues = new Queue<Message>[] { new(), new() };
            _sem = new SemaphoreSlim(1, 1);
            _processSem = new SemaphoreSlim(0, 1);
        }

        public void Listen(MessageQueue queue)
        {
            Console.WriteLine("Listening...");
            try
            {
                while (true)
                {
                    Message message = new();

                    queue.Receive(ref message, MessageType.Request);
                    Console.WriteLine(message);
                    _sem.Wait();
                    try
                    {

                        _requestQueues[message.Direction].Enqueue(message);

                        if (_requestQueues[_direction].Count >= REQUESTS_TRESHOLD)
                            _processSem.Release(); // EXCEPTION
                    }
                    finally
                    {
                        _sem.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task RunAsync()
        {
            try
            {
                var queue = MessageQueue.GetOrCreate(QUEUE_KEY, Permissions.UserReadWrite);

                Console.CancelKeyPress += (_, _) =>
                {
                    queue.Delete();
                };

                var rts = new CancellationTokenSource(); // TODO: Timeout
                _ = Task.Run(() => Listen(queue));

                while (true)
                {
                    var timeout = _rnd.Next(500, 1000);
                    Console.WriteLine($"Waiting for batch of {REQUESTS_TRESHOLD} requests or {timeout} ms, direction={_direction}");
                    await _processSem.WaitAsync(timeout);

                    Message[] requests;
                    await _sem.WaitAsync();
                    try
                    {
                        var requestCount = _requestQueues[_direction].Count;
                        if (requestCount > REQUESTS_TRESHOLD)
                            requestCount = REQUESTS_TRESHOLD;

                        requests = new Message[requestCount];
                        for (int i = 0; i < requests.Length; i++)
                            requests[i] = _requestQueues[_direction].Dequeue();

                        _direction ^= 1;
                    }
                    finally
                    {
                        _sem.Release();
                    }


                    var tasks = requests.Select(x => ProcessRequest(queue, x.CarId, x.Direction))
                        .ToArray();

                    if (tasks.Length > 0)
                        Console.WriteLine($"Waiting for {tasks.Length} tasks.");

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task ProcessRequest(MessageQueue queue, int carId, int direction)
        {
            var message = new Message();
            message.CarId = carId;
            message.Direction = direction;

            message.Type = MessageType.BeginLeft + direction;
            queue.Send(message);

            var delay = _rnd.Next(1000, 3000);
            await Task.Delay(delay);

            message.Type = MessageType.EndLeft + direction;
            queue.Send(message);
        }
    }
}
