namespace NOS.Lab1.Zad1b
{
    class DbEntry
    {
        public DbEntry(int pid, int timestamp, int count)
        {
            Pid = pid;
            Timestamp = timestamp;
            Count = count;
        }

        public int Pid { get; }
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
            => $"Pid={Pid},Timestamp={Timestamp},Count={Count}";
    }
}
