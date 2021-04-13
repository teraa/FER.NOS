#!/bin/bash
for file in {kirk,spock,manpage}; do
    gcc $file.c -o $file.out
done
