class Program
{
    static void Main(string[] args)
    {
        Console.Title = "NativeAudioHelper";
        var audioController = new AudioController();
        var server = new AudioWebSocketServer(audioController);

        server.Start();

        Console.WriteLine("✅ NativeAudioHelper is running. Press Enter to exit.");
        Console.ReadLine();
    }
}
