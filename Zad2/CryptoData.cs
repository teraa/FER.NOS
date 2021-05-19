using System.Security.Cryptography;

namespace Zad2
{
    public class CryptoData
    {
        public string? Description { get; set; }
        public string? FileName { get; set; }
        public string[]? Methods { get; set; }
        public byte[]? IV { get; set; }
        public int[]? KeySizes { get; set; }
        public byte[]? SecretKey { get; set; }
        public CipherMode? CipherMode { get; set; }
        public byte[]? Modulus { get; set; }
        public byte[]? PubExp { get; set; }
        public byte[]? PrivExp { get; set; }
        public byte[]? Signature { get; set; }
        public string? Data { get; set; }
        public byte[]? EnvelopeData { get; set; }
        public byte[]? EnvelopeCryptKey { get; set; }
    }
}
