using System;

namespace NOS.Lab1
{
    class DbEntry
    {
        public int ProcessId { get; set; }
        public int Timestamp { get; set; }
        public int Count { get; set; }

        public static DbEntry Parse(string input)
        {
            var parts = input.Split(',');
            if (parts.Length != 3)
                throw new ArgumentException();

            var pid = int.Parse(parts[0]);
            var ts = int.Parse(parts[1]);
            var count = int.Parse(parts[2]);

            return new DbEntry
            {
                ProcessId = pid,
                Timestamp = ts,
                Count = count,
            };
        }

        public override string ToString()
            => $"{ProcessId},{Timestamp},{Count}";
    }
}
