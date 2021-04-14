using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NOS.Lab1
{
    class Proces
    {
        private const int ENTRIES = 1;
        private const string MM_FILE = "data.db";
        private const long MM_FILE_SIZE = 1024;

        private static int s_ts = 0;
        private static int s_pid;
        private static int s_n;
        private static bool s_waiting;
        private static SemaphoreSlim s_sem = null!;
        private static SemaphoreSlim s_clientSem = null!;
        private static SemaphoreSlim s_serverSem = null!;
        private static BlockingCollection<Message>[] s_sendQueues = null!;
        private static ConcurrentQueue<int> s_waitingResponses = new();
        private static Random s_rnd = new();
        private static MemoryMappedFile s_mmf = null!;

        static async Task Main(string[] args)
        {
            #region args
            if (args.Length != 2
                || !int.TryParse(args[0], out s_n)
                || !int.TryParse(args[1], out s_pid))
            {
                Usage();
                return;
            }

            if (s_n < 2)
            {
                Console.WriteLine("Error: N must be greater than 1");
                Usage();
                return;
            }

            if (s_pid < 0)
            {
                Console.WriteLine("Error: ID must be a positive number.");
                Usage();
                return;
            }

            if (s_pid >= s_n)
            {
                Console.WriteLine("Error: ID must be less than N");
                Usage();
                return;
            }

            static void Usage() => Console.WriteLine("Usage: <N> <ID>");
            #endregion

            s_mmf = MemoryMappedFile.CreateFromFile(MM_FILE, FileMode.OpenOrCreate, null, MM_FILE_SIZE);


            s_sem = new SemaphoreSlim(0, s_n - 1);
            s_clientSem = new SemaphoreSlim(0, s_n - 1);
            s_serverSem = new SemaphoreSlim(0, s_n - 1);

            s_sendQueues = new BlockingCollection<Message>[s_n];
            var tasks = new List<Task>(2 * (s_n - 1));

            for (int i = 0; i < s_n; i++)
            {
                if (i == s_pid) continue;
                tasks.Add(RunServerAsync(i, s_n - 1));
                tasks.Add(RunClientAsync(i));
                s_sendQueues[i] = new BlockingCollection<Message>();
            }

            for (int i = 0; i < s_n - 1; i++)
            {
                await s_serverSem.WaitAsync();
                await s_clientSem.WaitAsync();
            }

            Console.WriteLine("All connected.\n");

            for (int i = 0; i < ENTRIES; i++)
            {

                // Korak 1.
                Broadcast(new Message { Type = MessageType.Request, ProcessId = s_pid, Timestamp = s_ts });

                // Korak 3.
                // wait
                s_waiting = true;
                for (int j = 0; j < s_n - 1; j++)
                {
                    await s_sem.WaitAsync();
                    Console.WriteLine($"{j + 1} / {s_n - 1}");
                }

                Console.WriteLine("Ulaz");
                await Task.Delay(5000); // TMP
                // await DoWorkAsync();
                Console.WriteLine("Izlaz");

                s_waiting = false;

                // Korak 4.
                var response = new Message { Type = MessageType.Response, ProcessId = s_pid, Timestamp = s_ts };
                while (s_waitingResponses.TryDequeue(out var targetId))
                {
                    Send(response, targetId);
                }
            }

            for (int i = 0; i < s_n; i++)
            {
                if (i == s_pid) continue;
                s_sendQueues[i].CompleteAdding();
            }

            Console.WriteLine("CompleteAdding");

            await Task.WhenAll(tasks);
        }

        static async Task RunServerAsync(int internalId, int instances)
        {
            // Console.WriteLine($"Starting server ({internalId+1}/{instances})");
            using var pipeServer = new NamedPipeServerStream($"pipe_{s_pid}", PipeDirection.InOut, instances);
            await pipeServer.WaitForConnectionAsync();
            s_serverSem.Release();
            Console.WriteLine($"Server[{internalId}]: Client connected.");

            // read
            using var sr = new StreamReader(pipeServer);
            string? line;
            while ((line = await sr.ReadLineAsync()) is not null)
            {
                var message = Message.Parse(line);
                // Console.WriteLine($"Server[{internalId}] > {message}");
                // Process message

                bool messageIsEarlier = s_ts > message.Timestamp || (s_ts == message.Timestamp && s_pid > message.ProcessId);

                if (s_ts < message.Timestamp)
                    s_ts = message.Timestamp;

                s_ts++;


                switch (message.Type)
                {
                    case MessageType.Request:
                        // Korak 2.
                        Console.WriteLine($"waiting={s_waiting}, messageIsEarlier={messageIsEarlier}");
                        if (s_waiting && !messageIsEarlier)
                        {
                            Console.WriteLine("Spremam zahtjev.");
                            s_waitingResponses.Enqueue(message.ProcessId);
                        }
                        else
                        {
                            Console.WriteLine($"Šaljem odgovor.");
                            var response = new Message { Type = MessageType.Response, ProcessId = s_pid, Timestamp = s_ts };
                            Send(response, message.ProcessId);
                        }
                        break;

                    case MessageType.Response:
                        // Korak 3.
                        Console.WriteLine("Release");
                        s_sem.Release();
                    break;

                    default:
                        Console.WriteLine($"Unknown message type: {message.Type}");
                    break;
                }
            }

            Console.WriteLine($"Server[{internalId}] Done.");
        }

        static async Task RunClientAsync(int targetId)
        {
            using var pipeClient = new NamedPipeClientStream(".", $"pipe_{targetId}", PipeDirection.InOut);
            await pipeClient.ConnectAsync();
            s_clientSem.Release();
            Console.WriteLine($"Client: Connected to server {targetId}");

            using var sw = new StreamWriter(pipeClient)
            {
                AutoFlush = true,
            };

            foreach (var msg in s_sendQueues[targetId].GetConsumingEnumerable())
            {
                var str = msg.ToString();
                Console.WriteLine($"{targetId} < {str}");
                await sw.WriteLineAsync(str);
            }

            Console.WriteLine($"Client: Disconnected from server {targetId}");
        }

        #region working
        static void Broadcast(Message message)
        {
            for (int i = 0; i < s_sendQueues.Length; i++)
            {
                if (i == s_pid) continue;

                s_sendQueues[i].Add(message);
            }
        }

        static void Send(Message message, int targetId)
        {
            if (targetId < 0 || targetId >= s_sendQueues.Length || targetId == s_pid)
                throw new ArgumentOutOfRangeException(nameof(targetId), targetId, "Target ID must be less than N and not equal to ID");

            s_sendQueues[targetId].Add(message);
        }

        static async Task DoWorkAsync()
        {
            var entries = new List<DbEntry>();

            // pročitaj bazu
            using (var stream = s_mmf.CreateViewStream())
            using (var sr = new StreamReader(stream))
            {
                string? line;
                while ((line = await sr.ReadLineAsync()) is not null)
                {
                    line = line.TrimEnd('\0');
                    if (line.Length == 0) break;

                    var entry = DbEntry.Parse(line);
                    entries.Add(entry);
                }
            }

            // ispiši sadržaj baze
            Console.WriteLine("DB:");
            foreach (var entry in entries)
                Console.WriteLine($"\t{entry}");

            // ažuriraj svoj red
            var myEntry = entries.FirstOrDefault(x => x.ProcessId == s_pid);
            if (myEntry is null)
            {
                myEntry = new DbEntry
                {
                    ProcessId = s_pid,
                    Timestamp = s_ts,
                    Count = 1,
                };

                entries.Add(myEntry);
            }
            else
            {
                myEntry.Timestamp = s_ts;
                myEntry.Count++;
            }

            // clear file
            using (var stream = s_mmf.CreateViewStream())
            {
                await stream.WriteAsync(new byte[MM_FILE_SIZE]);
            }

            using (var stream = s_mmf.CreateViewStream())
            using (var sw = new StreamWriter(stream) { AutoFlush = true })
            {
                foreach (var entry in entries)
                {
                    await sw.WriteLineAsync(entry.ToString());
                }
            }

            await Task.Delay(s_rnd.Next(100, 2000));
        }
        #endregion
    }
}
