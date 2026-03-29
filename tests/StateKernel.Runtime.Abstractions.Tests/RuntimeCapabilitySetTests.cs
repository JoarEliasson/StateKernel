using StateKernel.Runtime.Abstractions;

namespace StateKernel.Runtime.Abstractions.Tests;

public sealed class RuntimeCapabilitySetTests
{
    [Fact]
    public void Constructor_DeduplicatesCapabilities()
    {
        var capabilities = new RuntimeCapabilitySet(
            new[]
            {
                RuntimeCapability.SecurityProfiles,
                RuntimeCapability.SecurityProfiles,
                RuntimeCapability.BenchmarkExecution,
            });

        Assert.Equal(2, capabilities.Count);
    }

    [Fact]
    public void Supports_ReturnsTrueOnlyForCapabilitiesInTheSet()
    {
        var capabilities = new RuntimeCapabilitySet(new[] { RuntimeCapability.NodeSetImport });

        Assert.True(capabilities.Supports(RuntimeCapability.NodeSetImport));
        Assert.False(capabilities.Supports(RuntimeCapability.SecurityProfiles));
    }
}
