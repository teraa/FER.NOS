using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NOS.Lab1
{
    class Program
    {
        const string DLL_NAME = "../shared/msg.so";
        const int IPC_CREAT = 0x200;
        const int IPC_EXCL = 0x400;
        const int IPC_NOWAIT = 0x800;
        const int IPC_RMID = 0;
        const int IPC_SET = 1;
        const int IPC_STAT = 2;

        [DllImport(DLL_NAME)] static extern void print(string message);
        [DllImport(DLL_NAME)] static extern void test_struct(ref Message value);
        [DllImport(DLL_NAME)] static extern void test_chars(string text);

        [DllImport(DLL_NAME)] static extern int msgget(int key, int flags);
        [DllImport(DLL_NAME)] static extern int msgsnd(int msqid, ref Message msgp, int msgsz, int msgflg);
        [DllImport(DLL_NAME)] static extern int msgrcv(int msqid, ref Message msgp, int msgsz, long msgtyp, int msgflg);
        [DllImport(DLL_NAME, EntryPoint = "my_msgctl")] static extern int msgctl(int msqid, int cmd);

        static int msgsnd(int msqid, ref Message msgp, int msgflg)
            => msgsnd(msqid, ref msgp, Encoding.Unicode.GetByteCount(msgp.Text) + 1, msgflg);

        static void Main(string[] args)
        {
            test_chars("test_chars");
            var msg = new Message(1337, "test_struct");
            test_struct(ref msg);

            var result = Spock();
            Console.WriteLine($"Exit result: {result}");
        }

        static int Kirk()
        {
            var message = new Message(1L, "Kirk: We are attacked. Spock, send reinforcement.");
            int msqid;
            int key = 12345;

            if ((msqid = msgget(key, 0b110_000_000 | IPC_CREAT)) == -1)
            {
                Console.WriteLine("msgget: error");
                return 1;
            }

            if (msgsnd(msqid, ref message, 0) == -1)
            {
                Console.WriteLine("msgsnd: error");
                return 1;
            }

            Console.WriteLine("Sent");

            return 0;
        }

        static int Spock()
        {
            Message message = default;
            int msqid;
            int key = 12345;

            if ((msqid = msgget(key, 0b110_000_000 | IPC_CREAT)) == -1)
            {
                Console.WriteLine("msgget: error");
                return 1;
            }

            for (;;)
            {
                if (msgrcv(msqid, ref message, 200, 0, 0) == -1) // TODO: 200
                {
                    Console.WriteLine("msgrcv: error");
                    return 1;
                }

                Console.WriteLine($"> {message.Text}");
            }

            // clean, handle sigint

            // if (msgctl(msqid, IPC_RMID) == -1)
            // {
            //     Console.WriteLine("msgctl: error");
            //     return 1;
            // }

            // return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        internal const int TextSize = 200;

        long _type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = TextSize)] string _text;

        public Message(long type, string text)
        {
            _type = type;
            _text = text;
        }

        public long Type => _type;
        public string Text => _text;
    }
}
