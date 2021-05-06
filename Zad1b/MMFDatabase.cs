using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace NOS.Lab1.Zad1b
{
    class MMFDatabase : IDatabase, IDisposable
    {
        private long _size;
        private MemoryMappedFile _mmf;

        public MMFDatabase(string filePath, long size)
        {
            _size = size;
            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate, null, size);
        }

        public void Dispose()
        {
            ((IDisposable)_mmf).Dispose();
        }

        public List<DbEntry> GetEntries()
        {
            using var stream = _mmf.CreateViewStream();
            using var sr = new StreamReader(stream);

            List<DbEntry> entries = new();
            string? line;
            while ((line = sr.ReadLine()?.TrimEnd('\0')) is { Length: > 0 })
            {
                var entry = DbEntry.Parse(line);
                entries.Add(entry);
            }

            return entries;
        }

        public void SetEntries(List<DbEntry> entries)
        {
            // clear file
            using (var stream = _mmf.CreateViewStream())
                stream.Write(new byte[_size]);

            using (var stream = _mmf.CreateViewStream())
            {
                using var sw = new StreamWriter(stream) { AutoFlush = true };

                foreach (var entry in entries)
                    sw.WriteLine(entry.ToString());
            }
        }
    }
}
