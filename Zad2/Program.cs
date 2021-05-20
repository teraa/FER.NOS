using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Security.Cryptography;

namespace Zad2
{
    class Program
    {
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
                    getDefaultValue: () => "data/rsa.json",
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
                    getDefaultValue: () => "data/rsa_pub.json",
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
                    getDefaultValue: () => "data/rsa.json",
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
                    getDefaultValue: () => "data/rsa_pub.json",
                    description: "Output file for the RSA public key."
                ),
                new Option<string>(
                    aliases: new[] { "-s", "--priv", "--private-key-file" },
                    getDefaultValue: () => "data/rsa.json",
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

            var checkSignCommand = new Command(name: "checksign")
            {
                new Option<string>(
                    aliases: new[] { "-i", "--input-file" },
                    getDefaultValue: () => "data/in.txt",
                    description: "Input data file"
                ),
                new Option<string>(
                    aliases: new[] { "--signature", "--signature-file" },
                    getDefaultValue: () => "data/sign.json",
                    description: "Signature file"
                ),
                new Option<string>(
                    aliases: new[] { "-p", "--pub", "--public-key-file" },
                    getDefaultValue: () => "data/rsa_pub.json",
                    description: "RSA public key."
                ),
            };

            var checkEnvelopeCommand = new Command(name: "checkenvelope")
            {
                new Option<string>(
                    aliases: new[] { "--envelope", "--envelope-file" },
                    getDefaultValue: () => "data/envelope.json",
                    description: "Envelope file"
                ),
                new Option<string>(
                    aliases: new[] { "-s", "--priv", "--private-key-file" },
                    getDefaultValue: () => "data/rsa.json",
                    description: "RSA private key."
                ),
                new Option<string>(
                    aliases: new[] { "--sym", "--symmetric-algorithm" },
                    getDefaultValue: () => "AES",
                    description: "Symmetric algorithm to use, allowed values: AES, 3DES"
                ),
            };

            var checkSignEnvelopeCommand = new Command(name: "checksignenvelope")
            {
                new Option<string>(
                    aliases: new[] { "-i", "--input-file" },
                    getDefaultValue: () => "data/in.txt",
                    description: "Input data file"
                ),
                new Option<string>(
                    aliases: new[] { "--sign-envelope-file" },
                    getDefaultValue: () => "data/sign_envelope.json",
                    description: "Sign envelope file"
                ),
                new Option<string>(
                    aliases: new[] { "-s", "--priv", "--private-key-file" },
                    getDefaultValue: () => "data/rsa.json",
                    description: "RSA private key."
                ),
                new Option<string>(
                    aliases: new[] { "--sym", "--symmetric-algorithm" },
                    getDefaultValue: () => "AES",
                    description: "Symmetric algorithm to use, allowed values: AES, 3DES"
                ),
            };

            rootCommand.AddCommand(signCommand);
            rootCommand.AddCommand(envelopeCommand);
            rootCommand.AddCommand(signEnvelopeCommand);
            rootCommand.AddCommand(genRsaCommand);
            rootCommand.AddCommand(genCommand);
            rootCommand.AddCommand(checkSignCommand);
            rootCommand.AddCommand(checkEnvelopeCommand);
            rootCommand.AddCommand(checkSignEnvelopeCommand);

            signCommand.Handler = CommandHandler.Create<string, string, string, string>(SignCommandHandler);
            envelopeCommand.Handler = CommandHandler.Create<string, string, string, string, string>(EnvelopeCommandHandler);
            signEnvelopeCommand.Handler = CommandHandler.Create<string, string, string, string, string, string>(SignEnvelopeCommandHandler);
            genRsaCommand.Handler = CommandHandler.Create<int, string, string>(GenRsaCommandHandler);
            genCommand.Handler = CommandHandler.Create<string, int, CipherMode, string>(GenKeyCommandHandler);
            checkSignCommand.Handler = CommandHandler.Create<string, string, string>(CheckSignCommandHandler);
            checkEnvelopeCommand.Handler = CommandHandler.Create<string, string, string>(CheckEnvelopeCommandHandler);
            checkSignEnvelopeCommand.Handler = CommandHandler.Create<string, string, string, string>(CheckSignEnvelopeCommandHandler);

            rootCommand.Invoke(args);
        }

