#!/bin/bash
readonly MIN_N=3
readonly MAX_N=10

rm -f "data.db"

if [ $# -eq 0 ]; then
    n=$(($RANDOM % ($MAX_N - $MIN_N) + $MIN_N))
elif [ $# -eq 1 ]; then
    n=$1
else
    echo "Usage: $0 [N]"
    echo "    N - broj procesa"
fi

echo "Starting $n processes"
trap 'kill $(jobs -p)' SIGINT SIGTERM

for i in $(seq 1 $n); do
    ./bin/Zad1b $(($n - 1)) $(($i - 1)) &
done

wait
echo "done"
