using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using StateKernel.Runtime.Abstractions;

namespace StateKernel.Runtime.UaNet;

internal sealed class UaNetEndpointSetVerifier : IUaNetEndpointSetVerifier
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });

    public async ValueTask VerifyAsync(
        ApplicationConfiguration configuration,
        string endpointUrl,
        RuntimeEndpointProfile endpointProfile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointUrl);
        ArgumentNullException.ThrowIfNull(endpointProfile);

        if (endpointProfile.Id != RuntimeEndpointProfiles.BaselineSecure.Id)
        {
            return;
        }

        var endpoints = await DiscoverEndpointsAsync(
            configuration,
            endpointUrl,
            cancellationToken);

        if (endpoints.Count == 0)
        {
            throw new InvalidOperationException(
                "Secure startup verification discovered no exposed OPC UA endpoints.");
        }

        var insecureEndpoint = endpoints.FirstOrDefault(static endpoint =>
            endpoint.SecurityMode == MessageSecurityMode.None ||
            string.Equals(
                endpoint.SecurityPolicyUri,
                SecurityPolicies.None,
                StringComparison.Ordinal));

        if (insecureEndpoint is not null)
        {
            throw new InvalidOperationException(
                "Secure startup verification discovered an insecure OPC UA endpoint.");
        }

        var unexpectedEndpoint = endpoints.FirstOrDefault(static endpoint =>
            endpoint.SecurityMode != MessageSecurityMode.SignAndEncrypt ||
            !string.Equals(
                endpoint.SecurityPolicyUri,
                SecurityPolicies.Basic256Sha256,
                StringComparison.Ordinal));

        if (unexpectedEndpoint is not null)
        {
            throw new InvalidOperationException(
                "Secure startup verification discovered an endpoint outside the bounded baseline-secure contract.");
        }
    }

    private static async Task<EndpointDescriptionCollection> DiscoverEndpointsAsync(
        ApplicationConfiguration configuration,
        string endpointUrl,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var discoveryClient = await DiscoveryClient.CreateAsync(
                    configuration,
                    new Uri(endpointUrl),
                    DiagnosticsMasks.None,
                    cancellationToken);

                return await discoveryClient.GetEndpointsAsync(null, cancellationToken);
            }
            catch (Exception exception) when (attempt < 19)
            {
                lastException = exception;
                await Task.Delay(100, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException(
            "Secure startup verification could not enumerate the exposed OPC UA endpoint set.");
    }
}
