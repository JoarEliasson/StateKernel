using System.Xml;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using StateKernel.Runtime.Abstractions;
using StateKernel.Runtime.UaNet;
using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.UaNet.Tests;

public sealed class UaNetRuntimeAdapterTests
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationSignalId UnknownSignal = SimulationSignalId.From("Unknown");

    [Fact]
    public async Task Adapter_StartsAndCreatesProjectedNodesWithExpectedDisplayNames()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest("Source Display");

        var startResult = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            using var session = await ConnectAsync(startResult.EndpointUrl, CancellationToken.None);
            var node = await session.ReadNodeAsync(
                CreateNodeId(session, RuntimeNodeId.ForSignal(SourceSignal)),
                CancellationToken.None);

            Assert.NotNull(node);
            Assert.Equal("Source Display", node.DisplayName.Text);
            Assert.Equal(1, startResult.ExposedNodeCount);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_AppliesProjectedValueUpdatesToProjectedNodes()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest();
        var startResult = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            await adapter.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SourceSignal, 12.5, 3),
                ],
                CancellationToken.None);

            using var session = await ConnectAsync(startResult.EndpointUrl, CancellationToken.None);
            var value = await session.ReadValueAsync<double>(
                CreateNodeId(session, RuntimeNodeId.ForSignal(SourceSignal)),
                CancellationToken.None);

            Assert.Equal(12.5, value);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_RejectsUnprojectedSignalUpdatesClearly()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest();
        _ = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                adapter.ApplyUpdatesAsync(
                    [
                        new RuntimeValueUpdate(UnknownSignal, 9.0, 1),
                    ],
                    CancellationToken.None).AsTask());

            Assert.Contains(UnknownSignal.Value, exception.Message);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_ExposesProjectedNodesAsReadOnlyToClients()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest();
        var startResult = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            using var session = await ConnectAsync(startResult.EndpointUrl, CancellationToken.None);
            var nodeId = CreateNodeId(session, RuntimeNodeId.ForSignal(SourceSignal));
            var writeValues = new WriteValueCollection
            {
                new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(99.0d)),
                },
            };

            var writeResponse = await session.WriteAsync(
                null,
                writeValues,
                CancellationToken.None);

            Assert.Single(writeResponse.Results);
            Assert.Equal(StatusCodes.BadNotWritable, writeResponse.Results[0]);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_CanRestartCleanlyAndServeUpdatedValuesAgain()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest();
        var firstStart = await adapter.StartAsync(request, CancellationToken.None);

        await adapter.ApplyUpdatesAsync(
            [
                new RuntimeValueUpdate(SourceSignal, 7.5, 1),
            ],
            CancellationToken.None);

        using (var firstSession = await ConnectAsync(firstStart.EndpointUrl, CancellationToken.None))
        {
            var firstValue = await firstSession.ReadValueAsync<double>(
                CreateNodeId(firstSession, RuntimeNodeId.ForSignal(SourceSignal)),
                CancellationToken.None);

            Assert.Equal(7.5, firstValue);
        }

        _ = await adapter.StopAsync(CancellationToken.None);

        var secondStart = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            await adapter.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SourceSignal, 11.0, 2),
                ],
                CancellationToken.None);

            using var secondSession = await ConnectAsync(secondStart.EndpointUrl, CancellationToken.None);
            var secondValue = await secondSession.ReadValueAsync<double>(
                CreateNodeId(secondSession, RuntimeNodeId.ForSignal(SourceSignal)),
                CancellationToken.None);

            Assert.Equal(11.0, secondValue);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_FailsFastOnInvalidLifecycleUsage()
    {
        var adapter = new UaNetRuntimeAdapter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.StopAsync(CancellationToken.None).AsTask());

        var request = CreateStartRequest();
        _ = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                adapter.StartAsync(request, CancellationToken.None).AsTask());
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_StartsWithLocalDevelopmentProfileAndResolvesAnActualPortWhenRequested()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest(
            endpoint: RuntimeEndpointSettings.Loopback(0),
            endpointProfile: RuntimeEndpointProfiles.LocalDevelopment);

        var startResult = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            var endpointUri = new Uri(startResult.EndpointUrl);

            Assert.True(endpointUri.Port > 0);
            Assert.Equal("127.0.0.1", endpointUri.Host);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_StartsWithBaselineSecureProfileAndExposesOnlySecureEndpoints()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest(
            endpointProfile: RuntimeEndpointProfiles.BaselineSecure);
        var startResult = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            var endpoints = await DiscoverEndpointsAsync(startResult.EndpointUrl, CancellationToken.None);

            Assert.NotEmpty(endpoints);
            Assert.Contains(
                endpoints,
                endpoint => endpoint.SecurityMode == MessageSecurityMode.SignAndEncrypt &&
                    string.Equals(endpoint.SecurityPolicyUri, SecurityPolicies.Basic256Sha256, StringComparison.Ordinal));
            Assert.DoesNotContain(
                endpoints,
                endpoint => endpoint.SecurityMode == MessageSecurityMode.None ||
                    string.Equals(endpoint.SecurityPolicyUri, SecurityPolicies.None, StringComparison.Ordinal));
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_SecureVerificationFailureLeavesTheAdapterInactiveAndAllowsCleanSecureRestart()
    {
        var adapter = new UaNetRuntimeAdapter(new OneShotFailingSecureVerifier());
        var request = CreateStartRequest(
            endpointProfile: RuntimeEndpointProfiles.BaselineSecure);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.StartAsync(request, CancellationToken.None).AsTask());

        Assert.Contains("Configured secure verification failure", exception.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.StopAsync(CancellationToken.None).AsTask());

        var restartResult = await adapter.StartAsync(request, CancellationToken.None);

        try
        {
            var endpoints = await DiscoverEndpointsAsync(restartResult.EndpointUrl, CancellationToken.None);

            Assert.NotEmpty(endpoints);
            Assert.All(
                endpoints,
                endpoint =>
                {
                    Assert.Equal(MessageSecurityMode.SignAndEncrypt, endpoint.SecurityMode);
                    Assert.Equal(SecurityPolicies.Basic256Sha256, endpoint.SecurityPolicyUri);
                });
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_RejectsUnsupportedEndpointProfilesClearly()
    {
        var adapter = new UaNetRuntimeAdapter();
        var request = CreateStartRequest(
            endpointProfile: new RuntimeEndpointProfile(
                RuntimeEndpointProfileId.From("custom"),
                "Custom",
                RuntimeSecurityMode.None,
                RuntimeSecurityPolicy.None,
                true));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.StartAsync(request, CancellationToken.None).AsTask());

        Assert.Contains("custom", exception.Message);
    }

    private static RuntimeStartRequest CreateStartRequest(
        string displayName = "Source",
        RuntimeEndpointSettings? endpoint = null,
        RuntimeEndpointProfile? endpointProfile = null)
    {
        var projectionPlan = new RuntimeProjectionPlan(
        [
            new SimulationSignalProjection(
                SourceSignal,
                RuntimeNodeId.ForSignal(SourceSignal),
                displayName),
        ]);

        return new RuntimeStartRequest(
            UaNetRuntimeConstants.AdapterKey,
            new CompiledRuntimePlan(projectionPlan),
            endpoint ?? RuntimeEndpointSettings.Loopback(),
            endpointProfile ?? RuntimeEndpointProfiles.LocalDevelopment);
    }

    private static async Task<ISession> ConnectAsync(
        string endpointUrl,
        CancellationToken cancellationToken)
    {
        var configuration = await CreateClientConfigurationAsync(
            "UaNetClientTests",
            "StateKernel UA Runtime Test Client",
            "urn:statekernel:tests:ua-client",
            cancellationToken);

        Exception? lastException = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var selectedEndpoint = await CoreClientUtils.SelectEndpointAsync(
                    configuration,
                    endpointUrl,
                    false,
                    5000,
                    Telemetry,
                    cancellationToken);
                var configuredEndpoint = new ConfiguredEndpoint(
                    null,
                    selectedEndpoint,
                    EndpointConfiguration.Create(configuration));

                var sessionFactory = new DefaultSessionFactory(Telemetry);

                return await sessionFactory.CreateAsync(
                    configuration,
                    configuredEndpoint,
                    false,
                    "StateKernel Runtime Test Session",
                    60_000,
                    new UserIdentity(new AnonymousIdentityToken()),
                    null,
                    cancellationToken);
            }
            catch (Exception exception) when (attempt < 19)
            {
                lastException = exception;
                await Task.Delay(100, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("The UA test client could not connect to the runtime server.");
    }

    private static async Task<EndpointDescriptionCollection> DiscoverEndpointsAsync(
        string endpointUrl,
        CancellationToken cancellationToken)
    {
        var configuration = await CreateClientConfigurationAsync(
            "UaNetDiscoveryTests",
            "StateKernel UA Runtime Discovery Client",
            "urn:statekernel:tests:ua-discovery",
            cancellationToken);

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

        throw lastException ?? new InvalidOperationException("The UA discovery client could not enumerate runtime endpoints.");
    }

    private static async Task<ApplicationConfiguration> CreateClientConfigurationAsync(
        string configurationFolderName,
        string applicationName,
        string applicationUri,
        CancellationToken cancellationToken)
    {
        var configurationRootPath = Path.Combine(
            Path.GetTempPath(),
            "StateKernel",
            configurationFolderName,
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(configurationRootPath);

        var configuration = new ApplicationConfiguration(Telemetry)
        {
            ApplicationName = applicationName,
            ApplicationUri = applicationUri,
            ProductUri = applicationUri,
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "own"),
                    SubjectName = $"CN={applicationName}",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "issuer"),
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "trusted"),
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "rejected"),
                },
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 2048,
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas(),
            ClientConfiguration = new ClientConfiguration(),
            TraceConfiguration = new TraceConfiguration(),
            Extensions = new XmlElementCollection(),
        };

        await configuration.ValidateAsync(ApplicationType.Client, cancellationToken);

        var application = new ApplicationInstance(configuration, Telemetry)
        {
            ApplicationName = applicationName,
            ApplicationType = ApplicationType.Client,
        };

        await application.CheckApplicationInstanceCertificatesAsync(
            true,
            2048,
            cancellationToken);

        return configuration;
    }

    private static NodeId CreateNodeId(ISession session, RuntimeNodeId runtimeNodeId)
    {
        var expandedNodeId = ExpandedNodeId.Parse(
            $"nsu={UaNetRuntimeConstants.NamespaceUri};s={runtimeNodeId.Value}");
        return ExpandedNodeId.ToNodeId(expandedNodeId, session.NamespaceUris)!;
    }

    private sealed class OneShotFailingSecureVerifier : IUaNetEndpointSetVerifier
    {
        private readonly UaNetEndpointSetVerifier innerVerifier = new();
        private int secureVerificationCount;

        public async ValueTask VerifyAsync(
            ApplicationConfiguration configuration,
            string endpointUrl,
            RuntimeEndpointProfile endpointProfile,
            CancellationToken cancellationToken)
        {
            if (endpointProfile.Id == RuntimeEndpointProfiles.BaselineSecure.Id &&
                Interlocked.Increment(ref secureVerificationCount) == 1)
            {
                throw new InvalidOperationException("Configured secure verification failure.");
            }

            await innerVerifier.VerifyAsync(
                configuration,
                endpointUrl,
                endpointProfile,
                cancellationToken);
        }
    }
}
