using System;
using System.Collections.Concurrent;
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
        private ConcurrentQueue<Message>[] _requestQueues;
        private SemaphoreSlim _runSem;
        private CancellationTokenSource _cts = null!;

        public Semafor()
        {
            _rnd = new Random();
            _direction = _rnd.Next(0, 2);
            _requestQueues = new ConcurrentQueue<Message>[] { new(), new() };

            _runSem = new SemaphoreSlim(0, 1);
        }

        private void OnTokenCancelled()
        {
            _runSem.Release();
        }

        public void Listen(MessageQueue queue)
        {
            try
            {
                while (true)
                {
                    Message message = new();

                    queue.Receive(ref message, MessageType.Request);
                    Console.WriteLine($"Zahtjev za prijelaz: automobil {message.CarId}, smjer {message.Direction}");

                    _requestQueues[message.Direction].Enqueue(message);

                    if (_requestQueues[_direction].Count >= REQUESTS_TRESHOLD)
                    {
                        _cts.Cancel();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(1);
            }
        }

        public async Task RunAsync()
        {
            try
            {
                var queue = MessageQueue.GetOrCreate(QUEUE_KEY, Permissions.UserReadWrite);

                Console.CancelKeyPress += (_, _) =>
                {
                    try
                    {
                        queue.Delete();
                    }
                    catch { }
                };

                var rts = new CancellationTokenSource();
                _ = Task.Run(() => Listen(queue));

                while (true)
                {
                    var timeout = _rnd.Next(500, 1000);
                    _cts = new CancellationTokenSource(timeout);
                    _cts.Token.Register(OnTokenCancelled);
                    // Console.WriteLine($"Waiting for {REQUESTS_TRESHOLD} requests or {timeout} ms, direction={_direction}");

                    await _runSem.WaitAsync();

                    var direction = _direction;
                    var carIds = new List<int>();
                    for (int i = 0; i < REQUESTS_TRESHOLD && _requestQueues[direction].TryDequeue(out var req); i++)
                        carIds.Add(req.CarId);

                    _direction ^= 1;

                    if (carIds.Count > 0)
                        await ProcessRequests(queue, carIds, direction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task ProcessRequests(MessageQueue queue, IReadOnlyList<int> carIds, int direction)
        {
            Console.WriteLine($"Propuštam {carIds.Count} automobila u smjeru {direction} ({string.Join(", ", carIds)})");

            var tasks = carIds.Select(id => ProcessRequest(queue, id, direction))
                .ToArray();

            await Task.WhenAll(tasks);
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
