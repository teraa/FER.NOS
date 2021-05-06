using System;

namespace NOS.Lab1.Zad1b
{
    class Message
    {
        public Message(MessageType type, int pid, int timestamp)
        {
            Type = type;
            Pid = pid;
            Timestamp = timestamp;
        }

        public MessageType Type { get; }
        public int Pid { get; }
        public int Timestamp { get; }

        public static Message Parse(string input)
        {
            try
            {
                var idx = input.IndexOf('(');
                var prefix = input[..idx];
                var type = prefix switch
                {
                    "zahtjev" => MessageType.Request,
                    "odgovor" => MessageType.Response,
                    _ => Enum.Parse<MessageType>(prefix),
                };

                var args = input[(idx + 1)..(input.Length - 1)];
                var parts = args.Split(',');
                var pid = int.Parse(parts[0]);
                var ts = int.Parse(parts[1]);

                return new Message(type, pid, ts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"input={input}\n{ex}");
                throw;
            }

        }

        public override string ToString()
        {
            var prefix = Type switch
            {
                MessageType.Request => "zahtjev",
                MessageType.Response => "odgovor",
                _ => Type.ToString(),
            };

            return $"{prefix}({Pid},{Timestamp})";
        }
    }

    enum MessageType : int
    {
        Request,
        Response,
        End,
    }
}