        static void SignCommandHandler(
            string inputFile,
            string privateKeyFile,
            string hashAlgorithm,
            string outputFile)
        {
            Console.WriteLine($"inputFile: {inputFile}\nprivateKeyFile: {privateKeyFile}\nhashAlgorithm: {hashAlgorithm}\noutputFile: {outputFile}\n");

            using var crypto = new Crypto(hashAlgorithmName: hashAlgorithm);
            crypto.ImportPrivateKey(privateKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var signature = crypto.Sign(plainText, outputFile);

            Console.WriteLine("Signature: " + Convert.ToBase64String(signature));
        }

        static void CheckSignCommandHandler(
            string inputFile,
            string signatureFile,
            string publicKeyFile)
        {
            Console.WriteLine($"inputFile: {inputFile}\nsignatureFile: {signatureFile}\npublicKeyFile: {publicKeyFile}\n---");

            using var crypto = new Crypto();
            crypto.ImportPublicKey(publicKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var success = crypto.CheckSign(signatureFile, plainText);

            Console.WriteLine(success);
        }

        static void EnvelopeCommandHandler(
            string inputFile,
            string symmetricAlgorithm,
            string keyFile,
            string publicKeyFile,
            string outputFile)
        {
            Console.WriteLine($"inputFile: {inputFile}\nsymmetricAlgorithm: {symmetricAlgorithm}\nkeyFile: {keyFile}\npublicKeyFile: {publicKeyFile}\noutputFile: {outputFile}\n---");

            using var crypto = new Crypto(symmetricAlgorithmName: symmetricAlgorithm);
            crypto.ImportKey(keyFile);
            crypto.ImportPublicKey(publicKeyFile);

            var plainText = File.ReadAllBytes(inputFile);
            var (c1, c2) = crypto.Envelope(plainText, outputFile);

            Console.WriteLine("Envelope");
            Console.WriteLine($"c1: {Convert.ToBase64String(c1)}");
            Console.WriteLine($"c2: {Convert.ToBase64String(c2)}");
        }

        static void CheckEnvelopeCommandHandler(
            string envelopeFile,
            string privateKeyFile,
            string symmetricAlgorithm)
        {
            Console.WriteLine($"envelopeFile: {envelopeFile}\nprivateKeyFile: {privateKeyFile}\n---");

            using var crypto = new Crypto(symmetricAlgorithmName: symmetricAlgorithm);
            crypto.ImportPrivateKey(privateKeyFile);

            var success = crypto.CheckEnvelope(envelopeFile, out byte[]? plainTextBytes);

            Console.WriteLine(success);

            if (success)
            {
                string plainText = Crypto.Encoding.GetString(plainTextBytes!);
                Console.WriteLine($"Data: {plainText}");
            }
        }

        static void CheckSignEnvelopeCommandHandler(
            string inputFile,
            string signEnvelopeFile,
            string privateKeyFile,
            string symmetricAlgorithm)
        {
            Console.WriteLine($"inputFile: {inputFile}\nsignEnvelopeFile: {signEnvelopeFile}\nprivateKeyFile: {privateKeyFile}\n---");

            using var crypto = new Crypto(symmetricAlgorithmName: symmetricAlgorithm);
            crypto.ImportPrivateKey(privateKeyFile);

            var success = crypto.CheckSignEnvelope(signEnvelopeFile, out byte[]? plainTextBytes);

            Console.WriteLine(success);

            if (success)
            {
                string plainText = Crypto.Encoding.GetString(plainTextBytes!);
                Console.WriteLine($"Data: {plainText}");
            }
        }

        static void SignEnvelopeCommandHandler(
            string inputFile,
            string privateKeyFile,
            string hashAlgorithm,
            string symmetricAlgorithm,
            string keyFile,
            string outputFile)
        {
            Console.WriteLine($"inputFile: {inputFile}\nprivateKeyFile: {privateKeyFile}\nhashAlgorithm: {hashAlgorithm}\nsymmetricAlgorithm: {symmetricAlgorithm}\nkeyFile: {keyFile}\noutputFile: {outputFile}\n---");

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
            string privateKeyFile)
        {
            Console.WriteLine($"keySize: {keySize}\npublicKeyFile: {publicKeyFile}\nprivateKeyFile: {privateKeyFile}\n---");

            using var crypto = new Crypto();

            crypto.GenerateKeyPair(keySize, publicKeyFile, privateKeyFile);
        }

        static void GenKeyCommandHandler(
            string symmetricAlgorithm,
            int keySize,
            CipherMode cipherMode,
            string keyFile)
        {
            Console.WriteLine($"symmetricAlgorithm: {symmetricAlgorithm}\nkeySize: {keySize}\ncipherMode: {cipherMode}\nkeyFile: {keyFile}\n---");

            using var crypto = new Crypto(symmetricAlgorithmName: symmetricAlgorithm);

            crypto.GenerateKey(keySize, cipherMode, keyFile);
        }
    }
}
