# ЛР4 — комплексна система

- `StudentRating.Server` — TCP-сервер + SQLite (з ЛР3).
- `StudentRating.Client` — графічний клієнт (Avalonia, C#/XAML), працює з сервером через `Services/ServerApi.cs`.
- `StudentRating.Tests` — 20 автоматизованих тестових процедур.
- `run_lab4.sh` — зібрати все, запустити сервер, тести та демо-сценарій клієнта (знімки вікон → `../Звіт/screens`).

Ручний запуск: спочатку сервер (`cd StudentRating.Server && dotnet run`), потім клієнт (`cd StudentRating.Client && dotnet run`).
