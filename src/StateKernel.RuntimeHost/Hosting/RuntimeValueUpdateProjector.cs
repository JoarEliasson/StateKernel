using StateKernel.Runtime.Abstractions;
using StateKernel.Simulation.Signals;

namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Projects committed simulation signal snapshot values into ordered runtime value updates.
/// </summary>
public static class RuntimeValueUpdateProjector
{
    /// <summary>
    /// Creates ordered runtime value updates for all projected signals currently present in the committed snapshot.
    /// </summary>
    /// <param name="compiledPlan">The compiled runtime plan describing projected signals.</param>
    /// <param name="snapshot">The committed simulation signal snapshot to project.</param>
    /// <returns>The ordered runtime value updates for projected committed signals.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="compiledPlan" /> or <paramref name="snapshot" /> is null.
    /// </exception>
    public static IReadOnlyList<RuntimeValueUpdate> CreateUpdates(
        CompiledRuntimePlan compiledPlan,
        SimulationSignalSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(compiledPlan);
        ArgumentNullException.ThrowIfNull(snapshot);

        var updates = new List<RuntimeValueUpdate>();

        foreach (var binding in compiledPlan.Bindings)
        {
            if (!snapshot.TryGetValue(binding.SourceSignalId, out var signalValue))
            {
                continue;
            }

            updates.Add(
                new RuntimeValueUpdate(
                    signalValue.SignalId,
                    signalValue.Sample.Value,
                    signalValue.Tick.SequenceNumber));
        }

        return Array.AsReadOnly(updates.ToArray());
    }
}
