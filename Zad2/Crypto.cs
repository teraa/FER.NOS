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

        private HashAlgorithmName _hashAlgorithmName;
        private HashAlgorithm _hashAlgorithm;
        private readonly string _symmetricAlgorithmName;
        private readonly SymmetricAlgorithm _symmetricAlgorithm;
        private readonly RSA _rsa;

        public void SetHash(string name)
        {
            _hashAlgorithmName = name switch
            {
                "SHA256" => HashAlgorithmName.SHA256,
                "SHA512" => HashAlgorithmName.SHA512,
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };

            _hashAlgorithm = HashAlgorithm.Create(_hashAlgorithmName.Name!)!;
        }

        public Crypto(string hashAlgorithmName = "SHA256", string symmetricAlgorithmName = "AES")
        {
            _hashAlgorithm = null!; // Suppress warning
            SetHash(hashAlgorithmName);
            _symmetricAlgorithmName = symmetricAlgorithmName;
            _symmetricAlgorithm = _symmetricAlgorithmName switch
            {
                "AES" => Aes.Create(),
                "3DES" => TripleDES.Create(),
                _ => throw new ArgumentOutOfRangeException(nameof(symmetricAlgorithmName)),
            };
            _rsa = RSA.Create();
        }

        #region io
        public void GenerateKeyPair(int keySize, string publicKeyFile, string privateKeyFile)
        {
            _rsa.KeySize = keySize;

            byte[] pubBytes = _rsa.ExportRSAPublicKey();
            byte[] privBytes = _rsa.ExportRSAPrivateKey();

            string pub = Convert.ToBase64String(pubBytes);
            string priv = Convert.ToBase64String(privBytes);

            var param = _rsa.ExportParameters(true);

            var data = new CryptoData
            {
                Description = "Public key",
                Methods = new[] { "RSA" },
                KeySizes = new[] { _rsa.KeySize },
                Modulus = param.Modulus,
                PubExp = param.Exponent,
                Data = pub, // TODO: remove
            };

            var json = JsonSerializer.Serialize(data, s_jsonOptions);

            File.WriteAllText(publicKeyFile, json);

            data.Description = "Private key";
            data.PubExp = null;
            data.PrivExp = param.D;
            data.Data = priv;

            json = JsonSerializer.Serialize(data, s_jsonOptions);

            File.WriteAllText(privateKeyFile, json);
        }

        public void GenerateKey(int keySize, CipherMode cipherMode, string keyFile)
        {
            _symmetricAlgorithm.Mode = cipherMode;
            _symmetricAlgorithm.KeySize = keySize;

            var key = new CryptoData
            {
                Description = "Secret key",
                IV = _symmetricAlgorithm.IV,
                KeySizes = new int[] { _symmetricAlgorithm.KeySize },
                SecretKey = _symmetricAlgorithm.Key,
                CipherMode = _symmetricAlgorithm.Mode,
            };

            string json = JsonSerializer.Serialize(key, s_jsonOptions);
            File.WriteAllText(keyFile, json);
        }

        public void ImportPrivateKey(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<CryptoData>(json, s_jsonOptions)!;
            byte[] bytes = Convert.FromBase64String(data.Data!);
            _rsa.ImportRSAPrivateKey(bytes, out _);
        }

        public void ImportPublicKey(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<CryptoData>(json, s_jsonOptions)!;
            byte[] bytes = Convert.FromBase64String(data.Data!);
            _rsa.ImportRSAPublicKey(bytes, out _);
        }

        public void ImportKey(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var key = JsonSerializer.Deserialize<CryptoData>(json, s_jsonOptions)!;

            _symmetricAlgorithm.IV = key.IV!;
            _symmetricAlgorithm.KeySize = key.KeySizes![0];
            _symmetricAlgorithm.Key = key.SecretKey!;
            _symmetricAlgorithm.Mode = key.CipherMode!.Value;
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

        public byte[] Sign(byte[] plainText, string? filePath = null)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            byte[] signature = _rsa.SignData(plainText, _hashAlgorithmName, s_rsaSignaturePadding);

            if (filePath is not null)
            {
                var data = new CryptoData
                {
                    Description = "Signature",
                    Methods = new string[] { _hashAlgorithmName.Name!, "RSA" },
                    KeySizes = new int[] { _rsa.KeySize },
                    Signature = signature,
                };

                var json = JsonSerializer.Serialize(data, s_jsonOptions);
                File.WriteAllText(filePath, json);
            }

            return signature;
        }

        public bool CheckSign(string signatureFile, byte[] plainText)
        {
            var json = File.ReadAllText(signatureFile);
            var data = JsonSerializer.Deserialize<CryptoData>(json, s_jsonOptions)!;

            SetHash(data.Methods![0]);

            return CheckSign(data.Signature!, plainText);
        }

        public bool CheckSign(byte[] signature, byte[] plainText)
        {
            if (signature is null) throw new ArgumentNullException(nameof(signature));
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            return _rsa.VerifyData(plainText, signature, _hashAlgorithmName, s_rsaSignaturePadding);
        }

        public (byte[] c1, byte[] c2) Envelope(byte[] plainText, string? filePath = null)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            byte[] c1 = SymEncrypt(plainText);
            byte[] c2 = _rsa.Encrypt(_symmetricAlgorithm.Key, s_rsaEncryptionPadding);

            if (filePath is not null)
            {
                var data = new CryptoData
                {
                    Description = "Envelope",
                    Methods = new string[] { _symmetricAlgorithmName, "RSA" },
                    KeySizes = new int[] { _symmetricAlgorithm.KeySize, _rsa.KeySize },
                    EnvelopeData = c1,
                    EnvelopeCryptKey = c2,
                };

                var json = JsonSerializer.Serialize(data, s_jsonOptions);
                File.WriteAllText(filePath, json);
            }

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

        public (byte[] c1, byte[] c2, byte[] signature) SignEnvelope(byte[] plainText, string? filePath = null)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            var (c1, c2) = Envelope(plainText);

            // concatenate
            byte[] signatureSource = new byte[c1.Length + c2.Length];
            c1.CopyTo(signatureSource, 0);
            c2.CopyTo(signatureSource, c1.Length);

            byte[] signature = _rsa.SignData(signatureSource, _hashAlgorithmName, s_rsaSignaturePadding);

            if (filePath is not null)
            {
                var data = new CryptoData
                {
                    Description = "Envelope Signature",
                    Methods = new string[] { _symmetricAlgorithmName, "RSA", _hashAlgorithmName.Name! },
                    KeySizes = new int[] { _symmetricAlgorithm.KeySize, _rsa.KeySize },
                    EnvelopeData = c1,
                    EnvelopeCryptKey = c2,
                    Signature = signature,
                };

                var json = JsonSerializer.Serialize(data, s_jsonOptions);
                File.WriteAllText(filePath, json);
            }

            return (c1, c2, signature);
        }

        public bool CheckSignEnvelope(byte[] c1, byte[] c2, byte[] signature, [MaybeNullWhen(false)] out byte[] plainText)
        {
            if (c1 is null) throw new ArgumentNullException(nameof(c1));
            if (c2 is null) throw new ArgumentNullException(nameof(c2));

            // concatenate
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
