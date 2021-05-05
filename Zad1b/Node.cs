using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NOS.Lab1.Zad1b
{
    class Node
    {
        private const int RUN_COUNT = 2;

        private int _id;
        private int _peers;
        private Database _db;
        private int _timestamp;
        private int _requestTimestamp;
        private int _count;
        private bool _isAccessRequested;
        private Queue<(Message message, int targetId)> _sendQueue;
        private SemaphoreSlim _sem;
        private Random _rnd;
        private object _receiveLock;
        private Relay _relay;

        public Node(int id, int peers, Database db, Relay relay)
        {
            _id = id;
            _peers = peers;
            _db = db;
            _timestamp = 1;
            _requestTimestamp = 0;
            _count = 0;
            _isAccessRequested = false;
            _sendQueue = new();
            _sem = new SemaphoreSlim(0, _peers);
            _rnd = new Random();
            _receiveLock = new object();
            _relay = relay;
        }

        public async Task StartAsync()
        {
            for (int i = 0; i < RUN_COUNT; i++)
            {
                await RunAsync();
            }
        }

        public async Task RunAsync()
        {
            // Console.WriteLine($"{_id}: Start");

            // Posalji zahtjev svim procesima
            _isAccessRequested = true;
            _requestTimestamp = _timestamp;
            var request = new Message(MessageType.Request, _id, _requestTimestamp);
            Broadcast(request);

            // Pricekaj odgovore svih procesa
            // Console.WriteLine($"{_id}: Waiting for {_peers} responses.");
            for (int i = 0; i < _peers; i++)
                await _sem.WaitAsync();

            // K.O. START

            Console.WriteLine($"{_id}: K.O. Start");
            await RunCriticalAsync();
            // await Task.Delay(2000);
            Console.WriteLine($"{_id}: K.O. End");

            // K.O. KRAJ
            _isAccessRequested = false;

            // Posalji odgovor svim procesima koji cekaju na odgovor
            while (_sendQueue.TryDequeue(out var item))
                Send(item.message, item.targetId);

            // Console.WriteLine($"{_id}: End");
        }

        private async Task RunCriticalAsync()
        {
            _count++;

            var myEntry = new DbEntry(_id, _timestamp, _count);
            _db.Update(myEntry);

            Console.WriteLine($"{_id}: Ispis baze");
            foreach (var entry in _db.GetAll())
                Console.WriteLine($"{entry}");
            Console.WriteLine("--");

            await Task.Delay(_rnd.Next(100, 2000));
        }

        private void Broadcast(Message message)
        {
            for (int i = 0; i <= _peers; i++)
                if (i != _id)
                    Send(message, i);
        }

        private void Send(Message message, int targetId)
        {
            // Console.WriteLine($"{_id}: < {message} (to {targetId})");
            _relay.Send(message, targetId);
        }

        public void Receive(Message message)
        {
            lock (_receiveLock)
            {
                if (_timestamp < message.Timestamp)
                    _timestamp = message.Timestamp;
                _timestamp++;

                // Console.WriteLine($"{_id}: > {message}");
                // Console.WriteLine($"{_id}: Timestamp={_timestamp}");

                switch (message.Type)
                {
                    case MessageType.Request:
                        {
                            // odgovor(j, T(i))
                            var response = new Message(MessageType.Response, _id, message.Timestamp);

                            // var isActive = _isActive;
                            // string status = $"IsAccessRequested={_isAccessRequested},RequestTimestamp={_requestTimestamp},Message.Timestamp={message.Timestamp},Message.PId={message.PId}";
                            if (!_isAccessRequested || _requestTimestamp > message.Timestamp || (_requestTimestamp == message.Timestamp && _id > message.PId))
                            {
                                // Console.WriteLine($"{_id}: Odgovaram ({status}");
                                Send(response, message.PId);
                            }
                            else
                            {
                                // spremi zahtjev (tj. odgovor na zahtjev koji ce se poslati nakon K.O.)
                                // Console.WriteLine($"{_id}: Spremam ({status})");
                                _sendQueue.Enqueue((response, message.PId));
                            }
                        }
                        break;

                    case MessageType.Response:
                        {
                            // TOOD: add checks?
                            _sem.Release();
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown message type: {message.Type}");
                        break;
                }
            }
        }
    }
}
