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

    public enum MessageType : long
    {
        Any = 0,
        Request = 1,
        Begin = 2,
        End = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MyMessage
    {
        internal  const int SIZE = sizeof(int) * 2;

        private MessageType _type;
        private int _carId;
        private int _direction;

        public MyMessage() { }
        public MyMessage(MessageType type, int carId, int direction)
        {
            Type = type;
            CarId = carId;
            Direction = direction;
        }

        public MessageType Type
        {
            get => _type;
            set => _type = value;
        }

        public int CarId
        {
            get => _carId;
            set => _carId = value;
        }

        public int Direction
        {
            get => _direction;
            set
            {
                if (value is not (0 or 1))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be 0 or 1");

                _direction = value;
            }
        }

        public override string ToString()
            => $"Type={Type}, CarId={CarId}, Direction={Direction}";
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
        private static extern int msgget(int key, int flags);

        [DllImport(DLL_NAME)]
        private static extern int msgsnd(int msqid, MyMessage msgp, int msgsz, int msgflg);

        [DllImport(DLL_NAME)]
        private static extern int msgrcv(int msqid, MyMessage msgp, int msgsz, MessageType msgtyp, int msgflg);

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

        public void Send(MyMessage message, int flags = 0)
        {
            if (msgsnd(Id, message, MyMessage.SIZE, flags) == -1)
                throw new Exception("Failed to send message.");
        }

        public void Receive(ref MyMessage message, MessageType type = MessageType.Any, int flags = 0)
        {
            int result = msgrcv(Id, message, MyMessage.SIZE, type, flags);

            if (result == -1)
                throw new Exception("Failed to receive message.");
        }

        public bool TryReceive(ref MyMessage message, MessageType type = MessageType.Any, int flags = 0)
        {
            int result = msgrcv(Id, message, MyMessage.SIZE, type, flags);

            return result != -1;
        }

        public void Delete()
        {
            if (msgctl(Id, IPC_RMID) == -1)
                throw new Exception("Failed to delete queue.");
        }
    }
}
