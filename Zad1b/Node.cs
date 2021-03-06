using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace NOS.Lab1.Zad1b
{
    class Node
    {
        private readonly int _id;
        private readonly int _peers;
        private readonly int _runCount;
        private readonly IDatabase _db;
        private readonly Queue<(Message message, int targetId)> _sendQueue;
        private readonly SemaphoreSlim _responseSem;
        private readonly SemaphoreSlim _endSem;
        private readonly Random _rnd;
        private readonly object _receiveLock;
        private readonly NamedPipeServerStream[] _servers;
        private readonly NamedPipeClientStream[] _clients;
        private readonly StreamWriter[] _sws;
        private int _timestamp;
        private int _count;
        private Message? _request;

        public Node(int id, int peers, int runCount, IDatabase db)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "ID must be greater or equal to zero.");

            if (peers <= 0)
                throw new ArgumentOutOfRangeException(nameof(peers), peers, "Peer count must be greater than zero.");

            if (id > peers)
                throw new ArgumentOutOfRangeException(nameof(id), id, "ID cannot be greater than number of peers.");

            if (runCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(runCount), runCount, "Run count must be greater than zero.");

            _id = id;
            _peers = peers;
            _runCount = runCount;
            _db = db;

            _sendQueue = new();
            _responseSem = new SemaphoreSlim(0, _peers);
            _endSem = new SemaphoreSlim(0, _peers);
            _rnd = new Random();
            _receiveLock = new object();
            _servers = new NamedPipeServerStream[_peers + 1];
            _clients = new NamedPipeClientStream[_peers + 1];
            _sws = new StreamWriter[_peers + 1];

            _timestamp = 1;
            _count = 0;
            _request = null;
        }

        private void Write(string value)
        {
            Console.WriteLine($"{_id}: {value}");
        }

        private async Task InitializeAsync()
        {
            var tasks = new List<Task>();

            for (int i = 0; i <= _peers; i++)
            {
                if (i == _id) continue;

                _servers[i] = new NamedPipeServerStream($"{_id}", PipeDirection.In, _peers);
                _clients[i] = new NamedPipeClientStream(".", $"{i}", PipeDirection.Out);

                tasks.Add(_servers[i].WaitForConnectionAsync());
                tasks.Add(_clients[i].ConnectAsync());
            }

            // Pri??ekaj da se pove??u svi cjevovodi
            await Task.WhenAll(tasks);

            // Otvori pisa??a za svakog klijenta prije ??itanja servera
            for (int i = 0; i <= _peers; i++)
                if (i != _id)
                    _sws[i] = new StreamWriter(_clients[i]) { AutoFlush = true };

            // Paralelno ??itaj svaki server
            for (int i = 0; i <= _peers; i++)
                if (i != _id)
                    _ = ListenAsync(_servers[i]);
        }

        private async Task ListenAsync(NamedPipeServerStream server)
        {
            try
            {
                using var sr = new StreamReader(server);

                string? line;
                while ((line = await sr.ReadLineAsync()) is not null)
                {
                    var message = Message.Parse(line);
                    Receive(message);
                }
            }
            catch (Exception ex)
            {
                Write(ex.ToString());
            }

            server.Dispose();
        }

        public async Task StartAsync()
        {
            await InitializeAsync();

            for (int i = 0; i < _runCount; i++)
            {
                await Task.Delay(_rnd.Next(100, 2000));
                await RunAsync();
            }

            await WaitForEndAsync();
        }

        private async Task WaitForEndAsync()
        {
            var endMessage = new Message(MessageType.End, _id, _timestamp);
            Broadcast(endMessage);

            // Pri??ekaj kraj svih ??vorova
            for (int i = 0; i < _peers; i++)
                await _endSem.WaitAsync();

            // Po??isti
            for (int i = 0; i <= _peers; i++)
            {
                if (i == _id) continue;

                _sws[i].Dispose();
                _clients[i].Dispose();
                _servers[i].Dispose();
            }
        }

        public async Task RunAsync()
        {
            // Posalji zahtjev svim procesima
            _request = new Message(MessageType.Request, _id, _timestamp);
            Broadcast(_request);

            // Pricekaj odgovore svih procesa
            for (int i = 0; i < _peers; i++)
                await _responseSem.WaitAsync();

            // K.O. START
            await RunCriticalAsync();
            // K.O. KRAJ

            _request = null;

            // Posalji odgovor svim procesima koji cekaju na odgovor
            while (_sendQueue.TryDequeue(out var item))
                Send(item.message, item.targetId);
        }

        private async Task RunCriticalAsync()
        {
            Write("{");

            _count++;

            // A??uriraj svoje vrijednosti
            var myEntry = new DbEntry(_id, _timestamp, _count);
            var entries = _db.GetEntries();
            var idx = entries.FindIndex(x => x.Pid == _id);
            if (idx == -1)
                entries.Add(myEntry);
            else
                entries[idx] = myEntry;

            _db.SetEntries(entries);

            // Ispi??i sadr??aj baze
            Write("  Ispis baze");
            foreach (var entry in entries)
                Write($"  {entry}");

            // Spavaj
            await Task.Delay(_rnd.Next(100, 2000));

            Write("}");
        }

        private void Broadcast(Message message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            var raw = message.ToString();

            for (int i = 0; i <= _peers; i++)
                if (i != _id)
                    SendRaw(raw, i);
        }

        private void Send(Message message, int targetId)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            Console.WriteLine($"{_id} > {targetId}: {message}");
            var raw = message.ToString();
            SendRaw(raw, targetId);
        }

        private void SendRaw(string message, int targetId)
        {
            try
            {
                _sws[targetId].WriteLine(message);
            }
            catch (Exception ex)
            {
                Write($"Error: {ex}");
            }
        }

        public void Receive(Message message)
        {
            lock (_receiveLock)
            {
                if (message is null)
                    throw new ArgumentNullException(nameof(message));

                // A??uriraj svoj sat
                if (_timestamp < message.Timestamp)
                    _timestamp = message.Timestamp;
                _timestamp++;

                Console.WriteLine($"{_id} < {message.Pid}: {message}");

                switch (message.Type)
                {
                    case MessageType.Request:
                        {
                            // Generiraj odgovor(j, T(i))
                            var response = new Message(MessageType.Response, _id, message.Timestamp);

                            if (_request is null || _request.Timestamp > message.Timestamp || (_request.Timestamp == message.Timestamp && _id > message.Pid))
                            {
                                // Po??alji
                                Send(response, message.Pid);
                            }
                            else
                            {
                                // Spremi
                                _sendQueue.Enqueue((response, message.Pid));
                            }
                        }
                        break;

                    case MessageType.Response:
                        {
                            // Signaliziraj primitak odgovora
                            _responseSem.Release();
                        }
                        break;

                    case MessageType.End:
                        {
                            // Signaliziraj kraj ??vora
                            _endSem.Release();
                        }
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
