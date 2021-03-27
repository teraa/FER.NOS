using System;
using System.Runtime.InteropServices;

namespace NOS.Lab1
{
    public enum MessageType : long
    {
        Any = 0,
        Request = 1,
        Begin = 2,
        End = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Message
    {
        internal  const int SIZE = sizeof(int) * 2;

        private MessageType _type;
        private int _carId;
        private int _direction;

        public Message() { }
        public Message(MessageType type, int carId, int direction)
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
}
