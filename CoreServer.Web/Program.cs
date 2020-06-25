using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Rssdp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace CoreServer
{
    public class Program
    {
        // Declare _Publisher as a field so it doesn't get GCed after the method finishes
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
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                ipAddress = endPoint.Address.ToString();
            }

            Console.WriteLine($"IP address: {ipAddress}");

            var rootDevice = new SsdpRootDevice()
            {
                CacheLifetime = TimeSpan.FromMinutes(30), //How long SSDP clients can cache this info.
                Location = new Uri($"http://{ipAddress}"),
                DeviceTypeNamespace = "UOFileServer",
                DeviceType = "UOFileServer",
                FriendlyName = "UOFileServer",
                Manufacturer = "MobileUO",
                ModelName = "UOFileServer",
                Uuid = Environment.MachineName
            };

            var loginServerAddressPortTuple = GetLoginServerAddressPortTuple();
            if (loginServerAddressPortTuple != null)
            {
                Console.WriteLine($"LoginServer:{loginServerAddressPortTuple.Item1}");
                Console.WriteLine($"LoginPort:{loginServerAddressPortTuple.Item2}");
                rootDevice.CustomResponseHeaders.Add(new CustomHttpHeader("LoginServer", loginServerAddressPortTuple.Item1));
                rootDevice.CustomResponseHeaders.Add(new CustomHttpHeader("LoginPort", loginServerAddressPortTuple.Item2));
            }

            var clientVersion = GetClientVersion();
            if(string.IsNullOrEmpty(clientVersion) == false)
            {
                Console.WriteLine($"ClientVersion:{clientVersion}");
                rootDevice.CustomResponseHeaders.Add(new CustomHttpHeader("ClientVersion", clientVersion));
            }

            _Publisher = new SsdpDevicePublisher();
            _Publisher.AddDevice(rootDevice);
        }

        private static FileInfo GetFileInCurrentDirectory(string fileName)
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles().FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string GetClientVersion()
        {
            var clientExeFile = GetFileInCurrentDirectory("client.exe");
            if(clientExeFile != null)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(clientExeFile.FullName);
                if (string.IsNullOrEmpty(versionInfo?.FileVersion) == false)
                {
                    var version = versionInfo.FileVersion.Replace(",", ".").Replace(" ", "").ToLower();
                    return version;
                }
            }
            return null;
        }

        private static Tuple<string,string> GetLoginServerAddressPortTuple()
        {
            var loginCfgFile = GetFileInCurrentDirectory("login.cfg");
            if (loginCfgFile != null)
            {
                using (var stream = loginCfgFile.OpenText())
                {
                    var text = stream.ReadToEnd();
                    var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    const string loginServerPrefix = "LoginServer=";
                    var loginServerLine = lines.FirstOrDefault(l => l.StartsWith(loginServerPrefix));
                    if (loginServerLine != null)
                    {
                        var commaIndex = loginServerLine.IndexOf(',');
                        var serverAddress = loginServerLine.Substring(loginServerPrefix.Length, commaIndex - loginServerPrefix.Length);
                        var port = loginServerLine.Substring(commaIndex + 1, loginServerLine.Length - commaIndex - 1);
                        return new Tuple<string,string>(serverAddress, port);
                    }
                }
            }
            return null;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls($"http://{ipAddress}:{port}");
                });
    }
}
