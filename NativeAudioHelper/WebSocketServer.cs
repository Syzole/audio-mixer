using Fleck;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class AudioWebSocketServer
{
    private readonly AudioController audioController;

    public AudioWebSocketServer(AudioController controller)
    {
        audioController = controller;
    }

    public void Start()
    {
        FleckLog.Level = LogLevel.Warn;
        var server = new WebSocketServer("ws://127.0.0.1:8181");

        server.Start(socket =>
        {
            socket.OnOpen = () => Console.WriteLine("Client connected");
            socket.OnClose = () => Console.WriteLine("Client disconnected");
            socket.OnMessage = message =>
            {
                try
                {
                    JObject msg = JObject.Parse(message);
                    Console.WriteLine("Received message: " + message);
                    string? type = (string?)msg["type"];
                    Console.WriteLine("Received message: " + type);
                    string? process = (string?)msg["app"];
                    Console.WriteLine("Process: " + process);

                    if (process == null || type == null)
                    {
                        socket.Send("{\"error\":\"missing fields\"}");
                        return;
                    }

                    if (type == "setVolume")
                    {
                        float value = (float?)msg["value"] ?? 0f;
                        bool success = audioController.SetVolume(process, value);
                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\"}}");
                    }
                    else if (type == "toggleMute")
                    {
                        bool success = audioController.ToggleMute(process);
                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\"}}");
                    }
                    else if (type == "getStatus")
                    {
                        bool success = false;
                        float currentVolume = 0;

                        var session = audioController.FindSession(process);
                        if (session != null)
                        {
                            currentVolume = session.SimpleAudioVolume.Volume * 100f;
                            currentVolume = (float)Math.Round(currentVolume); // Round to nearest integer
                            success = true;
                        }

                        socket.Send($"{{\"app\":\"{process}\",\"volume\":{currentVolume},\"status\":\"{(success ? "ok" : "fail")}\"}}");
                    }
                    else if (type == "adjustVolume")
                    {
                        bool success = false;
                        var session = audioController.FindSession(process);
                        if (session != null)
                        {
                            float step = 0.05f; // 5%
                            float current = session.SimpleAudioVolume.Volume;
                            string? direction = (string?)msg["direction"];
                            float newVolume = direction == "up"
                                ? Math.Min(current + step, 1f)
                                : Math.Max(current - step, 0f);

                            session.SimpleAudioVolume.Volume = newVolume;
                            success = true;
                        }

                        int roundedVolume = session != null ? (int)Math.Round(session.SimpleAudioVolume.Volume * 100f) : 0;
                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\",\"volume\":{roundedVolume}}}");
                    }

                    else
                    {
                        socket.Send("{\"error\":\"unknown command\"}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    socket.Send("{\"error\":\"bad format\"}");
                }
            };
        });

        Console.WriteLine("WebSocket server started on ws://127.0.0.1:8181");
    }
}
