using StateKernel.Runtime.Abstractions;
using StateKernel.Runtime.UaNet;

namespace StateKernel.Runtime.UaNet.Tests;

public sealed class UaNetRuntimeAdapterCatalogTests
{
    [Fact]
    public void Default_DeclaresTheExpectedAdapterKeyAndCapabilities()
    {
        var descriptor = UaNetRuntimeAdapterCatalog.Default;

        Assert.Equal("ua-net", descriptor.Key);
        Assert.True(descriptor.Capabilities.Supports(RuntimeCapability.ReadOnlyValueExposure));
        Assert.False(descriptor.Capabilities.Supports(RuntimeCapability.SecurityProfiles));
    }
}
