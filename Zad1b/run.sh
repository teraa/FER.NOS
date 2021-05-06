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

echo "Nodes: $n, Runs: $runs, DB: $db_file"
trap 'kill $(jobs -p)' SIGINT SIGTERM

peers=$(($n - 1))

for i in $(seq 1 $n); do
    id=$(($i - 1))
    ./bin/Zad1b $id $peers $runs $db_file &
done

wait
echo "done"
