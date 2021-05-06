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
            var parts = input.Split(',');
            var type = Enum.Parse<MessageType>(parts[0].Split('=')[1]);
            var pid = int.Parse(parts[1].Split('=')[1]);
            var ts = int.Parse(parts[2].Split('=')[1]);

            return new Message(type, pid, ts);
        }

        public override string ToString()
            => $"Type={Type},Pid={Pid},Timestamp={Timestamp}";
    }

    enum MessageType : int
    {
        Request,
        Response,
    }
}
