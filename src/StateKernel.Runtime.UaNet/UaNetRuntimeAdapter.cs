using System.Net;
using System.Net.Sockets;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using StateKernel.Runtime.Abstractions;
using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.UaNet;

/// <summary>
/// Hosts a minimal UA-.NETStandard OPC UA server that exposes projected simulation signals as read-only nodes.
/// </summary>
public sealed class UaNetRuntimeAdapter : IRuntimeAdapter
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
    private readonly IUaNetEndpointSetVerifier endpointSetVerifier;
    private ApplicationInstance? application;
    private UaNetRuntimeServer? server;
    private CompiledRuntimePlan? compiledPlan;
    private string? endpointUrl;
    private string? configurationRootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="UaNetRuntimeAdapter" /> type.
    /// </summary>
    public UaNetRuntimeAdapter()
        : this(new UaNetEndpointSetVerifier())
    {
    }

    internal UaNetRuntimeAdapter(IUaNetEndpointSetVerifier endpointSetVerifier)
    {
        ArgumentNullException.ThrowIfNull(endpointSetVerifier);
        this.endpointSetVerifier = endpointSetVerifier;
    }

    /// <inheritdoc />
    public RuntimeAdapterDescriptor Descriptor => UaNetRuntimeAdapterCatalog.Default;

    /// <inheritdoc />
    public async ValueTask<RuntimeStartResult> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (server is not null)
        {
            throw new InvalidOperationException(
                "The UA .NET runtime adapter is already running.");
        }

        if (!string.Equals(request.AdapterKey, Descriptor.Key, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The UA .NET runtime adapter cannot start adapter key '{request.AdapterKey}'.");
        }

        if (request.EndpointProfile.RequiresLoopbackEndpoint &&
            !IsLoopbackHost(request.Endpoint.Host))
        {
            throw new InvalidOperationException(
                $"Runtime endpoint/profile '{request.EndpointProfile.Id}' requires a loopback endpoint host. Provided host: '{request.Endpoint.Host}'.");
        }

        var resolvedPort = request.Endpoint.Port == 0
            ? GetAvailablePort(request.Endpoint.Host)
            : request.Endpoint.Port;
        var resolvedEndpointUrl = $"opc.tcp://{request.Endpoint.Host}:{resolvedPort}{UaNetRuntimeConstants.EndpointPath}";
        var (configuration, startupRootPath) = await CreateApplicationConfigurationAsync(
            request.Endpoint.Host,
            resolvedEndpointUrl,
            request.EndpointProfile,
            cancellationToken);
        var serverInstance = new UaNetRuntimeServer(request.CompiledPlan.Bindings);
        var applicationInstance = new ApplicationInstance(configuration, Telemetry)
        {
            ApplicationName = "StateKernel Runtime UA .NET",
            ApplicationType = ApplicationType.Server,
        };

        try
        {
            await applicationInstance.CheckApplicationInstanceCertificatesAsync(
                true,
                2048,
                cancellationToken);
            await applicationInstance.StartAsync(serverInstance);
            await endpointSetVerifier.VerifyAsync(
                configuration,
                resolvedEndpointUrl,
                request.EndpointProfile,
                cancellationToken);

            application = applicationInstance;
            server = serverInstance;
            compiledPlan = request.CompiledPlan;
            endpointUrl = resolvedEndpointUrl;
            configurationRootPath = startupRootPath;

            return new RuntimeStartResult(resolvedEndpointUrl, request.CompiledPlan.Bindings.Count);
        }
        catch
        {
            await CleanupFailedStartAsync(applicationInstance, serverInstance, startupRootPath);
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask ApplyUpdatesAsync(
        IReadOnlyList<RuntimeValueUpdate> updates,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updates);
        cancellationToken.ThrowIfCancellationRequested();

        if (server is null || compiledPlan is null)
        {
            throw new InvalidOperationException(
                "The UA .NET runtime adapter must be started before updates can be applied.");
        }

        if (updates.Any(static update => update is null))
        {
            throw new ArgumentException(
                "Runtime update batches cannot contain null updates.",
                nameof(updates));
        }

        foreach (var update in updates)
        {
            _ = compiledPlan.GetRequiredBinding(update.SourceSignalId);
            server.ApplyUpdate(update.SourceSignalId, update.Value);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask<RuntimeStopResult> StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (server is null)
        {
            throw new InvalidOperationException(
                "The UA .NET runtime adapter is not currently running.");
        }

        try
        {
            if (application is not null)
            {
                await application.StopAsync();
            }
        }
        finally
        {
            server.Dispose();
            application = null;
            server = null;
            compiledPlan = null;
            endpointUrl = null;
            DeleteConfigurationRootPath(configurationRootPath);
            configurationRootPath = null;
        }

        await Task.CompletedTask;
        return new RuntimeStopResult(Descriptor.Key);
    }

    private static async Task<(ApplicationConfiguration Configuration, string ConfigurationRootPath)> CreateApplicationConfigurationAsync(
        string host,
        string resolvedEndpointUrl,
        RuntimeEndpointProfile endpointProfile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startupRootPath = Path.Combine(
            Path.GetTempPath(),
            "StateKernel",
            "UaNetRuntime",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(startupRootPath);

        try
        {
            var configuration = new ApplicationConfiguration
            {
                ApplicationName = "StateKernel Runtime UA .NET",
                ApplicationUri = $"urn:{host}:StateKernel:Runtime:UaNet",
                ProductUri = "urn:statekernel:runtime:ua-net",
                ApplicationType = ApplicationType.Server,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(startupRootPath, "pki", "own"),
                        SubjectName = "CN=StateKernel Runtime UA .NET",
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(startupRootPath, "pki", "issuer"),
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(startupRootPath, "pki", "trusted"),
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(startupRootPath, "pki", "rejected"),
                    },
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 2048,
                },
                TransportConfigurations = [],
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000,
                    MaxStringLength = 1_048_576,
                    MaxByteStringLength = 1_048_576,
                    MaxArrayLength = 65_535,
                    MaxMessageSize = 4 * 1024 * 1024,
                    MaxBufferSize = 65_535,
                    ChannelLifetime = 60_000,
                    SecurityTokenLifetime = 3_600_000,
                },
                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = [resolvedEndpointUrl],
                    SecurityPolicies =
                    [
                        CreateServerSecurityPolicy(endpointProfile),
                    ],
                    UserTokenPolicies =
                    [
                        new UserTokenPolicy(UserTokenType.Anonymous),
                    ],
                },
                TraceConfiguration = new TraceConfiguration(),
                DisableHiResClock = false,
                Extensions = new XmlElementCollection(),
            };

            await configuration.ValidateAsync(ApplicationType.Server, cancellationToken);
            return (configuration, startupRootPath);
        }
        catch
        {
            DeleteConfigurationRootPath(startupRootPath);
            throw;
        }
    }

    private static async Task CleanupFailedStartAsync(
        ApplicationInstance applicationInstance,
        UaNetRuntimeServer serverInstance,
        string configurationRootPath)
    {
        try
        {
            await applicationInstance.StopAsync();
        }
        catch
        {
        }
        finally
        {
            serverInstance.Dispose();
            DeleteConfigurationRootPath(configurationRootPath);
        }
    }

    private static ServerSecurityPolicy CreateServerSecurityPolicy(RuntimeEndpointProfile endpointProfile)
    {
        ArgumentNullException.ThrowIfNull(endpointProfile);

        return endpointProfile.Id.Value switch
        {
            "local-dev" => new ServerSecurityPolicy
            {
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
            },
            "baseline-secure" => new ServerSecurityPolicy
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
            },
            _ => throw new InvalidOperationException(
                $"The UA .NET runtime adapter does not support endpoint/profile id '{endpointProfile.Id}'."),
        };
    }

    private static int GetAvailablePort(string host)
    {
        var listenerAddress = IsLoopbackHost(host)
            ? IPAddress.Loopback
            : IPAddress.Any;
        var listener = new TcpListener(listenerAddress, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static bool IsLoopbackHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        var normalizedHost = host.Trim();

        if (string.Equals(normalizedHost, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedHost.Length > 2 &&
            normalizedHost[0] == '[' &&
            normalizedHost[^1] == ']')
        {
            normalizedHost = normalizedHost[1..^1];
        }

        return IPAddress.TryParse(normalizedHost, out var address) && IPAddress.IsLoopback(address);
    }

    private static void DeleteConfigurationRootPath(string? rootPath)
    {
        if (rootPath is null || !Directory.Exists(rootPath))
        {
            return;
        }

        try
        {
            Directory.Delete(rootPath, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class UaNetRuntimeServer : StandardServer
    {
        private readonly IReadOnlyList<RuntimeNodeBinding> bindings;

        public UaNetRuntimeServer(IReadOnlyList<RuntimeNodeBinding> bindings)
        {
            this.bindings = bindings;
        }

        public UaNetRuntimeNodeManager NodeManager { get; private set; } = null!;

        public void ApplyUpdate(SimulationSignalId signalId, double value)
        {
            NodeManager.ApplyUpdate(signalId, value);
        }

        protected override MasterNodeManager CreateMasterNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration)
        {
            NodeManager = new UaNetRuntimeNodeManager(server, configuration, bindings);
            return new MasterNodeManager(server, configuration, null, NodeManager);
        }
    }

    private sealed class UaNetRuntimeNodeManager : CustomNodeManager2
    {
        private readonly IReadOnlyList<RuntimeNodeBinding> bindings;
        private readonly Dictionary<SimulationSignalId, BaseDataVariableState> nodesBySignalId = [];
        private readonly Lock gate = new();

        public UaNetRuntimeNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration,
            IReadOnlyList<RuntimeNodeBinding> bindings)
            : base(server, configuration, UaNetRuntimeConstants.NamespaceUri)
        {
            this.bindings = bindings;
            SystemContext.NodeIdFactory = this;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            var signalsFolder = new FolderState(null)
            {
                NodeId = new NodeId(UaNetRuntimeConstants.SignalsFolderName, NamespaceIndex),
                BrowseName = new QualifiedName(UaNetRuntimeConstants.SignalsFolderName, NamespaceIndex),
                DisplayName = UaNetRuntimeConstants.SignalsFolderName,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                EventNotifier = EventNotifiers.None,
            };

            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
            {
                externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
            }

            signalsFolder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
            references.Add(
                new NodeStateReference(
                    ReferenceTypeIds.Organizes,
                    false,
                    signalsFolder.NodeId));

            AddPredefinedNode(SystemContext, signalsFolder);

            foreach (var binding in bindings)
            {
                var variable = new BaseDataVariableState(signalsFolder)
                {
                    NodeId = new NodeId(binding.TargetNodeId.Value, NamespaceIndex),
                    BrowseName = new QualifiedName(binding.TargetNodeId.Value, NamespaceIndex),
                    DisplayName = binding.DisplayName,
                    TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    DataType = DataTypeIds.Double,
                    ValueRank = ValueRanks.Scalar,
                    AccessLevel = AccessLevels.CurrentRead,
                    UserAccessLevel = AccessLevels.CurrentRead,
                    WriteMask = AttributeWriteMask.None,
                    UserWriteMask = AttributeWriteMask.None,
                    Value = 0.0d,
                    StatusCode = StatusCodes.Good,
                    Timestamp = DateTime.UtcNow,
                    Historizing = false,
                };

                signalsFolder.AddChild(variable);
                AddPredefinedNode(SystemContext, variable);
                nodesBySignalId.Add(binding.SourceSignalId, variable);
                variable.ClearChangeMasks(SystemContext, true);
            }

            signalsFolder.ClearChangeMasks(SystemContext, true);
        }

        public void ApplyUpdate(SimulationSignalId signalId, double value)
        {
            ArgumentNullException.ThrowIfNull(signalId);

            lock (gate)
            {
                if (!nodesBySignalId.TryGetValue(signalId, out var variable))
                {
                    throw new InvalidOperationException(
                        $"The UA .NET runtime adapter has no projected node for signal '{signalId}'.");
                }

                variable.Value = value;
                variable.StatusCode = StatusCodes.Good;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, false);
            }
        }

        public override NodeId New(ISystemContext context, NodeState node)
        {
            return new NodeId(Guid.NewGuid().ToString("N"), NamespaceIndex);
        }
    }
}
