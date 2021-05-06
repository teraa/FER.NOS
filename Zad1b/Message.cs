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

        public override string ToString()
            => $"Type={Type},Pid={Pid},Timestamp={Timestamp}";
    }

    enum MessageType : int
    {
        Request,
        Response,
    }
}
