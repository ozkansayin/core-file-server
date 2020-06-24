using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Rssdp;
using System;
using System.Net;
using System.Net.Sockets;

namespace CoreServer
{
    public class Program
    {
        // Declare \_Publisher as a field somewhere, so it doesn't get GCed after the method finishes.
        private static SsdpDevicePublisher _Publisher;

        private static string ipAddress;
        private const int port = 8080;

        public static void Main(string[] args)
        {
            PublishDeviceRSSDP();
            CreateHostBuilder(args).Build().Run();
        }

        private static void PublishDeviceRSSDP()
        {
            Console.WriteLine("MachineName: {0}", Environment.MachineName);

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                ipAddress = endPoint.Address.ToString();
            }

            Console.WriteLine($"IP address: {ipAddress}");

            // As this is a sample, we are only setting the minimum required properties.
            var deviceDefinition = new SsdpRootDevice()
            {
                CacheLifetime = TimeSpan.FromMinutes(30), //How long SSDP clients can cache this info.
                Location = new Uri($"http://{ipAddress}"), // Must point to the URL that serves your devices UPnP description document. 
                DeviceTypeNamespace = "UOFileServer",
                DeviceType = "UOFileServer",
                FriendlyName = "UOFileServer",
                Manufacturer = "MobileUO",
                ModelName = "UOFileServer",
                Uuid = Environment.MachineName // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
            };

            _Publisher = new SsdpDevicePublisher();
            _Publisher.AddDevice(deviceDefinition);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls($"http://{ipAddress}:{port}");
                });
    }
}
