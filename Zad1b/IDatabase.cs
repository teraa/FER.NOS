using System.Collections.Generic;

namespace NOS.Lab1.Zad1b
{
    interface IDatabase
    {
        List<DbEntry> GetEntries();
        void SetEntries(List<DbEntry> entries);
    }
}
