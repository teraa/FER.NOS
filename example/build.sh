#!/bin/bash
for file in {kirk,spock}; do
    gcc $file.c -o $file.out
done
