#!/bin/bash
# ЛР4: збірка, запуск сервера з чистою БД, автотести, демо-сценарій клієнта зі знімками.
export PATH="/usr/local/share/dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")"
OUT="$(cd .. && pwd)/Звіт/results"; SHOTS="$(cd .. && pwd)/Звіт/screens"
mkdir -p "$OUT" "$SHOTS"; rm -f "$SHOTS"/*.png
rm -f StudentRating.Server/rating.db StudentRating.Server/server.log

echo "== Збірка =="
for p in StudentRating.Server StudentRating.Tests StudentRating.Client; do
  dotnet build $p -v q -nologo || exit 1
done

echo "== Сервер =="
(cd StudentRating.Server && exec dotnet bin/Debug/net8.0/StudentRating.Server.dll) &
SERVER=$!
sleep 4

echo "== Тести =="
(cd StudentRating.Tests && dotnet bin/Debug/net8.0/StudentRating.Tests.dll 127.0.0.1 5050 "$OUT/tests.json") | tee "$OUT/tests.txt"

echo "== Демо-сценарій клієнта (вікна з'являтимуться самі, нічого не натискай) =="
(cd StudentRating.Client && dotnet bin/Debug/net8.0/StudentRating.dll --demo "$SHOTS")

kill $SERVER 2>/dev/null; wait $SERVER 2>/dev/null
cp StudentRating.Server/server.log "$OUT/server.log"
sqlite3 -header -column StudentRating.Server/rating.db \
  "SELECT Command, Status, COUNT(*) AS N FROM RequestLog GROUP BY Command, Status ORDER BY Command;" > "$OUT/requestlog.txt"
echo
ls "$SHOTS"
echo "Готово. Результати: Лабораторна_№4/Звіт/results і screens"
