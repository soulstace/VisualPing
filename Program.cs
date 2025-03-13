using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static volatile bool isRunning = true;
    private static readonly byte[] buffer = new byte[32];

    static void ShowUsage()
    {
        Console.WriteLine("Usage: pingg <host> [-w:timeout]");
    }

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }

        string host = args[0];
        int timeout = ParseTimeout(args);
        var ping = new Ping();
        var options = new PingOptions(64, true); // Set TTL to 64 and don't fragment
        var random = new Random();
        var waiter = new AutoResetEvent(false);

        ping.PingCompleted += (sender, e) => PingCompletedCallback(e, waiter);

        // Handle Ctrl+C for graceful exit
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            isRunning = false; // Set the running flag to false
            Console.WriteLine("\nExiting...");
        };

        while (isRunning)
        {
            random.NextBytes(buffer); // Fill the buffer with random bytes

            if (!waiter.WaitOne(0)) // Non-blocking check
            {
                ping.SendAsync(host, timeout, buffer, options, waiter); // Send the ping asynchronously
            }

            waiter.WaitOne(); // Wait for the ping to complete
            Thread.Sleep(1000); // Delay before the next ping
        }
    }

    private static int ParseTimeout(string[] args)
    {
        const string timeoutArg = "-w:";
        int timeout = 1000; // Default timeout

        foreach (var arg in args)
        {
            if (arg.StartsWith(timeoutArg))
            {
                if (int.TryParse(arg.Substring(timeoutArg.Length), out timeout))
                {
                    return timeout;
                }
                else
                {
                    ShowUsage();
                    Environment.Exit(1);
                }
            }
        }

        return timeout;
    }

    private static void PingCompletedCallback(PingCompletedEventArgs e, AutoResetEvent waiter)
    {
        if (e.Cancelled)
        {
            Console.WriteLine("Ping canceled.");
            waiter.Set();
            return;
        }

        if (e.Error != null)
        {
            _ = LogErrorAsync(e.Error.ToString());
            waiter.Set();
            return;
        }

        DisplayReply(e.Reply);
        waiter.Set(); // Let the main thread resume
    }

    private static void DisplayReply(PingReply reply)
    {
        if (reply == null) return;

        if (reply.Status == IPStatus.Success)
        {
            int rtt = (int)reply.RoundtripTime;
            Console.WriteLine($"{rtt} {new string('.', rtt)}"); // Print dots for round-trip time

            if (!reply.Buffer.SequenceEqual(buffer))
            {
                _ = LogErrorAsync("Buffer mismatch detected!");
            }
        }
        else
        {
            _ = LogErrorAsync($"Ping failed: {reply.Status}");
        }
    }

    private static async Task LogErrorAsync(string message)
    {
        string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ping_errors.log");
        string logMessage = $"{DateTime.Now}: {message}";

        using (var writer = new StreamWriter(logFilePath, true))
        {
            await writer.WriteLineAsync(logMessage);
        }

        Console.WriteLine(logMessage);
    }
}