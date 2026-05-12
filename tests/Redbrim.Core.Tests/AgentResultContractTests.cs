using System.Diagnostics.CodeAnalysis;

namespace Redbrim.Core.Tests;

[ExcludeFromCodeCoverage]
public class AgentResultContractTests
{
    [Fact]
    public void AgentExecutionInput_Defines_SystemSpecification_And_Log()
    {
        var specification = new SystemSpecification("spec");
        var log = new List<AgentActionLogEntry>
        {
            new(DateTime.UtcNow, "agent-1", CodingAgentRole.Requirements, "step")
        };
        var input = new AgentExecutionInput("prompt", specification, log);

        Assert.Equal("prompt", input.Prompt);
        Assert.Same(specification, input.SystemSpecification);
        Assert.Same(log, input.Log);
    }

    [Fact]
    public void AgentStopSignal_Defines_ExpectedValues()
    {
        var values = Enum.GetNames<AgentStopSignal>();
        Assert.Equal(["Continue", "SoftStop", "HardStop"], values);
    }

    [Fact]
    public void AgentCompletion_Defines_ExpectedValues()
    {
        var values = Enum.GetNames<AgentCompletion>();
        Assert.Equal(["Done", "NotDone", "Indeterminate"], values);
    }

    [Fact]
    public void AgentActionLogEntry_Defines_ExpectedFields()
    {
        var entry = new AgentActionLogEntry(DateTime.UtcNow, "agent-1", CodingAgentRole.Red, "action", """{"ok":true}""");

        Assert.IsType<DateTime>(entry.Timestamp);
        Assert.Equal("agent-1", entry.AgentId);
        Assert.Equal(CodingAgentRole.Red, entry.AgentRole);
        Assert.Equal("action", entry.Description);
        Assert.Equal("""{"ok":true}""", entry.Data);
    }

    [Fact]
    public void AgentResult_Defines_ExpectedFieldsOnly()
    {
        var result = new AgentResult(
            AgentStopSignal.Continue,
            AgentCompletion.Done,
            []);

        Assert.Equal(AgentStopSignal.Continue, result.StopSignal);
        Assert.Equal(AgentCompletion.Done, result.Completion);
        Assert.Empty(result.Log);

        var propertyNames = typeof(AgentResult).GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(["StopSignal", "Completion", "Log"], propertyNames);
    }
}
