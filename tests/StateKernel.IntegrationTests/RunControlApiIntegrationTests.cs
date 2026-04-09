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
using StateKernel.Runtime.UaNet;
using StateKernel.RuntimeHost.Execution;
using StateKernel.RuntimeHost.Hosting;
using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.IntegrationTests;

public sealed class RunControlApiIntegrationTests
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");

    [Fact]
    public async Task RunStatusEndpoint_ReturnsInactiveBeforeStart()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");

        Assert.NotNull(response);
        Assert.False(response.IsActive);
        Assert.Null(response.RunId);
        Assert.Null(response.RunDefinitionId);
        Assert.Null(response.AdapterKey);
        Assert.Null(response.EndpointUrl);
        Assert.Null(response.ProfileId);
        Assert.Null(response.ProjectedNodeCount);
        Assert.Null(response.LastCompletedTick);
        AssertNoRunFault(response);
    }

    [Fact]
    public async Task StartRunEndpoint_StartsTheRunAndReportsTheActualBoundEndpoint()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();

        try
        {
            var startResponse = await client.PostAsJsonAsync("/api/run/start", request);
            var runStatus = await startResponse.Content.ReadFromJsonAsync<RunStatusResponse>();

            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            Assert.NotNull(runStatus);
            Assert.True(runStatus.IsActive);
            Assert.NotNull(runStatus.RunId);
            Assert.Equal(request.RunDefinitionId, runStatus.RunDefinitionId);
            Assert.Equal(request.AdapterKey, runStatus.AdapterKey);
            Assert.Equal(request.ProfileId, runStatus.ProfileId);
            Assert.Equal(1, runStatus.ProjectedNodeCount);
            Assert.True(runStatus.LastCompletedTick >= 1);
            AssertNoRunFault(runStatus);

            var endpointUri = new Uri(runStatus.EndpointUrl!);

            Assert.True(endpointUri.Port > 0);

            var currentRunStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
            var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

            Assert.NotNull(currentRunStatus);
            Assert.Equal(runStatus.EndpointUrl, currentRunStatus.EndpointUrl);
            Assert.Equal(runStatus.ProfileId, currentRunStatus.ProfileId);
            Assert.Equal(runStatus.ProjectedNodeCount, currentRunStatus.ProjectedNodeCount);
            AssertNoRunFault(currentRunStatus);
            Assert.NotNull(runtimeStatus);
            Assert.True(runtimeStatus.IsActive);
            Assert.Equal(runStatus.EndpointUrl, runtimeStatus.EndpointUrl);
            AssertNoRuntimeFault(runtimeStatus);
        }
        finally
        {
            await StopRunIfActiveAsync(factory);
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsBadRequestForUnknownDefinitions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();
        request = new StartRunRequest
        {
            RunDefinitionId = "unknown-definition",
            AdapterKey = request.AdapterKey,
            ProfileId = request.ProfileId,
            EndpointHost = request.EndpointHost,
            EndpointPort = request.EndpointPort,
            NodeIdPrefix = request.NodeIdPrefix,
            ExposureChoices = request.ExposureChoices,
        };

        var response = await client.PostAsJsonAsync("/api/run/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid run start request", problem.Title);
        AssertNoRunFault(await client.GetFromJsonAsync<RunStatusResponse>("/api/run"));
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsBadRequestForDuplicateExposureChoices()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();
        request = new StartRunRequest
        {
            RunDefinitionId = request.RunDefinitionId,
            AdapterKey = request.AdapterKey,
            ProfileId = request.ProfileId,
            EndpointHost = request.EndpointHost,
            EndpointPort = request.EndpointPort,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest { SourceSignalId = "Source" },
                new StartRuntimeSignalExposureChoiceRequest { SourceSignalId = "Source" },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/run/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid run start request", problem.Title);
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsBadRequestForEmptyExposureChoices()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();
        request = new StartRunRequest
        {
            RunDefinitionId = request.RunDefinitionId,
            AdapterKey = request.AdapterKey,
            ProfileId = request.ProfileId,
            EndpointHost = request.EndpointHost,
            EndpointPort = request.EndpointPort,
            ExposureChoices = [],
        };

        var response = await client.PostAsJsonAsync("/api/run/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid run start request", problem.Title);
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsBadRequestForSignalsOutsideTheDefinitionSubset()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();
        request = new StartRunRequest
        {
            RunDefinitionId = request.RunDefinitionId,
            AdapterKey = request.AdapterKey,
            ProfileId = request.ProfileId,
            EndpointHost = request.EndpointHost,
            EndpointPort = request.EndpointPort,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest { SourceSignalId = "NotAllowed" },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/run/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid run start request", problem.Title);
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsBadRequestForDuplicateEffectiveRuntimeNodeIds()
    {
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<ISimulationRunDefinitionCatalog>();
            services.AddSingleton<ISimulationRunDefinitionCatalog>(
                _ => new SimulationRunDefinitionCatalog(
                [
                    new MultiSignalTestRunDefinition(),
                ]));
        });
        using var client = factory.CreateClient();
        var request = new StartRunRequest
        {
            RunDefinitionId = "multi-signal-test",
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

        var response = await client.PostAsJsonAsync("/api/run/start", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Invalid run start request", problem.Title);
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsConflictWhenRunIsAlreadyActive()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = CreateStartRequest();

        try
        {
            var firstResponse = await client.PostAsJsonAsync("/api/run/start", request);
            var secondResponse = await client.PostAsJsonAsync("/api/run/start", request);
            var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
            Assert.NotNull(problem);
            Assert.Equal("Run conflict", problem.Title);
        }
        finally
        {
            await StopRunIfActiveAsync(factory);
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StartRunEndpoint_ReturnsConflictWhenRuntimeIsAlreadyActive()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            var runtimeStartResponse = await client.PostAsJsonAsync(
                "/api/runtime/start",
                CreateRuntimeStartRequest());
            var runStartResponse = await client.PostAsJsonAsync("/api/run/start", CreateStartRequest());
            var problem = await runStartResponse.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.OK, runtimeStartResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, runStartResponse.StatusCode);
            Assert.NotNull(problem);
            Assert.Equal("Run conflict", problem.Title);
        }
        finally
        {
            await StopRunIfActiveAsync(factory);
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StopRunEndpoint_ReturnsConflictWhenRunIsInactive()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/run/stop", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Run conflict", problem.Title);
        AssertNoRunFault(await client.GetFromJsonAsync<RunStatusResponse>("/api/run"));
    }

    [Fact]
    public async Task StopRunEndpoint_LeavesBothRunAndRuntimeInactiveAndCleared()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            _ = await client.PostAsJsonAsync("/api/run/start", CreateStartRequest());

            var stopResponse = await client.PostAsync("/api/run/stop", null);
            var stopStatus = await stopResponse.Content.ReadFromJsonAsync<RunStatusResponse>();
            var currentRunStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
            var currentRuntimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

            Assert.Equal(HttpStatusCode.OK, stopResponse.StatusCode);
            Assert.NotNull(stopStatus);
            Assert.False(stopStatus.IsActive);
            Assert.Null(stopStatus.RunId);
            Assert.Null(stopStatus.RunDefinitionId);
            Assert.Null(stopStatus.EndpointUrl);
            Assert.Null(stopStatus.ProfileId);
            Assert.Null(stopStatus.ProjectedNodeCount);
            Assert.Null(stopStatus.LastCompletedTick);
            AssertNoRunFault(stopStatus);
            Assert.NotNull(currentRunStatus);
            Assert.False(currentRunStatus.IsActive);
            Assert.Null(currentRunStatus.EndpointUrl);
            Assert.Null(currentRunStatus.ProfileId);
            Assert.Null(currentRunStatus.ProjectedNodeCount);
            Assert.Null(currentRunStatus.LastCompletedTick);
            AssertNoRunFault(currentRunStatus);
            Assert.NotNull(currentRuntimeStatus);
            Assert.False(currentRuntimeStatus.IsActive);
            Assert.Null(currentRuntimeStatus.EndpointUrl);
            Assert.Null(currentRuntimeStatus.ProfileId);
            Assert.Null(currentRuntimeStatus.ProjectedNodeCount);
            AssertNoRuntimeFault(currentRuntimeStatus);
        }
        finally
        {
            await StopRunIfActiveAsync(factory);
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StartRunEndpoint_CanDriveTheRunFlowToARealOpcUaClientRead()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        try
        {
            var startResponse = await client.PostAsJsonAsync("/api/run/start", CreateStartRequest());
            var runStatus = await startResponse.Content.ReadFromJsonAsync<RunStatusResponse>();

            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            Assert.NotNull(runStatus);
            Assert.True(runStatus.IsActive);
            Assert.True(runStatus.LastCompletedTick >= 1);

            using var session = await ConnectAsync(runStatus.EndpointUrl!, CancellationToken.None);
            var value = await session.ReadValueAsync<double>(
                CreateNodeId(session, RuntimeNodeId.ForSignal(SourceSignal)),
                CancellationToken.None);

            Assert.Equal(5.0, value);

            var currentRunStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");

            Assert.NotNull(currentRunStatus);
            Assert.True(currentRunStatus.IsActive);
            Assert.True(currentRunStatus.LastCompletedTick >= 1);
            AssertNoRunFault(currentRunStatus);

            var stopResponse = await client.PostAsync("/api/run/stop", null);
            var stopStatus = await stopResponse.Content.ReadFromJsonAsync<RunStatusResponse>();
            var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

            Assert.Equal(HttpStatusCode.OK, stopResponse.StatusCode);
            Assert.NotNull(stopStatus);
            Assert.False(stopStatus.IsActive);
            Assert.Null(stopStatus.EndpointUrl);
            Assert.Null(stopStatus.ProfileId);
            Assert.Null(stopStatus.ProjectedNodeCount);
            Assert.Null(stopStatus.LastCompletedTick);
            AssertNoRunFault(stopStatus);
            Assert.NotNull(runtimeStatus);
            Assert.False(runtimeStatus.IsActive);
            Assert.Null(runtimeStatus.EndpointUrl);
            Assert.Null(runtimeStatus.ProfileId);
            Assert.Null(runtimeStatus.ProjectedNodeCount);
            AssertNoRuntimeFault(runtimeStatus);
        }
        finally
        {
            await StopRunIfActiveAsync(factory);
            await StopRuntimeIfRunningAsync(factory);
        }
    }

    [Fact]
    public async Task StartRunEndpoint_RuntimeAttachFailureReturns500AndRetainsFaultState()
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
            "/api/run/start",
            CreateStartRequest(adapterKey: "fake"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var runStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Run failure", problem.Title);
        Assert.NotNull(runStatus);
        Assert.False(runStatus.IsActive);
        Assert.Equal(SimulationRunFaultCodes.RuntimeAttachFailed, runStatus.FaultCode);
        Assert.NotNull(runStatus.FaultMessage);
        Assert.NotNull(runStatus.FaultOccurredAtUtc);
        Assert.NotNull(runtimeStatus);
        Assert.False(runtimeStatus.IsActive);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStartFailed, runtimeStatus.FaultCode);
    }

    [Fact]
    public async Task StartRunEndpoint_InitialStepFailureReturns500AndRetainsFaultState()
    {
        var fakeFactory = new ConfigurableFakeRuntimeAdapterFactory();
        fakeFactory.Adapter.ThrowOnApplyCallNumber = 1;
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(fakeFactory);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/run/start",
            CreateStartRequest(adapterKey: "fake"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var runStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Run failure", problem.Title);
        Assert.NotNull(runStatus);
        Assert.False(runStatus.IsActive);
        Assert.Equal(SimulationRunFaultCodes.InitialStepFailed, runStatus.FaultCode);
        Assert.NotNull(runtimeStatus);
        Assert.False(runtimeStatus.IsActive);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, runtimeStatus.FaultCode);
    }

    [Fact]
    public async Task LaterContinuousLoopFailureIsSurfacedOnlyThroughStatusReads()
    {
        var fakeFactory = new ConfigurableFakeRuntimeAdapterFactory();
        fakeFactory.Adapter.ThrowOnApplyCallNumber = 2;
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(fakeFactory);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/run/start",
            CreateStartRequest(adapterKey: "fake"));
        var startStatus = await response.Content.ReadFromJsonAsync<RunStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(startStatus);
        Assert.True(startStatus.IsActive);
        AssertNoRunFault(startStatus);

        await EventuallyAsync(
            async () =>
            {
                var runStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
                var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

                return runStatus is { IsActive: false, FaultCode: not null } &&
                    runtimeStatus is { IsActive: false, FaultCode: not null };
            },
            TimeSpan.FromSeconds(3));

        var finalRunStatus = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");
        var finalRuntimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.NotNull(finalRunStatus);
        Assert.False(finalRunStatus.IsActive);
        Assert.Equal(SimulationRunFaultCodes.ContinuousStepFailed, finalRunStatus.FaultCode);
        Assert.NotNull(finalRuntimeStatus);
        Assert.False(finalRuntimeStatus.IsActive);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, finalRuntimeStatus.FaultCode);
    }

    [Fact]
    public async Task RuntimeAndRunFaultClearingRemainIndependentAcrossLifecycleDomains()
    {
        var fakeFactory = new ConfigurableFakeRuntimeAdapterFactory();
        fakeFactory.Adapter.ThrowOnApplyCallNumber = 1;
        using var factory = CreateFactory(services =>
        {
            services.RemoveAll<IRuntimeAdapterFactory>();
            services.AddSingleton<IRuntimeAdapterFactory>(fakeFactory);
        });
        using var client = factory.CreateClient();

        _ = await client.PostAsJsonAsync(
            "/api/run/start",
            CreateStartRequest(adapterKey: "fake"));

        fakeFactory.Adapter.ThrowOnApplyCallNumber = null;

        var runtimeStartResponse = await client.PostAsJsonAsync(
            "/api/runtime/start",
            CreateRuntimeStartRequest(adapterKey: "fake"));
        var runtimeStatusAfterRuntimeStart = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");
        var runStatusAfterRuntimeStart = await client.GetFromJsonAsync<RunStatusResponse>("/api/run");

        Assert.Equal(HttpStatusCode.OK, runtimeStartResponse.StatusCode);
        Assert.NotNull(runtimeStatusAfterRuntimeStart);
        Assert.True(runtimeStatusAfterRuntimeStart.IsActive);
        AssertNoRuntimeFault(runtimeStatusAfterRuntimeStart);
        Assert.NotNull(runStatusAfterRuntimeStart);
        Assert.False(runStatusAfterRuntimeStart.IsActive);
        Assert.Equal(SimulationRunFaultCodes.InitialStepFailed, runStatusAfterRuntimeStart.FaultCode);

        _ = await client.PostAsync("/api/runtime/stop", null);

        var restartResponse = await client.PostAsJsonAsync(
            "/api/run/start",
            CreateStartRequest(adapterKey: "fake"));
        var runStatus = await restartResponse.Content.ReadFromJsonAsync<RunStatusResponse>();
        var runtimeStatus = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime");

        Assert.Equal(HttpStatusCode.OK, restartResponse.StatusCode);
        Assert.NotNull(runStatus);
        Assert.True(runStatus.IsActive);
        AssertNoRunFault(runStatus);
        Assert.NotNull(runtimeStatus);
        Assert.True(runtimeStatus.IsActive);
        AssertNoRuntimeFault(runtimeStatus);
    }

    private static WebApplicationFactory<Program> CreateFactory(Action<IServiceCollection>? configureServices = null)
    {
        var factory = new WebApplicationFactory<Program>();

        return configureServices is null
            ? factory
            : factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(configureServices));
    }

    private static StartRunRequest CreateStartRequest(string adapterKey = UaNetRuntimeConstants.AdapterKey)
    {
        return new StartRunRequest
        {
            RunDefinitionId = "baseline-constant-source",
            AdapterKey = adapterKey,
            ProfileId = RuntimeEndpointProfiles.LocalDevelopment.Id.Value,
            EndpointHost = "127.0.0.1",
            EndpointPort = 0,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest
                {
                    SourceSignalId = SourceSignal.Value,
                },
            ],
        };
    }

    private static StartRuntimeRequest CreateRuntimeStartRequest(string adapterKey = UaNetRuntimeConstants.AdapterKey)
    {
        return new StartRuntimeRequest
        {
            AdapterKey = adapterKey,
            ProfileId = RuntimeEndpointProfiles.LocalDevelopment.Id.Value,
            EndpointHost = "127.0.0.1",
            EndpointPort = 0,
            ExposureChoices =
            [
                new StartRuntimeSignalExposureChoiceRequest
                {
                    SourceSignalId = SourceSignal.Value,
                },
            ],
        };
    }

    private static async Task StopRunIfActiveAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var executionOrchestrator = scope.ServiceProvider.GetRequiredService<SimulationExecutionOrchestrator>();

        if (executionOrchestrator.GetStatus().IsActive)
        {
            _ = await executionOrchestrator.StopAsync(CancellationToken.None);
        }
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
            "RunControlApiIntegrationClientTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(configurationRootPath);

        var configuration = new ApplicationConfiguration(Telemetry)
        {
            ApplicationName = "StateKernel Run Control API Integration Client",
            ApplicationUri = "urn:statekernel:tests:run-control-api:ua-client",
            ProductUri = "urn:statekernel:tests:run-control-api:ua-client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(configurationRootPath, "pki", "own"),
                    SubjectName = "CN=StateKernel Run Control API Integration Client",
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
            ApplicationName = "StateKernel Run Control API Integration Client",
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
                    "StateKernel Run Control API Session",
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

    private static async Task EventuallyAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.True(await condition());
    }

    private static void AssertNoRunFault(RunStatusResponse? status)
    {
        Assert.NotNull(status);
        Assert.Null(status.FaultCode);
        Assert.Null(status.FaultMessage);
        Assert.Null(status.FaultOccurredAtUtc);
    }

    private static void AssertNoRuntimeFault(RuntimeStatusResponse? status)
    {
        Assert.NotNull(status);
        Assert.Null(status.FaultCode);
        Assert.Null(status.FaultMessage);
        Assert.Null(status.FaultOccurredAtUtc);
    }

    private sealed class MultiSignalTestRunDefinition : ISimulationExecutableRunDefinition
    {
        private static readonly SimulationSignalId AlphaSignal = SimulationSignalId.From("Alpha");
        private static readonly SimulationSignalId BetaSignal = SimulationSignalId.From("Beta");
        private static readonly SimulationMode RunMode = SimulationMode.From("Run");

        public IReadOnlyList<SimulationSignalId> ExposableSignals { get; } = Array.AsReadOnly(
        [
            AlphaSignal,
            BetaSignal,
        ]);

        public SimulationRunDefinitionId Id { get; } = SimulationRunDefinitionId.From("multi-signal-test");

        public string DisplayName => "Multi Signal Test";

        public SimulationExecutionSettings DefaultExecutionSettings { get; } =
            SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(50));

        public SimulationRunComponents CreateExecutionComponents()
        {
            var signalStore = new SimulationSignalValueStore();
            var outputSink = new BehaviorExecutionRecorder();
            var modeController = new SimulationModeController(RunMode);
            var context = SimulationContext.CreateDeterministic(
                new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8),
                StateKernel.Simulation.Seed.SimulationSeed.FromInt32(42));
            var plan = new SimulationSchedulerPlan(
            [
                new BehaviorScheduledWork(
                    "alpha",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(1.0),
                    new AlwaysActivePolicy(),
                    modeController,
                    signalStore,
                    outputSink,
                    AlphaSignal),
                new BehaviorScheduledWork(
                    "beta",
                    ExecutionCadence.EveryTick,
                    1,
                    new ConstantBehavior(2.0),
                    new AlwaysActivePolicy(),
                    modeController,
                    signalStore,
                    outputSink,
                    BetaSignal),
            ]);

            return new SimulationRunComponents(
                new DeterministicSimulationScheduler(context, plan),
                signalStore);
        }
    }
}
