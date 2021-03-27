namespace NOS.Lab1
{
    public static class Consts
    {
        public const int QUEUE_KEY = 5000;
    }

    public enum MessageType : long
    {
        Request = 1 << 5,
        Begin = 1 << 6,
        End = 1 << 7,
    }
}
