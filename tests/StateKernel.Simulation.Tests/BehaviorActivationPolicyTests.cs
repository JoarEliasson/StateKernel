using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Modes;

namespace StateKernel.Simulation.Tests;

public sealed class BehaviorActivationPolicyTests
{
    [Fact]
    public void AlwaysActivePolicy_ReturnsTrueAtOriginAndLaterTicks()
    {
        var policy = new AlwaysActivePolicy();

        Assert.True(policy.IsActive(CreateContext(0)));
        Assert.True(policy.IsActive(CreateContext(3)));
    }

    [Fact]
    public void AlwaysActivePolicy_IgnoresCurrentMode()
    {
        var policy = new AlwaysActivePolicy();

        Assert.True(policy.IsActive(CreateContext(1, "Idle")));
        Assert.True(policy.IsActive(CreateContext(1, "Run")));
    }

    [Fact]
    public void TickRangeActivationPolicy_UsesInclusiveBoundaries()
    {
        var policy = new TickRangeActivationPolicy(2, 4);

        Assert.False(policy.IsActive(CreateContext(1)));
        Assert.True(policy.IsActive(CreateContext(2)));
        Assert.True(policy.IsActive(CreateContext(3)));
        Assert.True(policy.IsActive(CreateContext(4)));
        Assert.False(policy.IsActive(CreateContext(5)));
    }

    [Fact]
    public void TickRangeActivationPolicy_IgnoresCurrentMode()
    {
        var policy = new TickRangeActivationPolicy(2, 2);

        Assert.True(policy.IsActive(CreateContext(2, "Idle")));
        Assert.True(policy.IsActive(CreateContext(2, "Run")));
    }

    [Fact]
    public void TickRangeActivationPolicy_CanTreatOriginTickAsActive()
    {
        var policy = new TickRangeActivationPolicy(0, 0);

        Assert.True(policy.IsActive(CreateContext(0)));
    }

    [Fact]
    public void TickRangeActivationPolicy_RejectsInvalidRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickRangeActivationPolicy(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickRangeActivationPolicy(0, -1));
        Assert.Throws<ArgumentException>(() => new TickRangeActivationPolicy(3, 2));
    }

    [Fact]
    public void ModeMatchActivationPolicy_IsActiveOnlyWhenCurrentModeMatches()
    {
        var policy = new ModeMatchActivationPolicy(SimulationMode.From("Run"));

        Assert.True(policy.IsActive(CreateContext(1, "Run")));
        Assert.False(policy.IsActive(CreateContext(1, "Idle")));
    }

    [Fact]
    public void ModeMatchActivationPolicy_RejectsNullConfiguredMode()
    {
        Assert.Throws<ArgumentNullException>(() => new ModeMatchActivationPolicy(null!));
    }

    private static BehaviorActivationContext CreateContext(long sequenceNumber, string modeName = "Run")
    {
        var tick = new SimulationTick(sequenceNumber, TimeSpan.FromMilliseconds(sequenceNumber * 10));
        var mode = SimulationMode.From(modeName);
        return new BehaviorActivationContext(tick, mode);
    }
}
