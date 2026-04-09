using System.Net;
using System.Net.Http.Json;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using StateKernel.ControlApi;
using StateKernel.ControlApi.Contracts.Run;
using StateKernel.ControlApi.Contracts.Runtime;
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

public sealed class RuntimeControlApiIntegrationTests
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public async Task RuntimeStatusEndpoint_ReturnsInactiveBeforeStart()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.NotNull(response);
        Assert.False(response.IsActive);
        Assert.Null(response.AdapterKey);
        Assert.Null(response.EndpointUrl);
        Assert.Null(response.ProfileId);
        Assert.Null(response.ProjectedNodeCount);
        AssertNoRuntimeFault(response);
    }

    [Fact]
    public async Task StartRuntimeEndpoint_StartsRuntimeAndReportsTheActualBoundEndpoint()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();

        try
        {
            var startResponse = await client.PostAsJsonAsync("/api/runtime/start", request);
            var runtimeStatus = await startResponse.Content.ReadFromJsonAsync<RuntimeStatusResponse>();

            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            Assert.NotNull(runtimeStatus);
            Assert.True(runtimeStatus.IsActive);
            Assert.Equal(UaNetRuntimeConstants.AdapterKey, runtimeStatus.AdapterKey);
            Assert.Equal(RuntimeEndpointProfiles.LocalDevelopment.Id.Value, runtimeStatus.ProfileId);
            Assert.Equal(1, runtimeStatus.ProjectedNodeCount);
            AssertNoRuntimeFault(runtimeStatus);

            var startEndpoint = new Uri(runtimeStatus.EndpointUrl!);

            Assert.True(startEndpoint.Port > 0);

            var statusResponse = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

            Assert.NotNull(statusResponse);
            Assert.True(statusResponse.IsActive);
            Assert.Equal(runtimeStatus.EndpointUrl, statusResponse.EndpointUrl);
            Assert.Equal(runtimeStatus.ProfileId, statusResponse.ProfileId);
            Assert.Equal(runtimeStatus.ProjectedNodeCount, statusResponse.ProjectedNodeCount);
            AssertNoRuntimeFault(statusResponse);
        }
        finally
        {
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StopRuntimeEndpoint_ReturnsInactiveStatusAfterStop()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            _ = await client.PostAsJsonAsync("/api/runtime/start", CreateStartRequest());

            var stopResponse = await client.PostAsync("/api/runtime/stop", null);
            var runtimeStatus = await stopResponse.Content.ReadFromJsonAsync<RuntimeStatusResponse>();

            Assert.Equal(HttpStatusCode.OK, stopResponse.StatusCode);
            Assert.NotNull(runtimeStatus);
            Assert.False(runtimeStatus.IsActive);
            Assert.Null(runtimeStatus.AdapterKey);
            Assert.Null(runtimeStatus.EndpointUrl);
            Assert.Null(runtimeStatus.ProfileId);
            Assert.Null(runtimeStatus.ProjectedNodeCount);
            AssertNoRuntimeFault(runtimeStatus);
        }
        finally
        {
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StartRuntimeEndpoint_ReturnsBadRequestForUnknownProfiles()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();
        request = new StartRuntimeRequest
        {
            AdapterKey = request.AdapterKey,
            ProfileId = "unknown-profile",
            EndpointHost = request.EndpointHost,
            EndpointPort = request.EndpointPort,
            NodeIdPrefix = request.NodeIdPrefix,
            ExposureChoices = request.ExposureChoices,
        };

        var response = await client.PostAsJsonAsync("/api/runtime/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid runtime start request", problem.Title);

        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.NotNull(runtimeStatus);
        Assert.False(runtimeStatus.IsActive);
        AssertNoRuntimeFault(runtimeStatus);
    }

    [Fact]
    public async Task StartRuntimeEndpoint_ReturnsBadRequestForDuplicateExposureChoices()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new StartRuntimeRequest
        {
            AdapterKey = UaNetRuntimeConstants.AdapterKey,
            ProfileId = RuntimeEndpointProfiles.LocalDevelopment.Id.Value,
            EndpointHost = "127.0.0.1",
            EndpointPort = 0,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest { SourceSignalId = "Source" },
                new StartRuntimeSignalExposureChoiceRequest { SourceSignalId = "Source" },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/runtime/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid runtime start request", problem.Title);
    }

    [Fact]
    public async Task StartRuntimeEndpoint_ReturnsBadRequestForDuplicateEffectiveRuntimeNodeIds()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new StartRuntimeRequest
        {
            AdapterKey = UaNetRuntimeConstants.AdapterKey,
            ProfileId = RuntimeEndpointProfiles.LocalDevelopment.Id.Value,
            EndpointHost = "127.0.0.1",
            EndpointPort = 0,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest { SourceSignalId = "Alpha" },
                new StartRuntimeSignalExposureChoiceRequest
                {
                    SourceSignalId = "Beta",
                    TargetNodeIdOverride = "Signals/Alpha",
                },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/runtime/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid runtime start request", problem.Title);
    }

    [Fact]
    public async Task StartRuntimeEndpoint_ReturnsConflictWhenRuntimeIsAlreadyActive()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();

        try
        {
            var firstResponse = await client.PostAsJsonAsync("/api/runtime/start", request);
            var secondResponse = await client.PostAsJsonAsync("/api/runtime/start", request);
            var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Runtime conflict", problem.Title);
        AssertNoRuntimeFault(await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime"));
        }
        finally
        {
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StopRuntimeEndpoint_ReturnsConflictWhenRuntimeIsInactive()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/runtime/stop", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Runtime conflict", problem.Title);

        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.NotNull(runtimeStatus);
        Assert.False(runtimeStatus.IsActive);
        AssertNoRuntimeFault(runtimeStatus);
    }

    [Fact]
    public async Task StartRuntimeEndpoint_InternalStartFailureReturns500AndRetainsFaultState()
    {
        var fakeFactory = new ConfigurableFakeRuntimeAdapterFactory();
        fakeFactory.Adapter.ThrowOnStartCallNumber = 1;
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(fakeFactory);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/runtime/start",
            CreateStartRequest(adapterKey: "fake"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Runtime failure", problem.Title);
        Assert.NotNull(runtimeStatus);
        Assert.False(runtimeStatus.IsActive);
        Assert.Null(runtimeStatus.AdapterKey);
        Assert.Null(runtimeStatus.EndpointUrl);
        Assert.Null(runtimeStatus.ProfileId);
        Assert.Null(runtimeStatus.ProjectedNodeCount);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStartFailed, runtimeStatus.FaultCode);
        Assert.NotNull(runtimeStatus.FaultMessage);
        Assert.NotNull(runtimeStatus.FaultOccurredAtUtc);
    }

    [Fact]
    public async Task StopRuntimeEndpoint_InternalStopFailureReturns500AndRetainsFaultState()
    {
        var fakeFactory = new ConfigurableFakeRuntimeAdapterFactory();
        fakeFactory.Adapter.ThrowOnStopCallNumber = 1;
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(fakeFactory);
        });
        using var client = factory.CreateClient();

        _ = await client.PostAsJsonAsync(
            "/api/runtime/start",
            CreateStartRequest(adapterKey: "fake"));

        var response = await client.PostAsync("/api/runtime/stop", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Runtime failure", problem.Title);
        Assert.NotNull(runtimeStatus);
        Assert.False(runtimeStatus.IsActive);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStopFailed, runtimeStatus.FaultCode);
        Assert.NotNull(runtimeStatus.FaultMessage);
        Assert.NotNull(runtimeStatus.FaultOccurredAtUtc);
    }

    [Fact]
    public async Task SuccessfulRuntimeStart_ClearsRetainedFaultState()
    {
        var fakeFactory = new ConfigurableFakeRuntimeAdapterFactory();
        fakeFactory.Adapter.ThrowOnStartCallNumber = 1;
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(fakeFactory);
        });
        using var client = factory.CreateClient();

        _ = await client.PostAsJsonAsync(
            "/api/runtime/start",
            CreateStartRequest(adapterKey: "fake"));

        fakeFactory.Adapter.ThrowOnStartCallNumber = null;

        var restartResponse = await client.PostAsJsonAsync(
            "/api/runtime/start",
            CreateStartRequest(adapterKey: "fake"));
        var runtimeStatus = await restartResponse.Content.ReadFromJsonAsync<RuntimeStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, restartResponse.StatusCode);
        Assert.NotNull(runtimeStatus);
        Assert.True(runtimeStatus.IsActive);
        AssertNoRuntimeFault(runtimeStatus);
    }

    [Fact]
    public async Task RuntimeLifecycle_LeavesRunStatusSeparateAndAccurate()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            var startResponse = await client.PostAsJsonAsync("/api/runtime/start", CreateStartRequest());
            var runStatusWhileRuntimeActive = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
            var stopResponse = await client.PostAsync("/api/runtime/stop", null);
            var runStatusAfterStop = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");

            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            Assert.NotNull(runStatusWhileRuntimeActive);
            Assert.False(runStatusWhileRuntimeActive.IsActive);
            AssertNoRunFault(runStatusWhileRuntimeActive);
            Assert.Equal(HttpStatusCode.OK, stopResponse.StatusCode);
            Assert.NotNull(runStatusAfterStop);
            Assert.False(runStatusAfterStop.IsActive);
            AssertNoRunFault(runStatusAfterStop);
        }
        finally
        {
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StartRuntimeEndpoint_ReportsSecureProfileStatusWithoutFaultState()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            var response = await client.PostAsJsonAsync(
                "/api/runtime/start",
                CreateStartRequest(profileId: RuntimeEndpointProfiles.BaselineSecure.Id.Value));
            var runtimeStatus = await response.Content.ReadFromJsonAsync<RuntimeStatusResponse>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(runtimeStatus);
            Assert.True(runtimeStatus.IsActive);
            Assert.Equal(RuntimeEndpointProfiles.BaselineSecure.Id.Value, runtimeStatus.ProfileId);
            AssertNoRuntimeFault(runtimeStatus);
        }
        finally
        {
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task SecureVerificationFailureLeavesInactiveStatusAndNoStaleEndpointThenAllowsCleanRestart()
    {
        var adapter = new UaNetRuntimeAdapter(new OneShotFailingSecureVerifier());
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(
                new SingleAdapterRuntimeFactory(adapter, UaNetRuntimeAdapterCatalog.Default));
        });
        using var client = factory.CreateClient();
        var fixedPort = GetAvailableLoopbackPort();
        var request = CreateStartRequest(
            profileId: RuntimeEndpointProfiles.BaselineSecure.Id.Value,
            endpointPort: fixedPort);
        var expectedEndpointUrl = $"opc.tcp://127.0.0.1:{fixedPort}{UaNetRuntimeConstants.EndpointPath}";

        var failedResponse = await client.PostAsJsonAsync("/api/runtime/start", request);
        var failedProblem = await failedResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        var failedStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.Equal(HttpStatusCode.InternalServerError, failedResponse.StatusCode);
        Assert.NotNull(failedProblem);
        Assert.Equal("Runtime failure", failedProblem.Title);
        Assert.NotNull(failedStatus);
        Assert.False(failedStatus.IsActive);
        Assert.Null(failedStatus.AdapterKey);
        Assert.Null(failedStatus.EndpointUrl);
        Assert.Null(failedStatus.ProfileId);
        Assert.Null(failedStatus.ProjectedNodeCount);
        Assert.Equal(RuntimeHostFaultCodes.SecureStartupFailed, failedStatus.FaultCode);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            ConnectAsync(expectedEndpointUrl, CancellationToken.None));

        var restartResponse = await client.PostAsJsonAsync("/api/runtime/start", request);
        var restartStatus = await restartResponse.Content.ReadFromJsonAsync<RuntimeStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, restartResponse.StatusCode);
        Assert.NotNull(restartStatus);
        Assert.True(restartStatus.IsActive);
        Assert.Equal(expectedEndpointUrl, restartStatus.EndpointUrl);
        Assert.Equal(RuntimeEndpointProfiles.BaselineSecure.Id.Value, restartStatus.ProfileId);
        AssertNoRuntimeFault(restartStatus);

        await StopRuntimeIfRunningAsync(factory);
    }

    [Fact]
    public async Task StartRuntimeEndpoint_CanDriveTheExistingRuntimeFlowToARealOpcUaClientRead()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();

        try
        {
            var startResponse = await client.PostAsJsonAsync("/api/runtime/start", request);
            var runtimeStatus = await startResponse.Content.ReadFromJsonAsync<RuntimeStatusResponse>();

            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            Assert.NotNull(runtimeStatus);

            using var scope = factory.Services.CreateScope();
            var runtimeHostService = scope.ServiceProvider.GetRequiredService<RuntimeHostService>();
            var signalStore = new SimulationSignalValueStore();
            var outputSink = new BehaviorExecutionRecorder();
            var modeController = new SimulationModeController(RunMode);
            var scheduler = CreateScheduler(signalStore, outputSink, modeController);
            var compositionResult = ComposeRequest(request);
            var frame = scheduler.RunNextTick();
            var committedSnapshot = signalStore.GetCommittedSnapshotForTick(frame.Tick.Advance(TickInterval));
            var updates = RuntimeValueUpdateProjector.CreateUpdates(
                compositionResult.CompiledRuntimePlan,
                committedSnapshot);

            await runtimeHostService.ApplyUpdatesAsync(updates, CancellationToken.None);

            using var session = await ConnectAsync(runtimeStatus.EndpointUrl!, CancellationToken.None);
            var value = await session.ReadValueAsync<double>(
                CreateNodeId(session, compositionResult.ProjectionPlan.Projections[0].TargetNodeId),
                CancellationToken.None);

            Assert.Equal(5.0, value);
            Assert.Single(outputSink.Records);
            Assert.Equal(SourceSignal, committedSnapshot.GetRequiredValue(SourceSignal).SignalId);
        }
        finally
        {
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(Action<IServiceCollection>? configureServices = null)
    {
        var factory = new WebApplicationFactory<Program>();

        return configureServices is null
            ? factory
            : factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(configureServices));
    }

    private static StartRuntimeRequest CreateStartRequest(
        string adapterKey = UaNetRuntimeConstants.AdapterKey,
        string profileId = null!,
        int endpointPort = 0)
    {
        return new StartRuntimeRequest
        {
            AdapterKey = adapterKey,
            ProfileId = profileId ?? RuntimeEndpointProfiles.LocalDevelopment.Id.Value,
            EndpointHost = "127.0.0.1",
            EndpointPort = endpointPort,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest
                {
                    SourceSignalId = SourceSignal.Value,
                },
            ],
        };
    }

    private static RuntimeCompositionResult ComposeRequest(StartRuntimeRequest request)
    {
        var exposureChoices = request.ExposureChoices!
            .Select(static choice =>
                new SimulationSignalExposureChoice(
                    SimulationSignalId.From(choice.SourceSignalId!),
                    choice.TargetNodeIdOverride is null
                        ? null
                        : RuntimeNodeId.From(choice.TargetNodeIdOverride),
                    choice.DisplayNameOverride))
            .ToArray();
        var selectionResult = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(exposureChoices));
        var compositionDefaults = request.NodeIdPrefix is null
            ? RuntimeCompositionDefaults.Baseline
            : new RuntimeCompositionDefaults(request.NodeIdPrefix);

        return RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                request.AdapterKey!,
                selectionResult.SignalSelections,
                compositionDefaults));
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

    private static async Task StopRuntimeIfRunningAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var runtimeHostService = scope.ServiceProvider.GetRequiredService<RuntimeHostService>();

        if (runtimeHostService.GetStatus().IsRunning)
        {
            _ = await runtimeHostService.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<ISession> ConnectAsync(
        string endpointUrl,
        CancellationToken cancellationToken)
    {
        var configurationRootPath = Path.Combine(
            Path.GetTempPath(),
            "StateKernel",
            "ControlApiRuntimeIntegrationClientTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(configurationRootPath);

        var configuration = new ApplicationConfiguration(Telemetry)
        {
            ApplicationName = "StateKernel Control API Runtime Integration Client",
            ApplicationUri = "urn:statekernel:tests:control-api:ua-client",
            ProductUri = "urn:statekernel:tests:control-api:ua-client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "own"),
                    SubjectName = "CN=StateKernel Control API Runtime Integration Client",
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
            ApplicationName = "StateKernel Control API Runtime Integration Client",
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
                    "StateKernel Control API Runtime Session",
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

    private static int GetAvailableLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void AssertNoRuntimeFault(RuntimeStatusResponse? status)
    {
        Assert.NotNull(status);
        Assert.Null(status.FaultCode);
        Assert.Null(status.FaultMessage);
        Assert.Null(status.FaultOccurredAtUtc);
    }

    private static void AssertNoRunFault(RunStatusResponse? status)
    {
        Assert.NotNull(status);
        Assert.Null(status.FaultCode);
        Assert.Null(status.FaultMessage);
        Assert.Null(status.FaultOccurredAtUtc);
    }

    private sealed class SingleAdapterRuntimeFactory : IRuntimeAdapterFactory
    {
        private readonly IRuntimeAdapter adapter;

        public SingleAdapterRuntimeFactory(
            IRuntimeAdapter adapter,
            RuntimeAdapterDescriptor descriptor)
        {
            this.adapter = adapter;
            Descriptor = descriptor;
        }

        public RuntimeAdapterDescriptor Descriptor { get; }

        public IRuntimeAdapter CreateAdapter()
        {
            return adapter;
        }
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
