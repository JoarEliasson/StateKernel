using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.StateMachines;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationStateMachineDefinitionTests
{
    private static readonly SimulationStateId IdleState = SimulationStateId.From("IdleState");
    private static readonly SimulationStateId RunState = SimulationStateId.From("RunState");
    private static readonly SimulationStateId WarmupState = SimulationStateId.From("WarmupState");
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public void SimulationStateId_FromRejectsNullEmptyAndWhitespace()
    {
        Assert.ThrowsAny<ArgumentException>(() => SimulationStateId.From(null!));
        Assert.ThrowsAny<ArgumentException>(() => SimulationStateId.From(string.Empty));
        Assert.ThrowsAny<ArgumentException>(() => SimulationStateId.From("   "));
    }

    [Fact]
    public void SimulationStateId_FromTrimsInputAndUsesOrdinalEquality()
    {
        var left = SimulationStateId.From("  RunState  ");
        var right = SimulationStateId.From("RunState");
        var differentCase = SimulationStateId.From("runstate");

        Assert.Equal("RunState", left.Value);
        Assert.Equal(left, right);
        Assert.NotEqual(left, differentCase);
    }

    [Fact]
    public void ValidDefinitionConstruction_Succeeds()
    {
        var definition = CreateDefinition(
            IdleState,
            [
                new SimulationStateTransitionDefinition(
                    IdleState,
                    RunState,
                    new CompletedTickMatchCondition(2)),
            ]);

        Assert.Equal(IdleState, definition.InitialStateId);
        Assert.Equal([IdleState, RunState], definition.States.Select(state => state.Id).ToArray());
        Assert.Single(definition.Transitions);
    }

    [Fact]
    public void DuplicateStateIds_AreRejected()
    {
        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationStateMachineDefinition(
                IdleState,
                [
                    new SimulationStateDefinition(IdleState),
                    new SimulationStateDefinition(IdleState),
                ],
                Array.Empty<SimulationStateTransitionDefinition>()));

        Assert.Contains("Duplicate state", exception.Message);
    }

    [Fact]
    public void MissingInitialState_IsRejected()
    {
        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationStateMachineDefinition(
                WarmupState,
                [
                    new SimulationStateDefinition(IdleState),
                    new SimulationStateDefinition(RunState),
                ],
                Array.Empty<SimulationStateTransitionDefinition>()));

        Assert.Contains("initial state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UndefinedTransitionSource_IsRejected()
    {
        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            CreateDefinition(
                IdleState,
                [
                    new SimulationStateTransitionDefinition(
                        WarmupState,
                        RunState,
                        new CompletedTickMatchCondition(2)),
                ]));

        Assert.Contains("source state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UndefinedTransitionTarget_IsRejected()
    {
        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            CreateDefinition(
                IdleState,
                [
                    new SimulationStateTransitionDefinition(
                        IdleState,
                        WarmupState,
                        new CompletedTickMatchCondition(2)),
                ]));

        Assert.Contains("target state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultipleTransitionsFromTheSameSourceState_AreRejected()
    {
        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            CreateDefinition(
                IdleState,
                [
                    new SimulationStateTransitionDefinition(
                        IdleState,
                        RunState,
                        new CompletedTickMatchCondition(2)),
                    new SimulationStateTransitionDefinition(
                        IdleState,
                        WarmupState,
                        new CompletedTickAtOrAfterCondition(4)),
                ],
                WarmupState));

        Assert.Contains("at most one transition definition per source state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DifferentSourceStates_CanEachDefineOneConditionDrivenTransition()
    {
        var definition = CreateDefinition(
            IdleState,
            [
                new SimulationStateTransitionDefinition(
                    IdleState,
                    RunState,
                    new CompletedTickAtOrAfterCondition(2)),
                new SimulationStateTransitionDefinition(
                    WarmupState,
                    IdleState,
                    new CompletedTickMatchCondition(2)),
            ],
            WarmupState);

        Assert.Equal(2, definition.Transitions.Count);
    }

    [Fact]
    public void SimulationStateModeMap_RejectsDuplicateBindings()
    {
        var definition = CreateDefinition(IdleState);

        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationStateModeMap(
                definition,
                [
                    new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, RunMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode),
                ]));

        Assert.Contains("Duplicate state binding", exception.Message);
    }

    [Fact]
    public void SimulationStateModeMap_RejectsMissingBindingsForDefinedStates()
    {
        var definition = CreateDefinition(IdleState);

        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationStateModeMap(
                definition,
                [
                    new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                ]));

        Assert.Contains("must define an operating mode", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SimulationStateModeMap_RejectsBindingsForUndefinedStates()
    {
        var definition = CreateDefinition(IdleState);

        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationStateModeMap(
                definition,
                [
                    new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(WarmupState, RunMode),
                ]));

        Assert.Contains("undefined state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CoordinatorConstruction_RejectsInitialStateModeMisalignment()
    {
        var definition = CreateDefinition(IdleState);
        var map = CreateModeMap(
            definition,
            new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
            new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode));
        var modeController = new SimulationModeController(RunMode);

        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationStateMachineCoordinator(definition, map, modeController));

        Assert.Contains("must match the mapped mode", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SimulationStateTransition_RejectsEquivalentPreviousAndNextStates()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new SimulationStateTransition(Clock.SimulationTick.Origin, IdleState, IdleState));

        Assert.Contains("must change the current state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SimulationStateMachineDefinition CreateDefinition(
        SimulationStateId initialStateId,
        IEnumerable<SimulationStateTransitionDefinition>? transitions = null,
        params SimulationStateId[] extraStates)
    {
        var states = new List<SimulationStateDefinition>
        {
            new(IdleState),
            new(RunState),
        };

        states.AddRange(extraStates.Select(stateId => new SimulationStateDefinition(stateId)));

        return new SimulationStateMachineDefinition(
            initialStateId,
            states,
            transitions ?? Array.Empty<SimulationStateTransitionDefinition>());
    }

    private static SimulationStateModeMap CreateModeMap(
        SimulationStateMachineDefinition definition,
        params KeyValuePair<SimulationStateId, SimulationMode>[] mappings)
    {
        return new SimulationStateModeMap(definition, mappings);
    }
}
