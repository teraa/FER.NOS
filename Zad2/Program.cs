using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zad2
{
    class Program : IDisposable
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

        public Program(string hashAlgorithmName, string symmetricAlgorithmName)
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

        static void SignCommandHandler(
            string inputFile,
            string privateKeyFile,
            string hashAlgorithm,
            string outputFile)
        {

            using var program = new Program(hashAlgorithm, "AES"); // TODO
            program.ImportPrivateKey(privateKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var signature = program.Sign(plainText);

            Console.WriteLine("Signature: " + Convert.ToBase64String(signature));
            // TODO: write to file
        }

        static void EnvelopeCommandHandler(
            string inputFile,
            string symmetricAlgorithm,
            string keyFile,
            string publicKeyFile,
            string outputFile)
        {
            using var program = new Program("SHA256", symmetricAlgorithm); // TODO
            program.ImportKey(keyFile);
            program.ImportPublicKey(publicKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var (c1, c2) = program.Envelope(plainText);

            Console.WriteLine("Envelope");
            Console.WriteLine($"c1: {Convert.ToBase64String(c1)}");
            Console.WriteLine($"c2: {Convert.ToBase64String(c2)}");
            // TODO: write to file
        }

        static void SignEnvelopeCommandHandler(
            string inputFile,
            string privateKeyFile,
            string hashAlgorithm,
            string symmetricAlgorithm,
            string keyFile,
            string outputFile)
        {
            using var program = new Program(hashAlgorithm, symmetricAlgorithm);
            program.ImportPrivateKey(privateKeyFile);
            program.ImportKey(keyFile);

            var plainText = File.ReadAllBytes(inputFile);

            var (c1, c2, signature) = program.SignEnvelope(plainText);

            Console.WriteLine("Signature: " + Convert.ToBase64String(signature));
            Console.WriteLine($"c1: {Convert.ToBase64String(c1)}");
            Console.WriteLine($"c2: {Convert.ToBase64String(c2)}");
            // TODO: write to file
        }

        static void Main(string[] args)
        {
            var rootCommand = new RootCommand();
            var signCommand = new Command(name: "sign")
            {
                new Option<string>(
                    aliases: new[] { "-i", "--input-file" },
                    getDefaultValue: () => "data/in.txt",
                    description: "Input data file"
                ),
                new Option<string>(
                    aliases: new[] { "--priv", "--private-key-file" },
                    getDefaultValue: () => "data/rsa",
                    description: "Private key file for asymmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "-h", "--hash", "--hash-algorithm" },
                    getDefaultValue: () => "SHA256",
                    description: "Hashing algorithm to use, allowed values: SHA256, SHA512."
                ),
                new Option<string>(
                    aliases: new[] { "-o", "--output-file" },
                    getDefaultValue: () => "data/sign.json",
                    description: "Output file for the signature"
                ),
            };
            var envelopeCommand = new Command(name: "envelope")
            {
                new Option<string>(
                    aliases: new[] { "-i", "--input-file" },
                    getDefaultValue: () => "data/in.txt",
                    description: "Input data file"
                ),
                new Option<string>(
                    aliases: new[] { "--sym", "--symmetric-algorithm" },
                    getDefaultValue: () => "AES",
                    description: "Symmetric algorithm to use, allowed values: AES, 3DES"
                ),
                new Option<string>(
                    aliases: new[] { "--key", "--key-file" },
                    getDefaultValue: () => "data/skey.json",
                    description: "Secret key file for symmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "--pub", "--public-key-file" },
                    getDefaultValue: () => "data/rsa.pub",
                    description: "Public key file for asymmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "-o", "--output-file" },
                    getDefaultValue: () => "data/envelope.json",
                    description: "Output file for the envelope data"
                ),
            };
            var signEnvelopeCommand = new Command(name: "signenvelope")
            {
                new Option<string>(
                    aliases: new[] { "-i", "--input-file" },
                    getDefaultValue: () => "data/in.txt",
                    description: "Input data file"
                ),
                new Option<string>(
                    aliases: new[] { "--priv", "--private-key-file" },
                    getDefaultValue: () => "data/rsa",
                    description: "Private key file for asymmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "-h", "--hash", "--hash-algorithm" },
                    getDefaultValue: () => "SHA256",
                    description: "Hashing algorithm to use, allowed values: SHA256, SHA512."
                ),
                new Option<string>(
                    aliases: new[] { "--sym", "--symmetric-algorithm" },
                    getDefaultValue: () => "AES",
                    description: "Symmetric algorithm to use, allowed values: AES, 3DES"
                ),
                new Option<string>(
                    aliases: new[] { "--key", "--key-file" },
                    getDefaultValue: () => "data/skey.json",
                    description: "Secret key file for symmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "-o", "--output-file" },
                    getDefaultValue: () => "data/signed_envelope.json",
                    description: "Output file for the signed envelope data"
                ),
            };

            rootCommand.AddCommand(signCommand);
            rootCommand.AddCommand(envelopeCommand);
            rootCommand.AddCommand(signEnvelopeCommand);

            signCommand.Handler = CommandHandler.Create<string, string, string, string>(SignCommandHandler);
            envelopeCommand.Handler = CommandHandler.Create<string, string, string, string, string>(EnvelopeCommandHandler);
            signEnvelopeCommand.Handler = CommandHandler.Create<string, string, string, string, string, string>(SignEnvelopeCommandHandler);

            rootCommand.Invoke(args);

            // using var program = new Program(HashAlgorithmName.SHA256, SymmetricAlgorithmName.AES);

            // program.ImportPrivateKey("data/rsa");
            // program.ImportKey("data/skey.json");

            // byte[] plainText = s_encoding.GetBytes("Tera");

            // Console.WriteLine();
            // var encrypted = program.SymEncrypt(plainText);
            // Console.WriteLine("Encrypted: " + Convert.ToBase64String(encrypted));


            // Console.WriteLine();
            // var decrypted = program.SymDecrypt(encrypted);
            // Console.WriteLine("Decrypted: " + s_encoding.GetString(decrypted));


            // Console.WriteLine();
            // var signature = program.Sign(plainText);
            // Console.WriteLine("Signature: " + Convert.ToBase64String(signature));


            // Console.WriteLine();
            // var signatureSuccess = program.CheckSign(signature, plainText);
            // Console.WriteLine($"SignatureSucess: {signatureSuccess}");


            // Console.WriteLine();
            // var envelope = program.Envelope(plainText);
            // Console.WriteLine("Envelope");
            // Console.WriteLine("c1: " + Convert.ToBase64String(envelope.c1));
            // Console.WriteLine("c2: " + Convert.ToBase64String(envelope.c2));


            // Console.WriteLine();
            // var envelopeSuccess = program.CheckEnvelope(envelope.c1, envelope.c2, out byte[]? envelopePlainText);
            // Console.WriteLine($"EnvelopeCheck: {envelopeSuccess}");
            // Console.WriteLine(s_encoding.GetString(envelopePlainText!));

            // Console.WriteLine();
            // var signEnvelope = program.SignEnvelope(plainText);
            // Console.WriteLine("SignEnvelope");
            // Console.WriteLine("c1: " + Convert.ToBase64String(signEnvelope.c1));
            // Console.WriteLine("c2: " + Convert.ToBase64String(signEnvelope.c2));
            // Console.WriteLine("Signature: " + Convert.ToBase64String(signEnvelope.signature));

            // Console.WriteLine();
            // var signEnvelopeSuccess = program.CheckSignEnvelope(signEnvelope.c1, signEnvelope.c2, signEnvelope.signature, out byte[]? signEnvelopePlainText);
            // Console.WriteLine($"SignEnvelopeCheck: {signEnvelopeSuccess}");
            // Console.WriteLine(s_encoding.GetString(signEnvelopePlainText!));

            // program.ImportKey("data/skey.json");
            // program.ImportPrivateKey("data/rsa");
            // program.ImportPublicKey("data/rsa.pub");

            // program.ExportKey("data/skey.json");
            // program.ExportPrivateKey("data/rsa");
            // program.ExportPublicKey("data/rsa.pub");
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
