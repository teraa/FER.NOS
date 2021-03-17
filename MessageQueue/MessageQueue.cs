using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NOS.Lab1
{
    [Flags]
    public enum Permissions : int
    {
        UserRead = 0b_100_000_000,
        UserWrite = 0b_010_000_000,
        UserReadWrite = UserRead | UserWrite,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public const int MaxSize = 200;

        long _type;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxSize)]
        string _text;

        public Message(long type, string text)
        {
            if (text.Length > MaxSize) throw new ArgumentOutOfRangeException(nameof(text));

            _type = type;
            _text = text;
        }

        public long Type => _type;
        public string Text => _text;
        public int Size => Encoding.Unicode.GetByteCount(Text) + 1;
    }

    public class MessageQueue
    {
        const string DLL_NAME = "../shared/msg.so";
        const int IPC_CREAT = 0x200;
        const int IPC_EXCL = 0x400;
        const int IPC_NOWAIT = 0x800;
        const int IPC_RMID = 0;
        const int IPC_SET = 1;
        const int IPC_STAT = 2;

        public int Id { get; private init; }
        public int Key { get; private init; }

        [DllImport(DLL_NAME)] static extern int msgget(int key, int flags);
        [DllImport(DLL_NAME)] static extern int msgsnd(int msqid, ref Message msgp, int msgsz, int msgflg);
        [DllImport(DLL_NAME)] static extern int msgrcv(int msqid, ref Message msgp, int msgsz, long msgtyp, int msgflg);
        [DllImport(DLL_NAME)] static extern int my_msgctl(int msqid, int cmd);

        public static MessageQueue GetOrCreate(int key, Permissions permissions)
        {
            int id = msgget(key, (int)permissions | IPC_CREAT);

            if (id == -1)
                throw new Exception("Failed to get or create message queue.");

            return new MessageQueue
            {
                Id = id,
                Key = key,
            };
        }

        public void Send(ref Message message)
        {
            int result = msgsnd(Id, ref message, message.Size, 0);

            if (result == -1)
                throw new Exception("Failed to send message.");
        }

        public void Receive(ref Message message, long type, int flags)
        {
            int result = msgrcv(Id, ref message, Message.MaxSize, type, flags);

            if (result == -1)
                throw new Exception("Failed to receive message.");
        }

        public void Delete()
        {
            if (my_msgctl(Id, IPC_RMID) == -1)
                throw new Exception("Failed to delete queue.");
        }
    }
}
