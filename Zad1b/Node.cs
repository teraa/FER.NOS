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
        private int _id;
        private int _peers;
        private int _runCount;
        private IDatabase _db;
        private int _timestamp;
        private int _count;
        private Message? _request;
        private Queue<(Message message, int targetId)> _sendQueue;
        private SemaphoreSlim _responseSem;
        private SemaphoreSlim _endSem;
        private Random _rnd;
        private object _receiveLock;
        private NamedPipeServerStream[] _servers;
        private NamedPipeClientStream[] _clients;
        private StreamWriter[] _sws;

        public Node(int id, int peers, int runCount, IDatabase db)
        {
            _id = id;
            _peers = peers;
            _runCount = runCount;
            _db = db;

            _timestamp = 1;
            _count = 0;
            _request = null;
            _sendQueue = new();
            _responseSem = new SemaphoreSlim(0, _peers);
            _endSem = new SemaphoreSlim(0, _peers);
            _rnd = new Random();
            _receiveLock = new object();
            _servers = new NamedPipeServerStream[_peers + 1];
            _clients = new NamedPipeClientStream[_peers + 1];
            _sws = new StreamWriter[_peers + 1];
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

            await Task.WhenAll(tasks);

            for (int i = 0; i <= _peers; i++)
            {
                if (i == _id) continue;

                _sws[i] = new StreamWriter(_clients[i]) { AutoFlush = true };
            }

            for (int i = 0; i <= _peers; i++)
            {
                if (i == _id) continue;

                _ = ListenAsync(_servers[i]);
            }
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

            // pričekaj kraj svih čvorova
            for (int i = 0; i < _peers; i++)
            {
                await _endSem.WaitAsync();
            }

            // cleanup
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

            // _isAccessRequested = false;
            _request = null;

            // Posalji odgovor svim procesima koji cekaju na odgovor
            while (_sendQueue.TryDequeue(out var item))
                Send(item.message, item.targetId);
        }

        private async Task RunCriticalAsync()
        {
            Write("{");

            _count++;

            var myEntry = new DbEntry(_id, _timestamp, _count);
            var entries = _db.GetEntries();
            var idx = entries.FindIndex(x => x.Pid == _id);
            if (idx == -1)
                entries.Add(myEntry);
            else
                entries[idx] = myEntry;

            _db.SetEntries(entries);

            Write("  Ispis baze");
            foreach (var entry in entries)
                Write($"  {entry}");

            await Task.Delay(_rnd.Next(100, 2000));

            Write("}");
        }

        private void Broadcast(Message message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            for (int i = 0; i <= _peers; i++)
                if (i != _id)
                    Send(message, i);
        }

        private void Send(Message message, int targetId)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            Console.WriteLine($"{_id} > {targetId}: {message}");
            var raw = message.ToString();

            try
            {
                var sw = _sws[targetId];
                sw.WriteLine(raw);
            }
            catch (Exception ex)
            {
                Write($"Error: {ex}");
                Write($"TargetId={targetId}");
                for (int i = 0; i < _sws.Length; i++)
                {
                    Write($"_sws[{i}] = {(_sws[i] is null ? "null" : "not null")}");
                }
            }
        }

        public void Receive(Message message)
        {
            lock (_receiveLock)
            {
                if (message is null)
                    throw new ArgumentNullException(nameof(message));

                if (_timestamp < message.Timestamp)
                    _timestamp = message.Timestamp;
                _timestamp++;

                Console.WriteLine($"{_id} < {message.Pid}: {message}");

                switch (message.Type)
                {
                    case MessageType.Request:
                        {
                            // odgovor(j, T(i))
                            var response = new Message(MessageType.Response, _id, message.Timestamp);

                            if (_request is null || _request.Timestamp > message.Timestamp || (_request.Timestamp == message.Timestamp && _id > message.Pid))
                            {
                                Send(response, message.Pid);
                            }
                            else
                            {
                                _sendQueue.Enqueue((response, message.Pid));
                            }
                        }
                        break;

                    case MessageType.Response:
                        {
                            _responseSem.Release();
                        }
                        break;

                    case MessageType.End:
                        {
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
