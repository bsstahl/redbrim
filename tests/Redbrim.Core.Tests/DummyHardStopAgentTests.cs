namespace Redbrim.Core.Tests;

public class DummyHardStopAgentTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsHardStopUnknownWithDummyLog()
    {
        var agent = new DummyHardStopAgent();

        var result = await agent.ExecuteAsync(new AgentExecutionInput("prompt"));

        Assert.Equal(AgentStopSignal.HardStop, result.StopSignal);
        Assert.Equal(AgentCompletion.Unknown, result.Completion);
        var entry = Assert.Single(result.Log);
        Assert.Equal("Dummy agent invoked", entry.Description);
    }
}
