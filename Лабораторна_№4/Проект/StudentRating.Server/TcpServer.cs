using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using StudentRating.Protocol;

namespace StudentRating.Server
{
    /// <summary>TCP-сервер: приймає клієнтів і обробляє кожного в окремому потоці.</summary>
    public class TcpServer
    {
        private readonly TcpListener _listener;
        private readonly CommandHandler _handler;
        private readonly Database _db;
        private volatile bool _running;

        public TcpServer(int port, CommandHandler handler, Database db)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _handler = handler;
            _db = db;
        }

        public void Start()
        {
            _listener.Start();
            _running = true;
            Logger.Info($"Слухаю TCP-порт {((IPEndPoint)_listener.LocalEndpoint).Port}");
            new Thread(AcceptLoop) { IsBackground = true, Name = "Accept" }.Start();
        }

        public void Stop()
        {
            _running = false;
            _listener.Stop();
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    new Thread(() => HandleClient(client)) { IsBackground = true }.Start();
                }
                catch (SocketException) when (!_running) { break; }
                catch (Exception ex) { Logger.Error("Accept: " + ex.Message); }
            }
        }

        private void HandleClient(TcpClient client)
        {
            var name = client.Client.RemoteEndPoint?.ToString() ?? "?";
            Logger.Info($"Підключився клієнт {name}");
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var channel = new PacketChannel(stream);
                    string line;
                    while ((line = channel.ReadLine()) != null)
                    {
                        if (line.Trim().Length == 0) continue;
                        Logger.In(name, line);
                        Response response;
                        Request request = null;
                        try
                        {
                            request = JsonSerializer.Deserialize<Request>(line, PacketChannel.Json);
                            response = _handler.Handle(request);
                        }
                        catch (JsonException)
                        {
                            response = Response.Fail(Statuses.Error, "Некоректний формат пакета (очікується JSON)");
                        }
                        catch (Exception ex)
                        {
                            response = Response.Fail(Statuses.Error, "Помилка сервера: " + ex.Message);
                        }

                        var json = JsonSerializer.Serialize(response, PacketChannel.Json);
                        channel.WriteLine(json);
                        Logger.Out(name, json.Length > 300 ? json.Substring(0, 300) + "…" : json);
                        _db.LogRequest(name, request?.Command ?? "?", response.Status);
                    }
                }
            }
            catch (System.IO.IOException) { /* клієнт розірвав з'єднання */ }
            catch (Exception ex) { Logger.Error($"[{name}] {ex.Message}"); }
            Logger.Info($"Клієнт {name} відключився");
        }
    }
}
