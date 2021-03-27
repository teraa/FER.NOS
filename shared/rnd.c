#include <errno.h>
#include <string.h>
#include <stdio.h>

struct text_msgbuf {
    long mtype;
    char mtext[200];
};

struct my_msgbuf {
    long mtype;
    int car_id;
    int direction;
};

void print(const char *message)
{
    printf("%s\n", message);
}

void test_text_struct(struct text_msgbuf *valuep)
{
    printf("mtype=%ld, mtext=%s\n", valuep->mtype, valuep->mtext);
}

void test_my_struct(struct my_msgbuf *valuep)
{
    printf("mtype=%ld, car_id=%d, direction=%d\n", valuep->mtype, valuep->car_id, valuep->direction);
}

void test_chars(char text[100])
{
    printf("%s\n", text);
}

char *get_error()
{
    return strerror(errno);
}

int main(void)
{
    test_chars("test_chars");

    struct text_msgbuf val = { 1337, "test_text_struct" };
    test_text_struct(&val);
}
