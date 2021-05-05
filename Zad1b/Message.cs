namespace NOS.Lab1.Zad1b
{
    class Message
    {
        public Message(MessageType type, int pId, int timestamp)
        {
            Type = type;
            PId = pId;
            Timestamp = timestamp;
        }

        public MessageType Type { get; }
        public int PId { get; }
        public int Timestamp { get; }

        public override string ToString()
            => $"Type={Type},PId={PId},Timestamp={Timestamp}";
    }

    enum MessageType : int
    {
        Request,
        Response,
    }
}
