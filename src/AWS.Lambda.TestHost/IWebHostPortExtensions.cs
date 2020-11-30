using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Logicality.AWS.Lambda.TestHost
{
    internal static class WebHostPortExtensions
    {
        public static int GetPort(this IWebHost host) 
            => host.GetPorts().First();

        internal static IEnumerable<int> GetPorts(this IWebHost host) =>
            host
                .GetUris()
                .Select(u => u.Port);

        internal static IEnumerable<Uri> GetUris(this IWebHost host) =>
            host
                .ServerFeatures
                .Get<IServerAddressesFeature>()
                .Addresses
                .Select(a => new Uri(a));
    }
}