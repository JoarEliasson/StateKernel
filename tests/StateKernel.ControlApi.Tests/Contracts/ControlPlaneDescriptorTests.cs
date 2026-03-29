using StateKernel.ControlApi.Contracts;

namespace StateKernel.ControlApi.Tests.Contracts;

public sealed class ControlPlaneDescriptorTests
{
    [Fact]
    public void CreateDefault_ExposesTheExpectedModuleBoundary()
    {
        var descriptor = ControlPlaneDescriptor.CreateDefault();

        Assert.Equal("StateKernel", descriptor.ProductName);
        Assert.Contains("StateKernel.RuntimeHost", descriptor.Modules);
        Assert.Contains("StateKernel.Simulation", descriptor.Modules);
    }
}
