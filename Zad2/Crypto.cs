using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zad2
{
    public class Crypto : IDisposable
    {
        private static readonly RSAEncryptionPadding s_rsaEncryptionPadding = RSAEncryptionPadding.Pkcs1;
        private static readonly RSASignaturePadding s_rsaSignaturePadding = RSASignaturePadding.Pkcs1;
        private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(),
            },
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly SymmetricAlgorithm _symmetricAlgorithm;
        private readonly RSA _rsa;
        private byte[]? _data;

        public Crypto(string hashAlgorithmName, string symmetricAlgorithmName)
        {
            _hashAlgorithmName = hashAlgorithmName switch
            {
                "SHA256" => HashAlgorithmName.SHA256,
                "SHA512" => HashAlgorithmName.SHA512,
                _ => throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName))
            };
            _hashAlgorithm = HashAlgorithm.Create(_hashAlgorithmName.Name!)!;
            _symmetricAlgorithm = symmetricAlgorithmName switch
            {
                "AES" => Aes.Create(),
                "3DES" => TripleDES.Create(),
                _ => throw new ArgumentOutOfRangeException(nameof(symmetricAlgorithmName)),
            };
            _rsa = RSA.Create();
        }

        public string? DataString
        {
            get => _data is null
                ? null
                : s_encoding.GetString(_data);

            set => _data = value is null
                ? null
                : s_encoding.GetBytes(value);
        }


        #region io
        public void ImportPrivateKey(string filePath)
        {
            string content = File.ReadAllText(filePath);
            byte[] bytes = Convert.FromBase64String(content);
            _rsa.ImportRSAPrivateKey(bytes, out _);
        }

        public void ExportPrivateKey(string filePath)
        {
            byte[] privateKey = _rsa.ExportRSAPrivateKey();
            string content = Convert.ToBase64String(privateKey);
            File.WriteAllText(filePath, content);
        }

        public void ImportPublicKey(string filePath)
        {
            string content = File.ReadAllText(filePath);
            byte[] bytes = Convert.FromBase64String(content);
            _rsa.ImportRSAPublicKey(bytes, out _);
        }

        public void ExportPublicKey(string filePath)
        {
            byte[] publicKey = _rsa.ExportRSAPublicKey();
            string content = Convert.ToBase64String(publicKey);
            File.WriteAllText(filePath, content);
        }

        public void ImportKey(string filePath)
        {
            string content = File.ReadAllText(filePath);
            var key = JsonSerializer.Deserialize<CryptoData>(content, s_jsonOptions)!;

            _symmetricAlgorithm.IV = key.IV!;
            _symmetricAlgorithm.KeySize = key.KeySize!.Value;
            _symmetricAlgorithm.Key = key.SecretKey!;
            _symmetricAlgorithm.Mode = key.CipherMode!.Value;
        }

        public void ExportKey(string filePath)
        {
            var key = new CryptoData
            {
                Description = "Secret key",
                IV = _symmetricAlgorithm.IV,
                KeySize = _symmetricAlgorithm.KeySize,
                SecretKey = _symmetricAlgorithm.Key,
                CipherMode = _symmetricAlgorithm.Mode,
            };

            string content = JsonSerializer.Serialize(key, s_jsonOptions);
            File.WriteAllText(filePath, content);
        }

        public void ImportData(string filePath)
        {
            string content = File.ReadAllText(filePath);
            _data = s_encoding.GetBytes(content);
        }
        #endregion

        public byte[] SymEncrypt(byte[] plainText)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            byte[] cipherText;

            using (var encryptor = _symmetricAlgorithm.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new BinaryWriter(cs))
                    sw.Write(plainText);

                cipherText = ms.ToArray();
            }

            return cipherText;
        }

        public byte[] SymDecrypt(byte[] cipherText)
        {
            if (cipherText is null) throw new ArgumentNullException(nameof(cipherText));

            string plainText;

            using (var decryptor = _symmetricAlgorithm.CreateDecryptor())
            using (var ms = new MemoryStream(cipherText))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                plainText = sr.ReadToEnd();
            }

            return s_encoding.GetBytes(plainText);
        }

        public byte[] Sign(byte[] plainText)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            byte[] signature = _rsa.SignData(plainText, _hashAlgorithmName, s_rsaSignaturePadding);
            return signature;
        }

        public bool CheckSign(byte[] signature, byte[] plainText)
        {
            if (signature is null) throw new ArgumentNullException(nameof(signature));
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            return _rsa.VerifyData(plainText, signature, _hashAlgorithmName, s_rsaSignaturePadding);
        }

        public (byte[] c1, byte[] c2) Envelope(byte[] plainText)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            byte[] c1 = SymEncrypt(plainText);
            byte[] c2 = _rsa.Encrypt(_symmetricAlgorithm.Key, s_rsaEncryptionPadding);

            return (c1, c2);
        }

        public bool CheckEnvelope(byte[] c1, byte[] c2, [MaybeNullWhen(false)] out byte[] plainText)
        {
            if (c1 is null) throw new ArgumentNullException(nameof(c1));
            if (c2 is null) throw new ArgumentNullException(nameof(c2));

            try
            {
                byte[] secretKey = _rsa.Decrypt(c2, s_rsaEncryptionPadding);
                _symmetricAlgorithm.Key = secretKey; // Unnecessary if secret key is already set.
                plainText = SymDecrypt(c1);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking envelope: {ex}");
                plainText = null;
                return false;
            }
        }

        public (byte[] c1, byte[] c2, byte[] signature) SignEnvelope(byte[] plainText)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            var (c1, c2) = Envelope(plainText);

            byte[] signatureSource = new byte[c1.Length + c2.Length];
            c1.CopyTo(signatureSource, 0);
            c2.CopyTo(signatureSource, c1.Length);

            byte[] signature = _rsa.SignData(signatureSource, _hashAlgorithmName, s_rsaSignaturePadding);

            return (c1, c2, signature);
        }

        public bool CheckSignEnvelope(byte[] c1, byte[] c2, byte[] signature, [MaybeNullWhen(false)] out byte[] plainText)
        {
            if (c1 is null) throw new ArgumentNullException(nameof(c1));
            if (c2 is null) throw new ArgumentNullException(nameof(c2));

            byte[] signatureSource = new byte[c1.Length + c2.Length];
            c1.CopyTo(signatureSource, 0);
            c2.CopyTo(signatureSource, c1.Length);

            if (!CheckSign(signature, signatureSource))
            {
                Console.WriteLine("Signature check failed");
                plainText = null;
                return false;
            }

            return CheckEnvelope(c1, c2, out plainText);
        }

        public void Dispose()
        {
            _hashAlgorithm?.Dispose();
            _symmetricAlgorithm?.Dispose();
            _rsa?.Dispose();
        }
    }
}
