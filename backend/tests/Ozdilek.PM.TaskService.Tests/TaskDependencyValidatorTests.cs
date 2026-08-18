using FluentAssertions;
using Ozdilek.PM.TaskService.Domain;
using Xunit;

namespace Ozdilek.PM.TaskService.Tests;

public class TaskDependencyValidatorTests
{
    [Fact]
    public void SelfDependency_IsACycle()
    {
        var taskId = Guid.NewGuid();

        var result = TaskDependencyValidator.WouldCreateCycle(new Dictionary<Guid, Guid?>(), taskId, taskId);

        result.Should().BeTrue();
    }

    [Fact]
    public void NoExistingEdges_IsNotACycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var result = TaskDependencyValidator.WouldCreateCycle(new Dictionary<Guid, Guid?>(), a, b);

        result.Should().BeFalse();
    }

    [Fact]
    public void DirectBackReference_IsACycle()
    {
        // A already depends on B. Adding B -> A would close the loop.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var edges = new Dictionary<Guid, Guid?> { [a] = b };

        var result = TaskDependencyValidator.WouldCreateCycle(edges, b, a);

        result.Should().BeTrue();
    }

    [Fact]
    public void TransitiveBackReference_IsACycle()
    {
        // A -> B -> C already. Adding C -> A would close a 3-node loop.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var edges = new Dictionary<Guid, Guid?> { [a] = b, [b] = c };

        var result = TaskDependencyValidator.WouldCreateCycle(edges, c, a);

        result.Should().BeTrue();
    }

    [Fact]
    public void LinearChain_IsNotACycle()
    {
        // A -> B already. Adding C -> B (C depends on B) is a valid tree, not a cycle.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var edges = new Dictionary<Guid, Guid?> { [a] = b };

        var result = TaskDependencyValidator.WouldCreateCycle(edges, c, b);

        result.Should().BeFalse();
    }
}
