using StateKernel.ControlApi.Contracts;
using StateKernel.ControlApi.Contracts.Run;
using StateKernel.ControlApi.Contracts.Runtime;
using StateKernel.ControlApi.Run;
using StateKernel.ControlApi.Runtime;
using StateKernel.Runtime.Abstractions;
using StateKernel.Runtime.UaNet;
using StateKernel.RuntimeHost.Execution;
using StateKernel.RuntimeHost.Hosting;

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
        builder.Services.AddSingleton<IRuntimeAdapterFactory, UaNetRuntimeAdapterFactory>();
        builder.Services.AddSingleton<RuntimeHostService>();
        builder.Services.AddSingleton<ISimulationRunDefinitionCatalog>(_ =>
            SimulationRunDefinitionCatalog.CreateDefault());
        builder.Services.AddSingleton<SimulationExecutionOrchestrator>();
        builder.Services.AddSingleton<RuntimeControlService>();
        builder.Services.AddSingleton<RunControlService>();

        var app = builder.Build();

        app.UseExceptionHandler();

        app.MapGet(
            "/health",
            () => TypedResults.Ok(new { status = "ok", service = "StateKernel.ControlApi" }));

        app.MapGet(
            "/api/system/descriptor",
            () => TypedResults.Ok(ControlPlaneDescriptor.CreateDefault()));

        var runtimeGroup = app.MapGroup("/api/runtime");

        runtimeGroup.MapGet(
            string.Empty,
            (RuntimeControlService runtimeControlService) =>
                TypedResults.Ok(runtimeControlService.GetStatus()));

        runtimeGroup.MapPost(
            "/start",
            StartRuntimeAsync);

        runtimeGroup.MapPost(
            "/stop",
            StopRuntimeAsync);

        var runGroup = app.MapGroup("/api/run");

        runGroup.MapGet(
            string.Empty,
            (RunControlService runControlService) =>
                TypedResults.Ok(runControlService.GetStatus()));

        runGroup.MapPost(
            "/start",
            StartRunAsync);

        runGroup.MapPost(
            "/stop",
            StopRunAsync);

        app.Run();
    }

    private static bool IsBadRequestException(Exception exception)
    {
        return exception is ArgumentException or InvalidOperationException;
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult CreateProblem(
        int statusCode,
        string title,
        string detail)
    {
        return TypedResults.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title);
    }

    private static async Task<IResult> StartRuntimeAsync(
        StartRuntimeRequest request,
        RuntimeControlService runtimeControlService,
        CancellationToken cancellationToken)
    {
        return await ExecuteRuntimeLifecycleAsync(
            cancellation => runtimeControlService.StartAsync(request, cancellation),
            cancellationToken,
            "Invalid runtime start request");
    }

    private static async Task<IResult> StopRuntimeAsync(
        RuntimeControlService runtimeControlService,
        CancellationToken cancellationToken)
    {
        return await ExecuteRuntimeLifecycleAsync(
            runtimeControlService.StopAsync,
            cancellationToken);
    }

    private static async Task<IResult> StartRunAsync(
        StartRunRequest request,
        RunControlService runControlService,
        CancellationToken cancellationToken)
    {
        return await ExecuteRunLifecycleAsync(
            cancellation => runControlService.StartAsync(request, cancellation),
            cancellationToken,
            "Invalid run start request");
    }

    private static async Task<IResult> StopRunAsync(
        RunControlService runControlService,
        CancellationToken cancellationToken)
    {
        return await ExecuteRunLifecycleAsync(
            runControlService.StopAsync,
            cancellationToken);
    }

    private static async Task<IResult> ExecuteRuntimeLifecycleAsync(
        Func<CancellationToken, ValueTask<RuntimeStatusResponse>> operation,
        CancellationToken cancellationToken,
        string? badRequestTitle = null)
    {
        try
        {
            return TypedResults.Ok(await operation(cancellationToken));
        }
        catch (RuntimeControlConflictException exception)
        {
            return CreateProblem(
                StatusCodes.Status409Conflict,
                "Runtime conflict",
                exception.Message);
        }
        catch (RuntimeHostFaultException exception)
        {
            return CreateProblem(
                StatusCodes.Status500InternalServerError,
                "Runtime failure",
                exception.FaultInfo.Message);
        }
        catch (Exception exception) when (badRequestTitle is not null && IsBadRequestException(exception))
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                badRequestTitle,
                exception.Message);
        }
    }

    private static async Task<IResult> ExecuteRunLifecycleAsync(
        Func<CancellationToken, ValueTask<RunStatusResponse>> operation,
        CancellationToken cancellationToken,
        string? badRequestTitle = null)
    {
        try
        {
            return TypedResults.Ok(await operation(cancellationToken));
        }
        catch (RunControlConflictException exception)
        {
            return CreateProblem(
                StatusCodes.Status409Conflict,
                "Run conflict",
                exception.Message);
        }
        catch (SimulationRunFaultException exception)
        {
            return CreateProblem(
                StatusCodes.Status500InternalServerError,
                "Run failure",
                exception.FaultInfo.Message);
        }
        catch (Exception exception) when (badRequestTitle is not null && IsBadRequestException(exception))
        {
            return CreateProblem(
                StatusCodes.Status400BadRequest,
                badRequestTitle,
                exception.Message);
        }
    }
}
