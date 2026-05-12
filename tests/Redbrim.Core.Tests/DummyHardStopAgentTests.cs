namespace Redbrim.Core.Tests;

public class DummyHardStopAgentTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsHardStopIndeterminateWithDummyLog()
    {
        var agent = new DummyHardStopAgentForTests();

        var result = await agent.ExecuteAsync(new AgentExecutionInput("prompt"));

        Assert.Equal(AgentStopSignal.HardStop, result.StopSignal);
        Assert.Equal(AgentCompletion.Indeterminate, result.Completion);
        var entry = Assert.Single(result.Log);
        Assert.Equal(agent.Name, entry.AgentId);
        Assert.Equal(agent.Role, entry.AgentRole);
        Assert.Equal("Dummy agent invoked", entry.Description);
    }

    private sealed class DummyHardStopAgentForTests : ICodingAgent
    {
        public string Name => "dummy-hard-stop";
        public CodingAgentRole Role => CodingAgentRole.Requirements;
        public IReadOnlyList<string> RequiredCapabilities => [];

        public Task<AgentResult> ExecuteAsync(AgentExecutionInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return Task.FromResult(new AgentResult(
                AgentStopSignal.HardStop,
                AgentCompletion.Indeterminate,
                [
                    new AgentActionLogEntry(DateTime.UtcNow, Name, Role, "Dummy agent invoked")
                ]));
        }
    }
}
