using NAudio.CoreAudioApi;
using System.Diagnostics;

public class AudioController
{
    private readonly MMDevice defaultDevice;

    public AudioController()
    {
        var enumerator = new MMDeviceEnumerator();
        defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public bool SetVolume(string processName, float volumePercent)
    {
        volumePercent = Math.Clamp(volumePercent, 0f, 100f);
        var session = FindSession(processName);
        if (session != null)
        {
            session.SimpleAudioVolume.Volume = volumePercent / 100f;
            return true;
        }
        return false;
    }

    public bool ToggleMute(string processName)
    {
        var session = FindSession(processName);
        if (session != null)
        {
            var current = session.SimpleAudioVolume.Mute;
            session.SimpleAudioVolume.Mute = !current;
            return true;
        }
        return false;
    }

    public AudioSessionControl? FindSession(string processName)
    {
        var sessions = defaultDevice.AudioSessionManager.Sessions;
        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            try
            {
                var proc = Process.GetProcessById((int)session.GetProcessID);
                if (proc.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    return session;
            }
            catch
            {
                // Ignore processes that can't be found
            }
        }
        return null;
    }
}