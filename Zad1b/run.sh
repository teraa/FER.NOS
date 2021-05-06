#!/bin/bash
readonly MIN_N=3
readonly MAX_N=10

db_file="data.db"
runs=5

rm -f $db_file

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
    ./bin/Zad1b $(($i - 1)) $(($n - 1)) $runs $db_file &
done

wait
echo "done"
