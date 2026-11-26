# ЛР3 — серверна частина

- `StudentRating.Server` — консольний TCP-сервер (порт 5050) з БД SQLite (`rating.db`).
- `StudentRating.TestClient` — консольний клієнт, що надсилає серію команд і показує відповіді.
- `Protocol/Protocol.cs` — спільний опис протоколу (JSON-рядки по TCP).

Швидкий запуск усієї демонстрації (macOS/Linux): `./run_demo.sh`

Окремо:
```
cd StudentRating.Server && dotnet run            # [порт] [файл_БД]
cd StudentRating.TestClient && dotnet run        # [host] [port]
```
