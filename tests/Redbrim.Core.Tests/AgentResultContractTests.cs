namespace Redbrim.Core.Tests;

public class AgentResultContractTests
{
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
        Assert.Equal(["Done", "NotDone", "Unknown"], values);
    }

    [Fact]
    public void AgentActionLogEntry_Defines_ExpectedFields()
    {
        var entry = new AgentActionLogEntry(DateTime.UtcNow, "action", """{"ok":true}""");

        Assert.IsType<DateTime>(entry.Timestamp);
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
