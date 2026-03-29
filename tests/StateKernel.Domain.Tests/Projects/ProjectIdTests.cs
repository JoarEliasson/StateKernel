using StateKernel.Domain.Projects;

namespace StateKernel.Domain.Tests.Projects;

public sealed class ProjectIdTests
{
    [Fact]
    public void New_ReturnsANonEmptyIdentifier()
    {
        var projectId = ProjectId.New();

        Assert.NotEqual(Guid.Empty, projectId.Value);
        Assert.Equal(projectId.Value.ToString("D"), projectId.ToString());
    }

    [Fact]
    public void Parse_RejectsTheEmptyGuid()
    {
        var action = () => ProjectId.Parse(Guid.Empty.ToString("D"));

        Assert.Throws<FormatException>(action);
    }
}
