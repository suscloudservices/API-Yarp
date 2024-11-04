using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

public class EnvConfigFilter : IProxyConfigFilter
{
    private readonly Regex _envVarPattern = new("\\{\\{(\\w+)\\}\\}");

    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig origCluster, CancellationToken cancel)
    {
        var newDests = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var dest in origCluster.Destinations)
        {
            var origAddress = dest.Value.Address;
            if (_envVarPattern.IsMatch(origAddress))
            {
                var envVarName = _envVarPattern.Match(origAddress).Groups[1].Value;
                var envValue = Environment.GetEnvironmentVariable(envVarName);

                if (string.IsNullOrWhiteSpace(envValue))
                {
                    throw new ArgumentException($"Environment variable '{envVarName}' not found for destination '{dest.Key}'.");
                }

                var updatedDest = dest.Value with { Address = envValue };
                newDests.Add(dest.Key, updatedDest);
            }
            else
            {
                newDests.Add(dest.Key, dest.Value);
            }
        }

        return new ValueTask<ClusterConfig>(origCluster with { Destinations = newDests });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig cluster, CancellationToken cancel)
    {
        return new ValueTask<RouteConfig>(route);
    }
}
