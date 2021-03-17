#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <sys/ipc.h>
#include <sys/msg.h>

struct my_msgbuf {
    long mtype;
    char mtext[200];
};

void print(const char *message)
{
    printf("%s\n", message);
}

void test_struct(struct my_msgbuf *valuep)
{
    printf("mtype=%ld, mtext=%s\n", valuep->mtype, valuep->mtext);
}

void test_chars(char text[100])
{
    printf("%s\n", text);
}

int my_msgctl(int msqid, int cmd)
{
    msgctl(msqid, cmd, NULL);
}

char *get_error()
{
    return strerror(errno);
}

int main(void)
{
    test_chars("test_chars");

    struct my_msgbuf val = { 1337, "test_struct" };
    test_struct(&val);
}
