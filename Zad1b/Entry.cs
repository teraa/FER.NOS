namespace NOS.Lab1.Zad1b
{
    class DbEntry
    {
        public DbEntry(int pId, int timestamp, int count)
        {
            PId = pId;
            Timestamp = timestamp;
            Count = count;
        }

        public int PId { get; }
        public int Timestamp { get; }
        public int Count { get; }

        public override string ToString()
            => $"PId={PId},Timestamp={Timestamp},Count={Count}";
    }
}
