using StateKernel.ControlApi.Contracts;

namespace StateKernel.ControlApi;

/// <summary>
/// Hosts the ASP.NET Core control plane for StateKernel.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Starts the control API process.
    /// </summary>
    /// <param name="args">The command-line arguments supplied to the process.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.UseExceptionHandler();

        app.MapGet(
            "/health",
            () => TypedResults.Ok(new { status = "ok", service = "StateKernel.ControlApi" }));

        app.MapGet(
            "/api/system/descriptor",
            () => TypedResults.Ok(ControlPlaneDescriptor.CreateDefault()));

        app.Run();
    }
}
