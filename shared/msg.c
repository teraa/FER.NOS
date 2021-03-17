#include <stdlib.h>
#include <sys/ipc.h>
#include <sys/msg.h>

int my_msgctl(int msqid, int cmd)
{
    msgctl(msqid, cmd, NULL);
}
