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

        public static DbEntry Parse(string input)
        {
            var parts = input.Split(',');
            int pid = int.Parse(parts[0].Split('=')[1]);
            int ts = int.Parse(parts[1].Split('=')[1]);
            int count = int.Parse(parts[2].Split('=')[1]);

            return new DbEntry(pid, ts, count);
        }

        public override string ToString()
            => $"PId={PId},Timestamp={Timestamp},Count={Count}";
    }
}
