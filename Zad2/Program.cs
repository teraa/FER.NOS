using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

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

            using var program = new Crypto(hashAlgorithm, "AES"); // TODO
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
            using var program = new Crypto("SHA256", symmetricAlgorithm); // TODO
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
            using var program = new Crypto(hashAlgorithm, symmetricAlgorithm);
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
                    getDefaultValue: () => "data/skey.json",
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
    }
}
