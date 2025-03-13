using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;

class Program
{
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: pingg <host>");
            return;
        }

        string host = args[0];
        Ping ping = new Ping();
        PingOptions options = new PingOptions(64, true); // Set TTL to 64 and don't fragment
        int timeout = 1000;
        Random random = new Random();
        byte[] buffer = new byte[32];

        // Handle Ctrl+C to exit gracefully
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent the process from terminating immediately
            isRunning = false; // Set the running flag to false
            Console.WriteLine("\nExiting...");
        };

        while (isRunning)
        {
            // Fill the buffer with random bytes
            random.NextBytes(buffer);

            try
            {
                PingReply reply = ping.Send(host, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    int rtt = (int)reply.RoundtripTime;
                    Console.WriteLine($"{rtt} {new string('.', rtt)}"); // Print one dot for each millisecond of the round-trip time
                }
                else
                {
                    LogError($"Ping failed: {reply.Status}");
                }
            }
            catch (PingException ex)
            {
                LogError($"Ping failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"An error occurred: {ex.Message}");
            }

            // Wait for a second before the next ping
            Thread.Sleep(1000);
        }
    }

    static void LogError(string message)
    {
        string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ping_errors.log");
        string logMessage = $"{DateTime.Now}: {message}";

        // Append the error message to the log file
        File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        Console.WriteLine(logMessage); // Optionally print the log message to the console
    }
}