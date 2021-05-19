using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Security.Cryptography;

namespace Zad2
{
    class Program
    {
        static void SignCommandHandler(
            string inputFile,
            string privateKeyFile,
            string hashAlgorithm,
            string outputFile)
        {

            using var crypto = new Crypto(hashAlgorithmName: hashAlgorithm);
            crypto.ImportPrivateKey(privateKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var signature = crypto.Sign(plainText, outputFile);

            Console.WriteLine("Signature: " + Convert.ToBase64String(signature));
        }

        static void EnvelopeCommandHandler(
            string inputFile,
            string symmetricAlgorithm,
            string keyFile,
            string publicKeyFile,
            string outputFile)
        {
            using var crypto = new Crypto(symmetricAlgorithmName: symmetricAlgorithm);
            crypto.ImportKey(keyFile);
            crypto.ImportPublicKey(publicKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var (c1, c2) = crypto.Envelope(plainText, outputFile);

            Console.WriteLine("Envelope");
            Console.WriteLine($"c1: {Convert.ToBase64String(c1)}");
            Console.WriteLine($"c2: {Convert.ToBase64String(c2)}");
        }

        static void SignEnvelopeCommandHandler(
            string inputFile,
            string privateKeyFile,
            string hashAlgorithm,
            string symmetricAlgorithm,
            string keyFile,
            string outputFile)
        {
            using var crypto = new Crypto(hashAlgorithm, symmetricAlgorithm);
            crypto.ImportPrivateKey(privateKeyFile);
            crypto.ImportKey(keyFile);

            var plainText = File.ReadAllBytes(inputFile);

            var (c1, c2, signature) = crypto.SignEnvelope(plainText, outputFile);

            Console.WriteLine("Signature: " + Convert.ToBase64String(signature));
            Console.WriteLine($"c1: {Convert.ToBase64String(c1)}");
            Console.WriteLine($"c2: {Convert.ToBase64String(c2)}");
        }

        static void GenRsaCommandHandler(
            int keySize,
            string publicKeyFile,
            string privateKeyFile
        )
        {
            using var crypto = new Crypto();

            crypto.GenerateKeyPair(keySize, publicKeyFile, privateKeyFile);
        }

        static void GenKeyCommandHandler(
            string symmetricAlgorithm,
            int keySize,
            CipherMode cipherMode,
            string keyFile
        )
        {
            using var crypto = new Crypto(symmetricAlgorithmName: symmetricAlgorithm);

            crypto.GenerateKey(keySize, cipherMode, keyFile);
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
                    aliases: new[] { "-s", "--priv", "--private-key-file" },
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
                    aliases: new[] { "-k", "--key", "--key-file" },
                    getDefaultValue: () => "data/key.json",
                    description: "Secret key file for symmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "-p", "--pub", "--public-key-file" },
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
                    aliases: new[] { "-s", "--priv", "--private-key-file" },
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
                    aliases: new[] { "-k", "--key", "--key-file" },
                    getDefaultValue: () => "data/key.json",
                    description: "Secret key file for symmetric encryption"
                ),
                new Option<string>(
                    aliases: new[] { "-o", "--output-file" },
                    getDefaultValue: () => "data/sign_envelope.json",
                    description: "Output file for the envelope and signature data"
                ),
            };

            var genRsaCommand = new Command(name: "gen-rsa")
            {
                new Option<int>(
                    aliases: new[] { "--size", "--key-size" },
                    getDefaultValue: () => 2048,
                    description: "RSA key size"
                ),
                new Option<string>(
                    aliases: new[] { "-p", "--pub", "--public-key-file" },
                    getDefaultValue: () => "data/rsa.pub",
                    description: "Output file for the RSA public key."
                ),
                new Option<string>(
                    aliases: new[] { "-s", "--priv", "--private-key-file" },
                    getDefaultValue: () => "data/rsa",
                    description: "Output file for the RSA private key."
                ),
            };
            var genCommand = new Command(name: "gen")
            {
                new Option<string>(
                    aliases: new[] { "--sym", "--symmetric-algorithm" },
                    getDefaultValue: () => "AES",
                    description: "Symmetric algorithm to use, allowed values: AES, 3DES"
                ),
                new Option<int>(
                    aliases: new[] { "--size", "--key-size" },
                    getDefaultValue: () => 256,
                    description: "Key size"
                ),
                new Option<CipherMode>(
                    aliases: new[] { "-m", "--mode", "--cipher-mode" },
                    getDefaultValue: () => CipherMode.CBC,
                    description: "Cipher mode"
                ),
                new Option<string>(
                    aliases: new[] { "-k", "--key", "--key-file" },
                    getDefaultValue: () => "data/key.json",
                    description: "Output file for the secret key"
                ),
            };

            rootCommand.AddCommand(signCommand);
            rootCommand.AddCommand(envelopeCommand);
            rootCommand.AddCommand(signEnvelopeCommand);

            rootCommand.AddCommand(genRsaCommand);
            rootCommand.AddCommand(genCommand);

            signCommand.Handler = CommandHandler.Create<string, string, string, string>(SignCommandHandler);
            envelopeCommand.Handler = CommandHandler.Create<string, string, string, string, string>(EnvelopeCommandHandler);
            signEnvelopeCommand.Handler = CommandHandler.Create<string, string, string, string, string, string>(SignEnvelopeCommandHandler);

            genRsaCommand.Handler = CommandHandler.Create<int, string, string>(GenRsaCommandHandler);
            genCommand.Handler = CommandHandler.Create<string, int, CipherMode, string>(GenKeyCommandHandler);

            rootCommand.Invoke(args);

            // using var crypto = new Crypto("SHA256", "AES");

            // crypto.ImportPrivateKey("data/rsa");
            // crypto.ImportKey("data/key.json");

            // byte[] plainText = s_encoding.GetBytes("Tera");

            // Console.WriteLine();
            // var encrypted = crypto.SymEncrypt(plainText);
            // Console.WriteLine("Encrypted: " + Convert.ToBase64String(encrypted));


            // Console.WriteLine();
            // var decrypted = crypto.SymDecrypt(encrypted);
            // Console.WriteLine("Decrypted: " + s_encoding.GetString(decrypted));


            // Console.WriteLine();
            // var signature = crypto.Sign(plainText);
            // Console.WriteLine("Signature: " + Convert.ToBase64String(signature));


            // Console.WriteLine();
            // var signatureSuccess = crypto.CheckSign(signature, plainText);
            // Console.WriteLine($"SignatureSucess: {signatureSuccess}");


            // Console.WriteLine();
            // var envelope = crypto.Envelope(plainText);
            // Console.WriteLine("Envelope");
            // Console.WriteLine("c1: " + Convert.ToBase64String(envelope.c1));
            // Console.WriteLine("c2: " + Convert.ToBase64String(envelope.c2));


            // Console.WriteLine();
            // var envelopeSuccess = crypto.CheckEnvelope(envelope.c1, envelope.c2, out byte[]? envelopePlainText);
            // Console.WriteLine($"EnvelopeCheck: {envelopeSuccess}");
            // Console.WriteLine(s_encoding.GetString(envelopePlainText!));

            // Console.WriteLine();
            // var signEnvelope = crypto.SignEnvelope(plainText);
            // Console.WriteLine("SignEnvelope");
            // Console.WriteLine("c1: " + Convert.ToBase64String(signEnvelope.c1));
            // Console.WriteLine("c2: " + Convert.ToBase64String(signEnvelope.c2));
            // Console.WriteLine("Signature: " + Convert.ToBase64String(signEnvelope.signature));

            // Console.WriteLine();
            // var signEnvelopeSuccess = crypto.CheckSignEnvelope(signEnvelope.c1, signEnvelope.c2, signEnvelope.signature, out byte[]? signEnvelopePlainText);
            // Console.WriteLine($"SignEnvelopeCheck: {signEnvelopeSuccess}");
            // Console.WriteLine(s_encoding.GetString(signEnvelopePlainText!));

            // crypto.ImportKey("data/key.json");
            // crypto.ImportPrivateKey("data/rsa");
            // crypto.ImportPublicKey("data/rsa.pub");

            // crypto.ExportKey("data/key.json");
            // crypto.ExportPrivateKey("data/rsa");
            // crypto.ExportPublicKey("data/rsa.pub");
        }
    }
}
