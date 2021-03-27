#!/bin/bash
echo
for i in {1..20}
do
    dotnet run --no-build $i $(($RANDOM % 2)) &
done
