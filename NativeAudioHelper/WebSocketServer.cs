using Fleck;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using NAudio.CoreAudioApi;

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
                    int? amount = (int?)msg["amount"];
                    Console.WriteLine("Amount: " + amount);

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
                        bool success = false;
                        float currentVolume = 0;

                        if (process == null)
                        {
                            socket.Send("{\"error\":\"missing app\"}");
                            return;
                        }

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

                        if (process == null)
                        {
                            socket.Send("{\"error\":\"missing app\"}");
                            return;
                        }

                        var session = audioController.FindSession(process);
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
                        }

                        int roundedVolume = session != null ? (int)Math.Round(session.SimpleAudioVolume.Volume * 100f) : 0;
                        socket.Send($"{{\"status\":\"{(success ? "ok" : "fail")}\",\"volume\":{roundedVolume}}}");
                    }

                    else if (type == "getRunningApps")
                    {
                        var enumerator = new MMDeviceEnumerator();
                        // Enumerate all active playback devices
                        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        var uniqueApps = new HashSet<string>();

                        foreach (var device in devices)
                        {
                            var sessions = device.AudioSessionManager.Sessions;
                            for (int i = 0; i < sessions.Count; i++)
                            {
                                var session = sessions[i];
                                string procName;

                                try
                                {
                                    int pid = (int)session.GetProcessID;
                                    if (pid == 0)
                                    {
                                        procName = "System Sounds"; // Special case
                                    }
                                    else
                                    {
                                        var proc = Process.GetProcessById(pid);
                                        procName = proc.ProcessName;
                                    }
                                }
                                catch
                                {
                                    procName = "Unknown"; // Could not get process
                                }

                                uniqueApps.Add(procName); // Add to HashSet to deduplicate
                            }
                        }

                        // Convert to JSON and send
                        string appsJson = uniqueApps.Count > 0 ? JArray.FromObject(uniqueApps).ToString() : "[]";
                        Console.WriteLine("Sending running apps: " + appsJson);

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
