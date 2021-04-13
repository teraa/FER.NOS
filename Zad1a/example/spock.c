/*
** spock.c -- reads from a message queue
*/
#define _XOPEN_SOURCE 500
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/ipc.h>
#include <sys/msg.h>
#include <signal.h>

struct my_msgbuf {
    long mtype;
    char mtext[200];
};

int msqid;

void retreat(int failure)
{
    if (msgctl(msqid, IPC_RMID, NULL) == -1) {
        perror("msgctl");
        exit(1);
    }
    exit(0);
}

int main(void)
{
    struct my_msgbuf buf;
    key_t key;

    key = 12345;

    if ((msqid = msgget(key, 0600 | IPC_CREAT)) == -1) { /* connect to the queue */
        perror("msgget");
        exit(1);
    }

    sigset(SIGINT, retreat);

    for(;;) { /* Spock never quits to his captain! */
        if (msgrcv(msqid, (struct msgbuf *)&buf, sizeof(buf)-sizeof(long), 0, 0) == -1) {
            perror("msgrcv");
            exit(1);
        }
        printf("Spock: received: \"%s\". \nSending reinforcement!\n", buf.mtext);
    }

    return 0;
}



