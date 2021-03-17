using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NOS.Lab1
{
    [Flags]
    public enum Permissions : int
    {
        None = 0,
        OtherWrite = 0b_000_000_010,
        OtherRead = 0b_000_000_100,
        OtherReadWrite = OtherRead | OtherWrite,
        GroupWrite = 0b_000_010_000,
        GroupRead = 0b_000_100_000,
        GroupReadWrite = GroupRead | GroupWrite,
        UserWrite = 0b_010_000_000,
        UserRead = 0b_100_000_000,
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

        [DllImport(DLL_NAME)]
        static extern int msgget(int key, int flags);

        [DllImport(DLL_NAME)]
        static extern int msgsnd(int msqid, ref Message msgp, int msgsz, int msgflg);

        [DllImport(DLL_NAME)]
        static extern int msgrcv(int msqid, ref Message msgp, int msgsz, long msgtyp, int msgflg);

        [DllImport(DLL_NAME, EntryPoint = "my_msgctl")]
        static extern int msgctl(int msqid, int cmd);

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

        public void Send(ref Message message, int flags = 0)
        {
            if (msgsnd(Id, ref message, message.Size, flags) == -1)
                throw new Exception("Failed to send message.");
        }

        public void Receive(ref Message message, long type = 0, int flags = 0)
        {
            int result = msgrcv(Id, ref message, Message.MaxSize, type, flags);

            if (result == -1)
                throw new Exception("Failed to receive message.");
        }

        public void Delete()
        {
            if (msgctl(Id, IPC_RMID) == -1)
                throw new Exception("Failed to delete queue.");
        }
    }
}
