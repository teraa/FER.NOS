using System;
using System.Runtime.InteropServices;

namespace MessageQueues
{
    public class MessageQueue
    {
        private const string DLL_NAME = "./msg.so";

        private readonly int _key;
        private readonly int _id;

        private MessageQueue(int key, int id)
        {
            _key = key;
            _id = id;
        }

        [DllImport(DLL_NAME)] static extern int msgget(int key, int flags);
        
        public static MessageQueue? GetOrCreate(int key)
        {
            int msqid = msgget(key, 0); // TODO

            return msqid is 0
                ? null
                : new MessageQueue(key, msqid);
        }
    }
}
