using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NOS.Lab1
{
    [Flags]
    public enum Permissions : int
    {
        None = 0,
        OtherWrite = 1 << 1,
        OtherRead = 1 << 2,
        OtherReadWrite = OtherRead | OtherWrite,
        GroupWrite = 1 << 4,
        GroupRead = 1 << 5,
        GroupReadWrite = GroupRead | GroupWrite,
        UserWrite = 1 << 7,
        UserRead = 1 << 8,
        UserReadWrite = UserRead | UserWrite,
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

    public class MessageQueue
    {
        const string DLL_NAME = "../shared/msg.so";
        public const int IPC_CREAT = 0x200;
        public const int IPC_EXCL = 0x400;
        public const int IPC_NOWAIT = 0x800;
        public const int IPC_RMID = 0;
        public const int IPC_SET = 1;
        public const int IPC_STAT = 2;

        private MessageQueue() { }

        public int Id { get; private init; }
        public int Key { get; private init; }

        [DllImport(DLL_NAME)]
        static extern int msgget(int key, int flags);

        [DllImport(DLL_NAME)]
        static extern int msgsnd(int msqid, ref TextMessage msgp, int msgsz, int msgflg);

        [DllImport(DLL_NAME)]
        static extern int msgrcv(int msqid, ref TextMessage msgp, int msgsz, long msgtyp, int msgflg);

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

        public void Send(ref TextMessage message, int flags = 0)
        {
            if (msgsnd(Id, ref message, message.Size, flags) == -1)
                throw new Exception("Failed to send message.");
        }

        public void Receive(ref TextMessage message, long type = 0, int flags = 0)
        {
            int result = msgrcv(Id, ref message, TextMessage.MaxSize, type, flags);

            if (result == -1)
                throw new Exception("Failed to receive message.");
        }

        public bool TryReceive(ref TextMessage message, long type = 0, int flags = 0)
        {
            int result = msgrcv(Id, ref message, TextMessage.MaxSize, type, flags);

            return result != -1;
        }

        public void Delete()
        {
            if (msgctl(Id, IPC_RMID) == -1)
                throw new Exception("Failed to delete queue.");
        }
    }
}
