using StateKernel.Simulation.Modes;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationModeTests
{
    [Fact]
    public void From_RejectsNullEmptyAndWhitespaceNames()
    {
        Assert.Throws<ArgumentNullException>(() => SimulationMode.From(null!));
        Assert.Throws<ArgumentException>(() => SimulationMode.From(string.Empty));
        Assert.Throws<ArgumentException>(() => SimulationMode.From("   "));
    }

    [Fact]
    public void From_StoresTheTrimmedCanonicalName()
    {
        var mode = SimulationMode.From("  Run  ");

        Assert.Equal("Run", mode.Value);
        Assert.Equal("Run", mode.ToString());
    }

    [Fact]
    public void Equality_UsesTheTrimmedStoredNameWithOrdinalSemantics()
    {
        var first = SimulationMode.From(" Run ");
        var second = SimulationMode.From("Run");
        var third = SimulationMode.From("run");

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first.GetHashCode(), third.GetHashCode());
    }

    [Fact]
    public void Controller_RejectsNullInitialMode()
    {
        Assert.Throws<ArgumentNullException>(() => new SimulationModeController(null!));
    }

    [Fact]
    public void Controller_ExposesInitialModeAndUpdatesDeterministically()
    {
        var idleMode = SimulationMode.From("Idle");
        var runMode = SimulationMode.From("Run");
        var controller = new SimulationModeController(idleMode);

        Assert.Equal(idleMode, controller.CurrentMode);

        controller.SetMode(runMode);

        Assert.Equal(runMode, controller.CurrentMode);
    }

    [Fact]
    public void Controller_RejectsNullModeUpdates()
    {
        var controller = new SimulationModeController(SimulationMode.From("Idle"));

        Assert.Throws<ArgumentNullException>(() => controller.SetMode(null!));
    }
}
