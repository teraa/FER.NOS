using System;

namespace NOS.Lab1
{
    class Message
    {
        public MessageType Type { get; set; }
        public int ProcessId { get; set; }
        public int Timestamp { get; set; }

        public static Message Parse(string input)
        {
            var parts = input.Split(',');
            if (parts.Length != 3)
                throw new ArgumentException("parts.Length != 3");

            var type = Enum.Parse<MessageType>(parts[0]);
            var pid = int.Parse(parts[1]);
            var ts = int.Parse(parts[2]);

            return new Message
            {
                Type = type,
                ProcessId = pid,
                Timestamp = ts,
            };
        }

        public override string ToString()
            => $"{Type},{ProcessId},{Timestamp}";
    }

    enum MessageType : int
    {
        Request,
        Response,
    }
}
