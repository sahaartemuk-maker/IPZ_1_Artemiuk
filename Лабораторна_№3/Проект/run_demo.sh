#!/bin/bash
# Запуск демонстрації ЛР3: сервер + тестовий клієнт + вміст таблиць БД.
export PATH="/usr/local/share/dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")"
OUT="../Звіт/results"; mkdir -p "$OUT"
rm -f StudentRating.Server/rating.db StudentRating.Server/server.log

echo "== Збірка =="
dotnet build StudentRating.Server -v q -nologo || exit 1
dotnet build StudentRating.TestClient -v q -nologo || exit 1

echo "== Запуск сервера =="
(cd StudentRating.Server && exec dotnet bin/Debug/net8.0/StudentRating.Server.dll) &
SERVER=$!
sleep 4

echo "== Тестовий клієнт =="
(cd StudentRating.TestClient && dotnet bin/Debug/net8.0/StudentRating.TestClient.dll)

sleep 1
kill $SERVER 2>/dev/null; wait $SERVER 2>/dev/null

cp StudentRating.Server/server.log "$OUT/server.log"
cp StudentRating.TestClient/client.log "$OUT/client.log"
DB=StudentRating.Server/rating.db
{
  echo "sqlite> SELECT s.Id, s.LastName, s.GroupName, sub.Name, g.Score, g.UpdatedAt FROM Grades g JOIN Students s ON s.Id=g.StudentId JOIN Subjects sub ON sub.Id=g.SubjectId WHERE s.Id=1;"
  sqlite3 -header -column $DB "SELECT s.Id, s.LastName, s.GroupName, sub.Name AS Subject, g.Score, g.UpdatedAt FROM Grades g JOIN Students s ON s.Id=g.StudentId JOIN Subjects sub ON sub.Id=g.SubjectId WHERE s.Id=1;"
  echo
  echo "sqlite> SELECT * FROM RequestLog;"
  sqlite3 -header -column $DB "SELECT * FROM RequestLog;"
  echo
  echo "sqlite> SELECT Id, Login, FullName, Role FROM Users;"
  sqlite3 -header -column $DB "SELECT Id, Login, FullName, Role FROM Users;"
} > "$OUT/db.txt" 2>&1
cat "$OUT/db.txt"
echo
echo "Готово. Результати: Лабораторна_№3/Звіт/results"
