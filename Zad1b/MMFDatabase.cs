using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace NOS.Lab1.Zad1b
{
    class MMFDatabase : IDatabase, IDisposable
    {
        private readonly long _size;
        private readonly MemoryMappedFile _mmf;
        private readonly byte[] _filler;

        public MMFDatabase(string filePath, long size)
        {
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));

            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Size must be greater than zero.");

            _size = size;
            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate, null, size);
            _filler = new byte[size];
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
                stream.Write(_filler);

            using (var stream = _mmf.CreateViewStream())
            {
                using var sw = new StreamWriter(stream) { AutoFlush = true };

                foreach (var entry in entries)
                    sw.WriteLine(entry.ToString());
            }
        }
    }
}
