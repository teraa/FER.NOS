using System.Security.Cryptography;

namespace Zad2
{
    public class SymmetricKey
    {
        public byte[] IV { get; set; } = null!;
        public int KeySize { get; set; }
        public byte[] Key { get; set; } = null!;
        public CipherMode Mode { get; set; }
    }
}
