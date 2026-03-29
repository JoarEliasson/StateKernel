using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StateKernel.ControlApi;
using StateKernel.ControlApi.Contracts;

namespace StateKernel.IntegrationTests;

public sealed class ControlApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ControlApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SystemDescriptorEndpoint_ReturnsTheFoundationDescriptor()
    {
        using var client = factory.CreateClient();

        var descriptor = await client.GetFromJsonAsync<ControlPlaneDescriptor>("/api/system/descriptor");

        Assert.NotNull(descriptor);
        Assert.Equal("StateKernel", descriptor.ProductName);
        Assert.Contains("StateKernel.Observability", descriptor.Modules);
    }
}
