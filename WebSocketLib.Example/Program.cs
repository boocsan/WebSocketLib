using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketLib.Example
{
    internal class Program
    {
        private static void Main()
        {
            Task.Run(async () =>
            {
                var server = new Server();
                server.Start(new[] {"http://localhost:57348/"});
                while (true)
                {
                    await server.SendMessage($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.ffffff}");
                    await Task.Delay(1000);
                }
                // ReSharper disable once FunctionNeverReturns
            });

            Task.Run(() =>
            {
                var client = new Client();
                client.OnMessage += message => Console.WriteLine($"[CLIENT {DateTime.Now:HH:mm:ss.ffffff}] Received. {message}");
                client.Opened += () => Console.WriteLine($"[CLIENT {DateTime.Now:HH:mm:ss.ffffff}] Opened.");
                client.Closed += () => Console.WriteLine($"[CLIENT {DateTime.Now:HH:mm:ss.ffffff}] Closed.");
                client.Error += e => Console.WriteLine($"[CLIENT {DateTime.Now:HH:mm:ss.ffffff}] Error. {e.Message}");
                client.Open("ws://localhost:57348");
            });

            new ManualResetEvent(false).WaitOne();
        }
    }
}
