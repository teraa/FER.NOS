#!/bin/bash

trap 'kill $(jobs -p)' EXIT

for i in {1..20}; do
    dotnet run --no-build -c Release -- $i $(($RANDOM % 2)) &
done

wait
echo "done"
