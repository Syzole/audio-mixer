using Fleck;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.Drawing;

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
                    string? process = (string?)msg["app"];
                    int? amount = (int?)msg["amount"];

                    if (type == null)
                    {
                        socket.Send("{\"error\":\"missing type\"}");
                        return;
                    }

                    if (type == "setVolume")
                    {
                        if (process == null)
                        {
                            socket.Send("{\"error\":\"missing app\"}");
                            return;
                        }

                        float value = (float?)msg["value"] ?? 0f;
                        bool success = audioController.SetVolume(process, value);
                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\"}}");
                    }
                    else if (type == "toggleMute")
                    {
                        if (process == null)
                        {
                            socket.Send("{\"error\":\"missing app\"}");
                            return;
                        }

                        bool success = audioController.ToggleMute(process);
                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\"}}");
                    }
                    else if (type == "getStatus")
                    {
                        if (process == null)
                        {
                            socket.Send("{\"error\":\"missing app\"}");
                            return;
                        }

                        var session = audioController.FindSession(process);
                        if (session != null)
                        {
                            float currentVolume = session.SimpleAudioVolume.Volume * 100f;
                            socket.Send($"{{\"app\":\"{process}\",\"volume\":{Math.Round(currentVolume)},\"status\":\"ok\"}}");
                        }
                        else
                        {
                            socket.Send($"{{\"app\":\"{process}\",\"volume\":0,\"status\":\"fail\"}}");
                        }
                    }
                    else if (type == "adjustVolume")
                    {
                        if (process == null)
                        {
                            socket.Send("{\"error\":\"missing app\"}");
                            return;
                        }

                        var session = audioController.FindSession(process);
                        bool success = false;
                        int roundedVolume = 0;

                        if (session != null)
                        {
                            float step = ((float?)amount ?? 0f) / 100f;
                            float current = session.SimpleAudioVolume.Volume;
                            string? direction = (string?)msg["direction"];

                            float newVolume = direction == "up"
                                ? Math.Min(current + step, 1f)
                                : Math.Max(current - step, 0f);

                            session.SimpleAudioVolume.Volume = newVolume;
                            success = true;
                            roundedVolume = (int)Math.Round(newVolume * 100f);
                        }

                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\",\"volume\":{roundedVolume}}}");
                    }
                    else if (type == "getRunningApps")
                    {
                        var enumerator = new MMDeviceEnumerator();
                        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        var appList = new JArray();
                        var uniqueApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var device in devices)
                        {
                            var sessions = device.AudioSessionManager.Sessions;
                            for (int i = 0; i < sessions.Count; i++)
                            {
                                var session = sessions[i];
                                string procName = "Unknown";
                                string iconBase64 = "";

                                try
                                {
                                    int pid = (int)session.GetProcessID;
                                    if (pid == 0)
                                    {
                                        procName = "System Sounds";
                                    }
                                    else
                                    {
                                        var proc = Process.GetProcessById(pid);
                                        procName = proc.ProcessName;

                                        if (uniqueApps.Add(procName))
                                        {
                                            try
                                            {
                                                string exePath = proc.MainModule?.FileName ?? "";
                                                if (!string.IsNullOrEmpty(exePath))
                                                {
                                                    using (Icon icon = Icon.ExtractAssociatedIcon(exePath))
                                                    {
                                                        if (icon != null)
                                                        {
                                                            using var bmp = icon.ToBitmap();
                                                            using var ms = new MemoryStream();
                                                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                                            iconBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                                                        }
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                // ignore icon extraction errors
                                            }

                                            appList.Add(new JObject
                                            {
                                                ["app"] = procName,
                                                ["icon"] = iconBase64
                                            });
                                        }
                                    }
                                }
                                catch
                                {
                                    // fallback for inaccessible processes
                                    if (uniqueApps.Add(procName))
                                    {
                                        appList.Add(new JObject
                                        {
                                            ["app"] = procName,
                                            ["icon"] = iconBase64
                                        });
                                    }
                                }
                            }
                        }

                        string appsJson = appList.ToString();
                        Console.WriteLine("Sending running apps with icons: " + appsJson);
                        socket.Send($"{{\"runningApps\":{appsJson}}}");
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
