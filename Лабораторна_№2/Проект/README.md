# ЛР2 — GUI клієнта

- `StudentRating.Client` — WPF (.NET Framework 4.8, Windows, Visual Studio): `StudentRating.sln`
- `StudentRating.Avalonia` — той самий C#/XAML-клієнт на Avalonia 11 (.NET 8), запускається на macOS/Linux/Windows.
  Моделі та сервіс даних спільні (підключені з `StudentRating.Client`).

Запуск Avalonia-версії:
```
cd StudentRating.Avalonia
dotnet run -- "<папка для скріншотів>"
```
F12 у будь-якому вікні — зберегти знімок вікна в PNG.
Демо-входи: student/1111, teacher/2222, admin/admin.
