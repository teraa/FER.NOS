#!/bin/bash
for file in {msg,rnd}; do
    gcc -shared $file.c -o $file.so
done
