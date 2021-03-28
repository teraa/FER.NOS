using System;
using System.Runtime.InteropServices;

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

    public class MessageQueue
    {
        private const string DLL_NAME = "../shared/msg.so";
        private const int IPC_CREAT = 0x200;
        private const int IPC_EXCL = 0x400;
        private const int IPC_NOWAIT = 0x800;
        private const int IPC_RMID = 0;
        private const int IPC_SET = 1;
        private const int IPC_STAT = 2;

        private MessageQueue() { }

        public int Id { get; private init; }
        public int Key { get; private init; }

        [DllImport(DLL_NAME)]
        private static extern int msgget(int key, int flags);

        [DllImport(DLL_NAME)]
        private static extern int msgsnd(int msqid, Message msgp, int msgsz, int msgflg);

        [DllImport(DLL_NAME)]
        private static extern int msgrcv(int msqid, Message msgp, int msgsz, MessageType msgtyp, int msgflg);

        [DllImport(DLL_NAME, EntryPoint = "my_msgctl")]
        private static extern int msgctl(int msqid, int cmd);

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

        public void Send(Message message, int flags = 0)
        {
            if (msgsnd(Id, message, Message.SIZE, flags) == -1)
                throw new Exception("Failed to send message.");
        }

        public void Receive(ref Message message, MessageType type = MessageType.Any, int flags = 0)
        {
            int result = msgrcv(Id, message, Message.SIZE, type, flags);

            if (result == -1)
                throw new Exception("Failed to receive message.");
        }

        public bool TryReceive(ref Message message, MessageType type = MessageType.Any, int flags = 0)
        {
            int result = msgrcv(Id, message, Message.SIZE, type, flags);

            return result != -1;
        }

        public void Delete()
        {
            if (msgctl(Id, IPC_RMID) == -1)
                throw new Exception("Failed to delete message queue.");
        }

        public bool TryDelete()
        {
            return msgctl(Id, IPC_RMID) != -1;
        }
    }
}
