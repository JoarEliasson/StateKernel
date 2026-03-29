namespace StateKernel.Simulation.Signals.Dependencies.Diagnostics;

/// <summary>
/// Defines the closed baseline set of timing-aware dependency diagnostic codes.
/// </summary>
public enum SimulationDependencyDiagnosticCode
{
    /// <summary>
    /// The consumer's first need and the producer's first publish occur on the same due tick, but
    /// same-tick reads are unsupported under the prior-step committed-snapshot model.
    /// </summary>
    SameTickDependencyUnavailable,

    /// <summary>
    /// The producer's first possible due tick occurs after the consumer's first due tick, so no
    /// prior producer value exists for the first consumer need under the baseline model.
    /// </summary>
    NoPriorProducerBeforeFirstConsumerTick,
}
