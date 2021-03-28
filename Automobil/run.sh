#!/bin/bash
readonly MIN_N=5
readonly MAX_N=100

if [ $# -eq 0 ]; then
    n=$(($RANDOM % ($MAX_N - $MIN_N) + $MIN_N))
elif [ $# -eq 1 ]; then
    n=$1
else
    echo "Usage: $0 [N]"
    echo "    N - broj automobila"
fi

echo "Starting $n processes"
trap 'kill $(jobs -p)' SIGINT SIGTERM

for i in $(seq 1 $n); do
    ./bin/Automobil $i $(($RANDOM % 2)) &
done

wait
echo "done"
