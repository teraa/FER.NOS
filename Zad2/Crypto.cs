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
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(),
            },
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private HashAlgorithmName _hashAlgorithmName;
        private HashAlgorithm _hashAlgorithm = null!;
        private string _symmetricAlgorithmName = null!;
        private SymmetricAlgorithm _symmetricAlgorithm = null!;
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

        public void SetSym(string name)
        {
            _symmetricAlgorithmName = name;
            _symmetricAlgorithm = name switch
            {
                "AES" => Aes.Create(),
                "3DES" => TripleDES.Create(),
                _ => throw new ArgumentOutOfRangeException(nameof(name)),
            };
        }

        public Crypto(string hashAlgorithmName = "SHA256", string symmetricAlgorithmName = "AES")
        {
            SetHash(hashAlgorithmName);
            SetSym(symmetricAlgorithmName);
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
                Data = pub,
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
            byte[] bytes = ExportKey();

            File.WriteAllBytes(keyFile, bytes);
        }
        public byte[] ExportKey()
        {
            var key = new CryptoData
            {
                Description = "Secret key",
                IV = _symmetricAlgorithm.IV,
                KeySizes = new int[] { _symmetricAlgorithm.KeySize },
                SecretKey = _symmetricAlgorithm.Key,
                CipherMode = _symmetricAlgorithm.Mode,
                Methods = new[] { _symmetricAlgorithmName },
            };

            return JsonSerializer.SerializeToUtf8Bytes(key, s_jsonOptions);
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
            byte[] json = File.ReadAllBytes(filePath);
            ImportKey(json);
        }
        public void ImportKey(byte[] keyJson)
        {
            var key = JsonSerializer.Deserialize<CryptoData>(keyJson, s_jsonOptions)!;
            SetSym(key.Methods![0]);

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

            return Encoding.GetBytes(plainText);
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

        // c1 - sym encrypted message
        // c2 - asym encrypted key json
        public (byte[] c1, byte[] c2) Envelope(byte[] plainText, string? filePath = null)
        {
            if (plainText is null) throw new ArgumentNullException(nameof(plainText));

            byte[] c1 = SymEncrypt(plainText);
            byte[] keyJson = ExportKey();
            byte[] c2 = _rsa.Encrypt(keyJson, s_rsaEncryptionPadding);

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

        public bool CheckEnvelope(string envelopeFile, [MaybeNullWhen(false)] out byte[] plainText)
        {
            var json = File.ReadAllText(envelopeFile);
            var data = JsonSerializer.Deserialize<CryptoData>(json, s_jsonOptions)!;

            byte[] c1 = data.EnvelopeData!;
            byte[] c2 = data.EnvelopeCryptKey!;

            return CheckEnvelope(c1, c2, out plainText);
        }

        public bool CheckEnvelope(byte[] c1, byte[] c2, [MaybeNullWhen(false)] out byte[] plainText)
        {
            if (c1 is null) throw new ArgumentNullException(nameof(c1));
            if (c2 is null) throw new ArgumentNullException(nameof(c2));

            try
            {
                byte[] keyJson = _rsa.Decrypt(c2, s_rsaEncryptionPadding);
                ImportKey(keyJson);
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

        public bool CheckSignEnvelope(string signEnvelopeFile, [MaybeNullWhen(false)] out byte[] plainText)
        {
            var json = File.ReadAllText(signEnvelopeFile);
            var data = JsonSerializer.Deserialize<CryptoData>(json, s_jsonOptions)!;

            byte[] c1 = data.EnvelopeData!;
            byte[] c2 = data.EnvelopeCryptKey!;
            byte[] signature = data.Signature!;

            return CheckSignEnvelope(c1, c2, signature, out plainText);
        }

        public bool CheckSignEnvelope(byte[] c1, byte[] c2, byte[] signature, [MaybeNullWhen(false)] out byte[] plainText)
        {
            if (c1 is null) throw new ArgumentNullException(nameof(c1));
            if (c2 is null) throw new ArgumentNullException(nameof(c2));
            if (signature is null) throw new ArgumentNullException(nameof(signature));

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
