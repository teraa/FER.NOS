using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NOS.Lab1
{
    class Program
    {
        const string DLL_NAME = "../shared/rnd.so";
        [DllImport(DLL_NAME)] static extern void print(string message);
        [DllImport(DLL_NAME)] static extern void test_text_struct(ref TextMessage value);
        [DllImport(DLL_NAME)] static extern void test_my_struct(Message value);
        [DllImport(DLL_NAME)] static extern void test_chars(string text);

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            print("test");

            var textMessage = new TextMessage(10, "abcdef");
            test_text_struct(ref textMessage);
            var myMessage = new Message(MessageType.End, 1337, 1);
            test_my_struct(myMessage);

            // Spock();
            // Kirk();
        }

        static void Kirk()
        {
            var message = new TextMessage(1L, "Kirk: We are attacked. Spock, send reinforcement.");
            int key = 12345;
            var queue = MessageQueue.GetOrCreate(key, Permissions.UserReadWrite);
            // queue.Send(ref message);
            Console.WriteLine("Sent");
        }

        static void Spock()
        {
            TextMessage message = default;
            int key = 12345;

            var queue = MessageQueue.GetOrCreate(key, Permissions.UserReadWrite);
            Console.CancelKeyPress += (_, _) => queue.Delete();

            try
            {
                while(true)
                {
                    // queue.Receive(ref message);
                    Console.WriteLine($"> {message.Text}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextMessage
    {
        public const int MaxSize = 200;

        long _type;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxSize)]
        string _text;

        public TextMessage(long type, string text)
        {
            if (type < 1)
                throw new ArgumentOutOfRangeException(nameof(type));
            if (text.Length > MaxSize)
                throw new ArgumentOutOfRangeException(nameof(text));

            _type = type;
            _text = text;
        }

        public long Type => _type;
        public string Text => _text;
        public int Size => Encoding.Unicode.GetByteCount(Text) + 1;

        public override string ToString() => $"{Type}: {Text}";
    }
}
