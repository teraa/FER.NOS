using System.Collections.Generic;
using System.Linq;

namespace NOS.Lab1.Zad1b
{

    class Database : IDatabase
    {
        private List<DbEntry> _entries;

        public Database()
        {
            _entries = new List<DbEntry>();
        }

        public List<DbEntry> GetEntries()
        {
            return _entries.ToList();
        }

        public void SetEntries(List<DbEntry> entries)
        {
            _entries = entries.ToList();
        }
    }
}
