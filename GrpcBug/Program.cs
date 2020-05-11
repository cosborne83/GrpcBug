using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcBug
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new BugTest();
            client.Test();
        }
    }

    class BugTest
    {
        private readonly object _lock = new object();
        private Channel _channel;
        private TestBug.TestBugClient _client;

        private TestBug.TestBugClient Client { get { lock (_lock) return _client; } }

        public void Test()
        {
            Task.Run(() => Connect());
            Console.ReadLine();
        }

        private void Call()
        {
            try
            {
                Client.Request(new TestRequest());
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Caught");
            }
        }

        private void Connect()
        {
            while (true)
            {
                lock (_lock)
                {
                    // There should *not* be a gRPC service listening on this port
                    _client = new TestBug.TestBugClient(_channel = new Channel("127.0.0.1", 12345, ChannelCredentials.Insecure));
                    Task.Run(() => Call());
                    try
                    {
                        var tokenSource = new CancellationTokenSource(2000);
                        _channel.ConnectAsync().Wait(tokenSource.Token);
                    }
                    catch
                    {
                        _channel.ShutdownAsync().Wait();
                    }
                }

                Thread.Sleep(500);
            }
        }
    }
}