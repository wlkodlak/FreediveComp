using Microsoft.Owin;
using Microsoft.Owin.BuilderProperties;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace MilanWilczak.FreediveComp
{
    public class UdpDiscoveryConfig
    {
        public int Port { get; set; }
    }

    public static class UdpDiscoveryExtensions
    {
        private const int DiscoveryPort = 51693;

        public static void UseUdpDiscovery(this IAppBuilder app)
        {
            var context = new OwinContext(app.Properties);
            var hostAddresses = context.Get<IList<IDictionary<string, object>>>("host.Addresses");
            var addresses = new AddressCollection(hostAddresses);
            var cancel = context.Get<CancellationToken>("host.OnAppDisposing");
            var uris = addresses
                .Where(a => !string.IsNullOrEmpty(a.Port))
                .Select(a => new UriBuilder(a.Scheme, a.Host, int.Parse(a.Port), a.Path).ToString())
                .ToList();

            RunDiscoveryListener(uris, cancel).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Trace.WriteLine("There were problems with discovery: " + t.Exception.ToString());
                }
            });
        }

        public static async Task RunDiscoveryListener(List<String> uris, CancellationToken cancellationToken)
        {
            var udpClient = new UdpClient(DiscoveryPort, AddressFamily.InterNetwork);
            cancellationToken.Register(() => udpClient.Dispose());
            while (!cancellationToken.IsCancellationRequested)
            {
                var receiveResult = await udpClient.ReceiveAsync();
                var incomingMessage = Encoding.UTF8.GetString(receiveResult.Buffer);
                if (incomingMessage == "FreediveComp.Discover")
                {
                    foreach (var uri in uris)
                    {
                        var responseBytes = Encoding.UTF8.GetBytes("FreediveComp.Response:" + uri);
                        udpClient.Send(responseBytes, responseBytes.Length, receiveResult.RemoteEndPoint);
                    }
                }
            }
        }
    }
}