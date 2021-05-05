namespace NOS.Lab1.Zad1b
{
    class Relay
    {
        private Node[] _nodes;

        public Relay(Node[] nodes)
        {
            _nodes = nodes;
        }

        public void Send(Message message, int targetId)
        {
            _nodes[targetId].Receive(message);
        }
    }
}
