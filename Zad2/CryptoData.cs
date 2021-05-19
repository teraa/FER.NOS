using System;
using System.Security.Cryptography;
using System.Text;

namespace Zad2
{
    public class CryptoData
    {
        public string? Description { get; set; }
        public string? FileName { get; set; }
        public string[]? Methods { get; set; }
        public byte[]? IV { get; set; }
        public int? KeySize { get; set; }
        public byte[]? SecretKey { get; set; }
        public CipherMode? CipherMode { get; set; }
        // public string? Modulus { get; set; }
        // public string? PubExp { get; set; }
        // public string? PrivExp { get; set; }
        public string? Signature { get; set; }
        public string? Data { get; set; }
        public string? EnvelopeData { get; set; }
        public string? EnvelopeCryptKey { get; set; }
    }
}
