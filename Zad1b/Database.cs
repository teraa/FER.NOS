using System.Collections.Generic;
using System.Linq;

namespace NOS.Lab1.Zad1b
{
    class Database
    {
        private List<DbEntry> _entries;

        public Database()
        {
            _entries = new List<DbEntry>();
        }

        public void Update(DbEntry entry)
        {
            var idx = _entries.FindIndex(x => x.PId == entry.PId);
            if (idx >= 0)
                _entries[idx] = entry;
            else
                _entries.Add(entry);
        }

        public IReadOnlyList<DbEntry> GetAll()
        {
            return _entries;
        }
    }
}
