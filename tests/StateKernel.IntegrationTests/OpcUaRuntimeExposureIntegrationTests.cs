using System.Xml;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using StateKernel.Runtime.Abstractions;
using StateKernel.Runtime.Abstractions.Composition;
using StateKernel.Runtime.Abstractions.Selection;
using StateKernel.Runtime.UaNet;
using StateKernel.RuntimeHost.Hosting;
using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.IntegrationTests;

public sealed class OpcUaRuntimeExposureIntegrationTests
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public async Task DeterministicSimulationSignals_AreExposedToARealOpcUaClientThroughTheRuntimeHost()
    {
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var outputSink = new BehaviorExecutionRecorder();
        var scheduler = CreateScheduler(
            signalStore,
            outputSink,
            modeController);
        var selectionResult = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(
            [
                new SimulationSignalExposureChoice(SourceSignal),
            ]));
        var compositionResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                UaNetRuntimeConstants.AdapterKey,
                selectionResult.SignalSelections,
                RuntimeCompositionDefaults.Baseline));
        var compiledPlan = compositionResult.CompiledRuntimePlan;
        var host = new RuntimeHostService([new UaNetRuntimeAdapterFactory()]);
        var startResult = await host.StartAsync(
            new RuntimeStartRequest(
                UaNetRuntimeConstants.AdapterKey,
                compiledPlan,
                RuntimeEndpointSettings.Loopback(),
                RuntimeEndpointProfiles.LocalDevelopment),
            CancellationToken.None);

        try
        {
            var frame = scheduler.RunNextTick();
            var committedSnapshot = signalStore.GetCommittedSnapshotForTick(frame.Tick.Advance(TickInterval));
            var updates = RuntimeValueUpdateProjector.CreateUpdates(compiledPlan, committedSnapshot);

            await host.ApplyUpdatesAsync(updates, CancellationToken.None);

            using var session = await ConnectAsync(startResult.EndpointUrl, CancellationToken.None);
            var value = await session.ReadValueAsync<double>(
                CreateNodeId(session, compositionResult.ProjectionPlan.Projections[0].TargetNodeId),
                CancellationToken.None);

            Assert.Equal(5.0, value);
            Assert.Single(outputSink.Records);
            Assert.Equal(SourceSignal, committedSnapshot.GetRequiredValue(SourceSignal).SignalId);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }

    private static DeterministicSimulationScheduler CreateScheduler(
        SimulationSignalValueStore signalStore,
        BehaviorExecutionRecorder outputSink,
        SimulationModeController modeController)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TickInterval, 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(42));
        var plan = new SimulationSchedulerPlan(
        [
            new BehaviorScheduledWork(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                modeController,
                signalStore,
                outputSink,
                SourceSignal),
        ]);

        return new DeterministicSimulationScheduler(context, plan);
    }

    private static async Task<ISession> ConnectAsync(
        string endpointUrl,
        CancellationToken cancellationToken)
    {
        var configurationRootPath = Path.Combine(
            Path.GetTempPath(),
            "StateKernel",
            "UaNetIntegrationClientTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(configurationRootPath);

        var configuration = new ApplicationConfiguration(Telemetry)
        {
            ApplicationName = "StateKernel UA Runtime Integration Client",
            ApplicationUri = "urn:statekernel:tests:integration:ua-client",
            ProductUri = "urn:statekernel:tests:integration:ua-client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "own"),
                    SubjectName = "CN=StateKernel UA Runtime Integration Client",
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
            ApplicationName = "StateKernel UA Runtime Integration Client",
            ApplicationType = ApplicationType.Client,
        };

        await application.CheckApplicationInstanceCertificatesAsync(
            true,
            2048,
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
                    "StateKernel Runtime Integration Session",
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

        throw lastException ?? new InvalidOperationException("The UA integration test client could not connect to the runtime server.");
    }

    private static NodeId CreateNodeId(ISession session, RuntimeNodeId runtimeNodeId)
    {
        var expandedNodeId = ExpandedNodeId.Parse(
            $"nsu={UaNetRuntimeConstants.NamespaceUri};s={runtimeNodeId.Value}");
        return ExpandedNodeId.ToNodeId(expandedNodeId, session.NamespaceUris)!;
    }
}
