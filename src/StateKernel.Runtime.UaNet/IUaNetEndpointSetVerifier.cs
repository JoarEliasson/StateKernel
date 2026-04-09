using Opc.Ua;
using Opc.Ua.Configuration;
using StateKernel.Runtime.Abstractions;

namespace StateKernel.Runtime.UaNet;

internal interface IUaNetEndpointSetVerifier
{
    ValueTask VerifyAsync(
        ApplicationConfiguration configuration,
        string endpointUrl,
        RuntimeEndpointProfile endpointProfile,
        CancellationToken cancellationToken);
}
