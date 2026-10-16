using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace RentoX.Api.Hosting;

/// <summary>
/// Behind the web server and a reverse proxy every request reaches the API from the same address, so the real visitor
/// address has to be read from <c>X-Forwarded-For</c>. That header is trusted ONLY when the request comes from a
/// trusted proxy (anyone could write it otherwise and escape the per-address limits).
/// </summary>
/// <remarks>
/// Trusted by default: this machine and the private networks (Docker, the local network). Set
/// <c>ForwardedHeaders:KnownNetworks</c> (CIDR, e.g. "10.0.0.0/8") and <c>ForwardedHeaders:KnownProxies</c> to narrow it
/// down. The API itself must not be reachable from the internet except through the proxy.
/// </remarks>
public static class ForwardedHeadersSetup
{
    private static readonly string[] DefaultNetworks =
        ["127.0.0.0/8", "::1/128", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "fc00::/7"];

    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // A chain of proxies is read from the right: only the entries added by trusted ones are skipped.
            options.ForwardLimit = configuration.GetValue("ForwardedHeaders:ForwardLimit", 2);

            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (string proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out IPAddress? address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            string[] networks =
                configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() is { Length: > 0 } configured
                    ? configured
                    : DefaultNetworks;

            foreach (string network in networks)
            {
                if (System.Net.IPNetwork.TryParse(network, out System.Net.IPNetwork parsed))
                {
                    options.KnownIPNetworks.Add(parsed);
                }
            }
        });
    }
}
