using StateKernel.Runtime.Abstractions;
using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions.Tests;

public sealed class RuntimeEndpointProfileTests
{
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");

    [Fact]
    public void RuntimeEndpointProfileId_FromRejectsNullEmptyAndWhitespace()
    {
        Assert.ThrowsAny<ArgumentException>(() => RuntimeEndpointProfileId.From(null!));
        Assert.ThrowsAny<ArgumentException>(() => RuntimeEndpointProfileId.From(string.Empty));
        Assert.ThrowsAny<ArgumentException>(() => RuntimeEndpointProfileId.From("   "));
    }

    [Fact]
    public void RuntimeEndpointProfiles_ExposeTheBoundedBaselineProfilesDeterministically()
    {
        Assert.Collection(
            RuntimeEndpointProfiles.All,
            profile => Assert.Equal("local-dev", profile.Id.Value),
            profile => Assert.Equal("baseline-secure", profile.Id.Value));

        Assert.Same(
            RuntimeEndpointProfiles.BaselineSecure,
            RuntimeEndpointProfiles.GetRequired(RuntimeEndpointProfileId.From("baseline-secure")));
    }

    [Fact]
    public void RuntimeStartRequests_RequireAnEndpointProfile()
    {
        var compiledPlan = new CompiledRuntimePlan(
            new RuntimeProjectionPlan(
            [
                new SimulationSignalProjection(SourceSignal, RuntimeNodeId.ForSignal(SourceSignal)),
            ]));

        Assert.Throws<ArgumentNullException>(() =>
            new RuntimeStartRequest(
                "ua-net",
                compiledPlan,
                RuntimeEndpointSettings.Loopback(),
                null!));
    }

    [Fact]
    public void RuntimeAdapterDescriptors_OrderSupportedEndpointProfilesDeterministically()
    {
        var descriptor = new RuntimeAdapterDescriptor(
            "ua-net",
            "UA",
            new RuntimeCapabilitySet([RuntimeCapability.ReadOnlyValueExposure, RuntimeCapability.SecurityProfiles]),
            [RuntimeEndpointProfiles.LocalDevelopment.Id, RuntimeEndpointProfiles.BaselineSecure.Id]);

        Assert.Collection(
            descriptor.SupportedEndpointProfiles,
            profileId => Assert.Equal("baseline-secure", profileId.Value),
            profileId => Assert.Equal("local-dev", profileId.Value));
    }

    [Fact]
    public void RuntimeAdapterDescriptors_RejectDuplicateSupportedEndpointProfiles()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeAdapterDescriptor(
                "ua-net",
                "UA",
                RuntimeCapabilitySet.Empty,
                [RuntimeEndpointProfiles.LocalDevelopment.Id, RuntimeEndpointProfiles.LocalDevelopment.Id]));

        Assert.Contains(RuntimeEndpointProfiles.LocalDevelopment.Id.Value, exception.Message);
    }
}
